using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
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
            On.RegionState.AdaptRegionStateToWorld -= OnAdaptRegionStateToWorld;
            IL.WinState.CycleCompleted -= ILCycleCompleted;
        }

        public static void OnPostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == ProcessManager.ProcessID.Game && !Plugin.isRandomizerActive)
            {
                if (self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat == null)
                {
                    Plugin.Log.LogError("No slugcat selected");
                }
                else
                {
                    try
                    {
                        Generation.StartNewGameSession(self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat,
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
                Plugin.isRandomizerActive = false;
            }

            if (ID == ProcessManager.ProcessID.SleepScreen)
            {
                // Check for any new passages
                foreach (string check in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
                {
                    WinState.EndgameID id = new WinState.EndgameID(check, false);

                    if (Generation.CheckBlacklists.ContainsKey(Plugin.Singleton.currentSlugcat)
                        && !Generation.CheckBlacklists[Plugin.Singleton.currentSlugcat].Contains($"Passage-{check}")
                        && !Plugin.Singleton.IsCheckGiven($"Passage-{check}")
                        && self.rainWorld.progression.currentSaveState.deathPersistentSaveData.winState.GetTracker(id, false) != null)
                    {
                        WinState.EndgameTracker tracker = self.rainWorld.progression.currentSaveState.deathPersistentSaveData.winState.GetTracker(id, false);

                        // Normal Passages
                        if (tracker.GoalFullfilled)
                        {
                            Plugin.Singleton.GiveCheck($"Passage-{check}");
                        }

                        // Gourmand Food Quest
                        if (tracker is WinState.GourFeastTracker gourTracker)
                        {
                            for (int i = 0; i < gourTracker.progress.Length; i++)
                            {
                                string type = WinState.GourmandPassageTracker[i].type == AbstractPhysicalObject.AbstractObjectType.Creature
                                    ? WinState.GourmandPassageTracker[i].crits[0].value : WinState.GourmandPassageTracker[i].type.value;

                                if (gourTracker.progress[i] > 0
                                    && !Plugin.Singleton.IsCheckGiven($"FoodQuest-{type}"))
                                {
                                    Plugin.Singleton.GiveCheck($"FoodQuest-{type}");
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

            if (!Plugin.isRandomizerActive || !self.IsStorySession) return;

            self.rainWorld.progression.ReloadLocksList();
            self.GetStorySession.saveState.deathPersistentSaveData.karmaCap = Plugin.Singleton.currentMaxKarma;

            // The glow is not death persistent, so I have to make sure it stays applied
            self.GetStorySession.saveState.theGlow = Plugin.Singleton.givenNeuronGlow;
            self.GetStorySession.saveState.deathPersistentSaveData.theMark = Plugin.Singleton.givenMark;
        }

        // Remove Hunter's starting macguffins
        public static void OnHardmodeStart(On.HardmodeStart.orig_Update orig, HardmodeStart self, bool eu)
        {
            if (!Plugin.isRandomizerActive
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
                    grasp.grabbed.abstractPhysicalObject.Destroy();
                }
            }
        }

        public static void Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);

            // Display any pending notifications
            if (Plugin.Singleton.notifQueue.Count > 0)
            {
                // If we have any pending messages and are in the actual game loop
                if (self.session.Players[0]?.realizedCreature != null
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
            if (!Plugin.isRandomizerActive) return;

            // Pearl Detection
            for (int i = 0; i < self.Players.Count; i++)
            {
                if (self.Players[i] != null
                    && self.Players[i].realizedCreature != null)
                {
                    // Applying glow effect if unlock has been given
                    if (Plugin.Singleton.givenNeuronGlow && !(self.Players[i].realizedCreature as Player).glowing)
                    {
                        (self.Players[i].realizedCreature as Player).glowing = true;
                    }

                    // Detect pearl stored in shelter (stolen from expedition code)
                    if (self.Players[i].realizedCreature.room != null
                        && self.Players[i].realizedCreature.room.abstractRoom.shelter)
                    {
                        for (int j = 0; j < self.Players[i].realizedCreature.room.updateList.Count; j++)
                        {
                            if (self.Players[i].realizedCreature.room.updateList[j] is DataPearl pearl)
                            {
                                // Check if this pearl has either already been checked or is not a valid target
                                string pearlID = (from x in Generation.randomizerKey
                                                  where Regex.Split(x.Key, "-").Length > 1
                                                  && Regex.Split(x.Key, "-")[0] == "Pearl"
                                                  && Regex.Split(x.Key, "-")[1] == pearl.AbstractPearl.dataPearlType.value
                                                  select x.Key).DefaultIfEmpty("").Single();

                                if (pearlID != "" && !Plugin.Singleton.IsCheckGiven(pearlID))
                                {
                                    Plugin.Singleton.GiveCheck(pearlID);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool OnSaveGame(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            if (Plugin.isRandomizerActive)
            {
                SaveManager.WriteSavedGameToFile(Generation.randomizerKey, Plugin.Singleton.currentSlugcat, Plugin.Singleton.rainWorld.options.saveSlot);

                if (saveCurrentState)
                {
                    SaveManager.WriteItemQueueToFile(Plugin.Singleton.itemDeliveryQueue, Plugin.Singleton.currentSlugcat, Plugin.Singleton.rainWorld.options.saveSlot);
                }
            }

            return orig(self, saveCurrentState, saveMaps, saveMiscProg);
        }

        public static void OnSessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            orig(self, game, survived, newMalnourished);
            if (!Plugin.isRandomizerActive) return;

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
