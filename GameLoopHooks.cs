﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public static class GameLoopHooks
    {
        public static void ApplyHooks()
        {
            On.ProcessManager.PostSwitchMainProcess += OnPostSwitchMainProcess;
            On.RainWorldGame.Update += Update;
            On.PlayerProgression.SaveToDisk += OnSaveGame;
            On.SaveState.SessionEnded += OnSessionEnded;
            On.RainWorldGame.ctor += OnStartSession;
            On.HardmodeStart.Update += OnHardmodeStart;
            On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += OnOEEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.Update += OnLCEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.Update += OnSpearEndingUpdate;
            On.RegionState.AdaptRegionStateToWorld += OnAdaptRegionStateToWorld;

            try
            {
                IL.WinState.CycleCompleted += ILCycleCompleted;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.ProcessManager.PostSwitchMainProcess -= OnPostSwitchMainProcess;
            On.RainWorldGame.Update -= Update;
            On.PlayerProgression.SaveToDisk -= OnSaveGame;
            On.SaveState.SessionEnded -= OnSessionEnded;
            On.RainWorldGame.ctor -= OnStartSession;
            On.HardmodeStart.Update -= OnHardmodeStart;
            On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update -= OnOEEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.Update -= OnLCEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.Update -= OnSpearEndingUpdate;
            On.RegionState.AdaptRegionStateToWorld -= OnAdaptRegionStateToWorld;
            IL.WinState.CycleCompleted -= ILCycleCompleted;
        }

        public static void OnPostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == ProcessManager.ProcessID.Game 
                && (Plugin.RandoManager == null || !Plugin.RandoManager.isRandomizerActive))
            {
                // If we don't have a manager yet, create one
                if (Plugin.RandoManager == null)
                {
                    Plugin.RandoManager = new ManagerVanilla();
                }

                if (self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat == null)
                {
                    Plugin.Log.LogError("No slugcat selected");
                }
                else
                {
                    try
                    {
                        Plugin.RandoManager.StartNewGameSession(self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat,
                            self.menuSetup.startGameCondition != ProcessManager.MenuSetup.StoryGameInitCondition.New);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError("Encountered exception while starting game session");
                        Plugin.Log.LogError(e);
                    }
                }
            }

            if (ID == ProcessManager.ProcessID.MainMenu)
            {
                // Turn off randomizer when quitting to menu
                if (Plugin.RandoManager != null) Plugin.RandoManager.isRandomizerActive = false;
            }

            if (ID == ProcessManager.ProcessID.SleepScreen)
            {
                // Check for any new passages
                foreach (string check in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
                {
                    WinState.EndgameID id = new WinState.EndgameID(check, false);

                    SaveState saveState = self.rainWorld.progression.currentSaveState == null
                        ? self.rainWorld.progression.starvedSaveState
                        : self.rainWorld.progression.currentSaveState;
                    if (!(Plugin.RandoManager.IsLocationGiven($"Passage-{check}") ?? true) // if location exists and is not given
                        && saveState.deathPersistentSaveData.winState.GetTracker(id, false) != null)
                    {
                        WinState.EndgameTracker tracker = saveState.deathPersistentSaveData.winState.GetTracker(id, false);

                        // Normal Passages
                        if (tracker.GoalFullfilled)
                        {
                            Plugin.RandoManager.GiveLocation($"Passage-{check}");
                        }

                        // Wanderer individual pips
                        // TODO: Add individual wanderer pips to standalone
                        if (tracker.ID == WinState.EndgameID.Traveller
                            && Plugin.RandoManager is ManagerArchipelago)
                        {
                            int progressCount = 0;
                            foreach (bool prog in (tracker as WinState.BoolArrayTracker).progress)
                            {
                                if (prog) progressCount++;
                            }

                            if (progressCount > 0)
                            {
                                Plugin.RandoManager.GiveLocation($"Wanderer-{progressCount}");
                            }
                        }

                        // Gourmand Food Quest
                        if (tracker is WinState.GourFeastTracker gourTracker)
                        {
                            for (int i = 0; i < gourTracker.progress.Length; i++)
                            {
                                string type = WinState.GourmandPassageTracker[i].type == AbstractPhysicalObject.AbstractObjectType.Creature
                                    ? WinState.GourmandPassageTracker[i].crits[0].value : WinState.GourmandPassageTracker[i].type.value;

                                if (gourTracker.progress[i] > 0)
                                {
                                    Plugin.RandoManager.GiveLocation($"FoodQuest-{type}");
                                }
                            }
                        }
                    }
                }
            }

            orig(self, ID);
        }

        public static void OnStartSession(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            Plugin.Singleton.game = self;

            if (!Plugin.RandoManager.isRandomizerActive || !self.IsStorySession) return;

            self.rainWorld.progression.ReloadLocksList();
            self.GetStorySession.saveState.deathPersistentSaveData.karmaCap = Plugin.RandoManager.CurrentMaxKarma;

            // The glow is not death persistent, so I have to make sure it stays applied
            self.GetStorySession.saveState.theGlow = Plugin.RandoManager.GivenNeuronGlow;
            self.GetStorySession.saveState.deathPersistentSaveData.theMark = Plugin.RandoManager.GivenMark;

            self.GetStorySession.saveState.hasRobo = Plugin.RandoManager.GivenRobo;
            self.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = Plugin.RandoManager.GivenPebblesOff;
            self.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = Plugin.RandoManager.GivenSpearPearlRewrite;
            }

        // Remove Hunter's starting macguffins
        public static void OnHardmodeStart(On.HardmodeStart.orig_Update orig, HardmodeStart self, bool eu)
        {
            if (!Plugin.RandoManager.isRandomizerActive
                || self.room.game.manager.fadeToBlack >= 1f
                || self.phase != HardmodeStart.Phase.Init
                || self.nshSwarmer == null)
            {
                orig(self, eu);
                return;
            }

            orig(self, eu);

            Player player = null;

            if (ModManager.CoopAvailable)
            {
                foreach (HardmodeStart.HardmodePlayer hardmodePlayer in self.hardmodePlayers)
                {
                    if (hardmodePlayer.MainPlayer)
                    {
                        player = hardmodePlayer.Player;
                    }
                }
            }

            if (player == null)
            {
                player = self.room.game.Players[0].realizedCreature as Player;
            }

            player.objectInStomach = null;

            foreach (Creature.Grasp grasp in player.grasps)
            {
                if (grasp != null
                    && grasp.grabbed != null
                    && grasp.grabbed.abstractPhysicalObject != null
                    && grasp.grabbed.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
                {
                    grasp.grabbed.AllGraspsLetGoOfThisObject(true);
                    self.room.game.GetStorySession.RemovePersistentTracker(grasp.grabbed.abstractPhysicalObject);
                    grasp.grabbed.abstractPhysicalObject.Destroy();
                    self.nshSwarmer = null;
                }
            }
        }

        public static void Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.GamePaused) return;

            // Display any pending notifications
            if (Plugin.Singleton.notifQueue.Count > 0)
            {
                // If we have any pending messages and are in the actual game loop
                if (self.session.Players[0]?.realizedCreature?.room != null
                    && self.cameras[0].hud?.textPrompt != null
                    && self.cameras[0].hud.textPrompt.messages.Count < 1
                    && self.manager.currentMainLoop.ID == ProcessManager.ProcessID.Game)
                {
                    //game.manager.rainWorld.inGameTranslator.Translate(notifQueue.Dequeue())

                    string message = Plugin.Singleton.notifQueue.Dequeue();
                    if (message.Contains("//"))
                    {
                        string[] split = Regex.Split(message, "//");
                        self.cameras[0].hud.textPrompt.AddMessage(split[0], 0, 160, true, true, 100f,
                            new List<MultiplayerUnlocks.SandboxUnlockID>() { new MultiplayerUnlocks.SandboxUnlockID(split[1]) });
                    }
                    else
                    {
                        self.cameras[0].hud.textPrompt.AddMessage(message, 0, 160, true, true);
                    }

                    self.session.Players[0].realizedCreature.room.PlaySound(SoundID.MENU_Passage_Button, 0, 1f, 1f);
                }
            }

            // Active only
            if (!Plugin.RandoManager.isRandomizerActive) return;

            // Applying glow effect if unlock has been given
            for (int i = 0; i < self.Players.Count; i++)
            {
                if (Plugin.RandoManager.GivenNeuronGlow
                    && self.Players[i]?.realizedCreature is Player player
                    && !player.glowing)
                {
                    player.glowing = true;
                }
            }

            // Detect pearl stored in shelter
            Room currentRoom = self.FirstRealizedPlayer?.room;
            if (currentRoom?.abstractRoom.shelter ?? false)
            {
                for (int j = 0; j < currentRoom.updateList.Count; j++)
                {
                    if (currentRoom.updateList[j] is DataPearl pearl)
                    {
                        string locName = Plugin.RandoManager is ManagerArchipelago
                            ? $"Pearl-{pearl.AbstractPearl.dataPearlType.value}-{currentRoom.abstractRoom.name.Substring(0, 2)}"
                            : $"Pearl-{pearl.AbstractPearl.dataPearlType.value}";

                        Plugin.RandoManager.GiveLocation(locName);
                    }
                }
            }
        }

        public static bool OnSaveGame(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            if (Plugin.RandoManager.isRandomizerActive)
            {
                Plugin.RandoManager.SaveGame(saveCurrentState);
            }

            return orig(self, saveCurrentState, saveMaps, saveMiscProg);
        }

        public static void OnSessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            orig(self, game, survived, newMalnourished);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            // If we survived this cycle, paste current queue to saved backup
            // If we died, restore current queue from saved backup
            if (survived)
            {
                Plugin.Singleton.lastItemDeliveryQueue = new Queue<Unlock.Item>(Plugin.Singleton.itemDeliveryQueue);
            }
            else
            {
                Plugin.Singleton.itemDeliveryQueue = new Queue<Unlock.Item>(Plugin.Singleton.lastItemDeliveryQueue);
            }
        }

        public static void ILCycleCompleted(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField("karmaCap")),
                    x => x.MatchLdcI4(4)
                    );

                // Remove the check for if the player has at least 5 karma
                // for the Survivor passage increase
                c.Index += 1;
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldc_I4_4);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for CycleCompleted");
                Plugin.Log.LogError(e);
            }
        }

        public static void OnOEEndingScriptUpdate(On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.orig_Update orig, MSCRoomSpecificScript.OE_GourmandEnding self, bool eu)
        {
            orig(self, eu);

            // Check for completion via Outer Expanse
            if (Plugin.RandoManager is ManagerArchipelago managerAP
                && !managerAP.gameCompleted
                && self.endTrigger)
            {
                managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SlugTree);
            }
        }

        public static void OnLCEndingScriptUpdate(On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.orig_Update orig, MSCRoomSpecificScript.LC_FINAL self, bool eu)
        {
            orig(self, eu);

            // Check for completion via killing Chieftain scavenger
            if (Plugin.RandoManager is ManagerArchipelago managerAP
                && !managerAP.gameCompleted
                && self.endingTriggered)
            {
                managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.ScavKing);
            }
        }

        public static void OnSpearEndingUpdate(On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.orig_Update orig, MSCRoomSpecificScript.SpearmasterEnding self, bool eu)
        {
            orig(self, eu);

            // Check for completion via delivering Spearmaster's pearl to Comms array
            if (Plugin.RandoManager is ManagerArchipelago managerAP
                && !managerAP.gameCompleted
                && self.SMEndingPhase == MSCRoomSpecificScript.SpearmasterEnding.SMEndingState.PEARLDATA)
            {
                managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.Messenger);
            }
        }

        public static void OnAdaptRegionStateToWorld(On.RegionState.orig_AdaptRegionStateToWorld orig, RegionState self, int playerShelter, int activeGate)
        {
            orig(self, playerShelter, activeGate);

            if (!Plugin.Singleton.ItemShelterDelivery) return;

            int count = Plugin.Singleton.itemDeliveryQueue.Count;
            for (int i = 0; i < count; i++)
            {
                AbstractPhysicalObject obj = Plugin.ItemToAbstractObject(Plugin.Singleton.itemDeliveryQueue.Dequeue(), self.world, self.world.GetAbstractRoom(playerShelter));
                if (obj != null) // Sometimes shelters have a -1 index, we can't spawn items in these
                    self.savedObjects.Add(obj.ToString());
            }
        }
    }
}
