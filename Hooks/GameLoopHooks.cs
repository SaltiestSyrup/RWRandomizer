using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;

namespace RainWorldRandomizer
{
    public static class GameLoopHooks
    {
        public static void ApplyHooks()
        {
            On.ProcessManager.PostSwitchMainProcess += OnPostSwitchMainProcess;
            On.RainWorldGame.Update += OnRainWorldGameUpdate;
            On.RainWorldGame.ExitGame += OnExitGame;
            On.PlayerProgression.SaveToDisk += OnSaveGame;
            On.SaveState.GhostEncounter += OnGhostEncounter;
            On.SaveState.SessionEnded += OnSessionEnded;
            On.RainWorldGame.ctor += OnRainWorldGameCtor;
            On.HardmodeStart.Update += OnHardmodeStart;
            On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += OnOEEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.Update += OnLCEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.Update += OnSpearEndingUpdate;

            try
            {
                IL.RainWorldGame.ctor += RainWorldGameCtorIL;
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
            On.RainWorldGame.Update -= OnRainWorldGameUpdate;
            On.PlayerProgression.SaveToDisk -= OnSaveGame;
            On.SaveState.SessionEnded -= OnSessionEnded;
            On.RainWorldGame.ctor -= OnRainWorldGameCtor;
            On.HardmodeStart.Update -= OnHardmodeStart;
            On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update -= OnOEEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.Update -= OnLCEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.Update -= OnSpearEndingUpdate;
            IL.RainWorldGame.ctor -= RainWorldGameCtorIL;
            IL.WinState.CycleCompleted -= ILCycleCompleted;
        }

        /// <summary>
        /// Handle various events triggered by game process changing
        /// </summary>
        public static void OnPostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == ProcessManager.ProcessID.Game
                && (Plugin.RandoManager is null || !Plugin.RandoManager.isRandomizerActive))
            {
                // If we don't have a manager yet, create one
                Plugin.RandoManager ??= new ManagerVanilla();

                if (self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat is null)
                {
                    Plugin.Log.LogError("No slugcat selected");
                }
                else
                {
                    // Assign contents of Gourmand's tracker data
                    WinState.GourmandPassageTracker = RandoOptions.UseExpandedFoodQuest ? MiscHooks.expanded : MiscHooks.unexpanded;

                    try
                    {
                        Plugin.RandoManager.StartNewGameSession(self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat,
                            self.menuSetup.startGameCondition != ProcessManager.MenuSetup.StoryGameInitCondition.New);
                    }
                    catch (Exception e)
                    {
                        Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText("Failed to start randomizer game. More details found in BepInEx/LogOutput.log", UnityEngine.Color.red));
                        Plugin.Log.LogError("Encountered exception while starting game session");
                        Plugin.Log.LogError(e);
                    }
                }
            }

            if (ID == ProcessManager.ProcessID.MainMenu)
            {
                // Vanilla manager does not exist outside of the scope of gameplay. TODO: Eventually, neither will any other manager
                if (Plugin.RandoManager is ManagerVanilla) Plugin.RandoManager = null;
                if (Plugin.RandoManager is not null) Plugin.RandoManager.isRandomizerActive = false;
            }

            if (ID == ProcessManager.ProcessID.SleepScreen)
            {
                // Check for any new passages
                foreach (string check in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
                {
                    WinState.EndgameID id = new(check, false);

                    SaveState saveState = self.rainWorld.progression.currentSaveState ?? self.rainWorld.progression.starvedSaveState;

                    // Gourmand passage needs to be fetched with addIfMissing = true for non-Gourmand slugcats
                    if (ModManager.MSC && id == MoreSlugcatsEnums.EndgameID.Gourmand
                        && RandoOptions.UseFoodQuest)
                    {
                        WinState.GourFeastTracker gourTracker = saveState.deathPersistentSaveData.winState.GetTracker(id, true) as WinState.GourFeastTracker;

                        for (int i = 0; i < gourTracker.progress.Length; i++)
                        {
                            string type = WinState.GourmandPassageTracker[i].type == AbstractPhysicalObject.AbstractObjectType.Creature
                                ? WinState.GourmandPassageTracker[i].crits[0].value : WinState.GourmandPassageTracker[i].type.value;

                            if (gourTracker.progress[i] > 0)
                            {
                                Plugin.RandoManager.GiveLocation($"FoodQuest-{type}");
                            }
                        }
                        continue;
                    }

                    if (Plugin.RandoManager.IsLocationGiven($"Passage-{check}") == false // if location exists and is not given
                        && saveState.deathPersistentSaveData.winState.GetTracker(id, false) is not null)
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
                    }
                }
            }

            orig(self, ID);
        }

        /// <summary>
        /// Set story state flags at the start of each session
        /// </summary>
        public static void OnRainWorldGameCtor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            Plugin.Singleton.Game = self;

            if (!Plugin.RandoManager.isRandomizerActive || !self.IsStorySession) return;

            Plugin.UpdateKarmaLocks();
            self.GetStorySession.saveState.deathPersistentSaveData.karmaCap = Plugin.RandoManager.CurrentMaxKarma;

            // Ensure found state triggers are set
            self.GetStorySession.saveState.theGlow = Plugin.RandoManager.GivenNeuronGlow;
            self.GetStorySession.saveState.deathPersistentSaveData.theMark = Plugin.RandoManager.GivenMark;
            self.GetStorySession.saveState.hasRobo = Plugin.RandoManager.GivenRobo;
            self.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = Plugin.RandoManager.GivenPebblesOff;
            self.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = Plugin.RandoManager.GivenSpearPearlRewrite;
        }

        /// <summary>
        /// Spawn pending delivery items on session start if shelter delivery is set
        /// </summary>
        public static void RainWorldGameCtorIL(ILContext il)
        {
            ILCursor c = new(il);

            // Add shelter delivery after overworld is created, but before first room is realized
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdnull(),
                x => x.MatchStloc(6)
                );
            c.MoveAfterLabels();

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0); // num, is the room index of the spawn room
            c.EmitDelegate(SpawnDeliveryItems);

            static void SpawnDeliveryItems(RainWorldGame self, int roomIndex)
            {
                // Spawn pending items in spawn room
                if (!RandoOptions.ItemShelterDelivery) return;

                while (Plugin.RandoManager.itemDeliveryQueue.Count > 0)
                {
                    AbstractPhysicalObject obj = Plugin.ItemToAbstractObject(Plugin.RandoManager.itemDeliveryQueue.Dequeue(), self.world, self.world.GetAbstractRoom(roomIndex));
                    try
                    {
                        self.world.GetAbstractRoom(roomIndex).AddEntity(obj);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError("Failed to spawn object in starting room");
                        Plugin.Log.LogError(e);
                    }
                }
            }
        }

        /// <summary>
        /// Remove Hunter's starting macguffins
        /// </summary>
        public static void OnHardmodeStart(On.HardmodeStart.orig_Update orig, HardmodeStart self, bool eu)
        {
            if (!Plugin.RandoManager.isRandomizerActive
                || self.room.game.manager.fadeToBlack >= 1f
                || self.phase != HardmodeStart.Phase.Init
                || self.nshSwarmer is null)
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

            player ??= self.room.game.Players[0].realizedCreature as Player;

            player.objectInStomach = null;

            foreach (Creature.Grasp grasp in player.grasps)
            {
                if (grasp?.grabbed?.abstractPhysicalObject?.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
                {
                    grasp.grabbed.AllGraspsLetGoOfThisObject(true);
                    self.room.game.GetStorySession.RemovePersistentTracker(grasp.grabbed.abstractPhysicalObject);
                    grasp.grabbed.abstractPhysicalObject.Destroy();
                    self.nshSwarmer = null;
                }
            }
        }

        /// <summary>
        /// Various processes that need to be handled in an update loop
        /// </summary>
        public static void OnRainWorldGameUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.GamePaused || !self.processActive) return;

            // Display any pending notifications
            else if (Plugin.Singleton.notifQueue.Count > 0)
            {
                if (RandoOptions.DisableNotificationQueue)
                {
                    Plugin.Singleton.notifQueue.Dequeue();
                }
                else if (RandoOptions.legacyNotifications.Value)
                {
                    Plugin.Singleton.DisplayLegacyNotification();
                }
                else if (HudExtension.CurrentChatLog is not null)
                {
                    HudExtension.CurrentChatLog.AddMessage(Plugin.Singleton.notifQueue.Dequeue());
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
                if (RandoOptions.UseShelterChecks
                    && $"Shelter-{currentRoom.abstractRoom.name.ToUpper()}" is string checkName
                    && Plugin.RandoManager.LocationExists(checkName))
                {
                    Plugin.RandoManager.GiveLocation(checkName);
                }

                for (int j = 0; j < currentRoom.updateList.Count; j++)
                {
                    if (currentRoom.updateList[j] is DataPearl pearl)
                    {
                        string locName = $"Pearl-{pearl.AbstractPearl.dataPearlType.value}";

                        if (Plugin.RandoManager is ManagerArchipelago)
                        {
                            // Check if this pearl matching the current region is valid
                            if (Plugin.RandoManager.LocationExists(locName + $"-{currentRoom.abstractRoom.name.Substring(0, 2)}"))
                            {
                                locName += $"-{currentRoom.abstractRoom.name.Substring(0, 2)}";
                            }
                            else
                            {
                                // More costly lookup to find where this pearl comes from
                                foreach (var region in self.rainWorld.regionDataPearls)
                                {
                                    if (region.Value.Contains(pearl.AbstractPearl.dataPearlType)
                                        && Plugin.RandoManager.LocationExists(locName + $"-{region.Key.ToUpperInvariant()}"))
                                    {
                                        locName += $"-{region.Key.ToUpperInvariant()}";
                                        break;
                                    }
                                }
                            }
                        }

                        Plugin.RandoManager.GiveLocation(locName);
                    }
                }
            }
        }

        /// <summary>
        /// Save randomizer state when game is saved
        /// </summary>
        public static bool OnSaveGame(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            bool origSuccess = orig(self, saveCurrentState, saveMaps, saveMiscProg);

            if (Plugin.RandoManager?.isRandomizerActive is true)
            {
                Plugin.RandoManager.SaveGame(saveCurrentState);
            }

            return origSuccess;
        }

        /// <summary>
        /// Update item delivery queue on session end
        /// </summary>
        private static void OnSessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            orig(self, game, survived, newMalnourished);
            SaveDiskUpdateItemQueue(survived, self.malnourished);
        }

        /// <summary>
        /// Update item delivery queue on quit to menu
        /// </summary>
        private static void OnExitGame(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
        {
            orig(self, asDeath, asQuit);
            SaveDiskUpdateItemQueue(false, false);
        }

        /// <summary>
        /// Update item delivery queue on Echo encounter
        /// </summary>
        private static void OnGhostEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
        {
            orig(self, ghost, rainWorld);
            SaveDiskUpdateItemQueue(false, false);
        }

        private static void SaveDiskUpdateItemQueue(bool completeCycle, bool malnourished)
        {
            // If we did not finish the cycle (death, quit out, etc.), restore current queue from saved backup
            if (!completeCycle)
            {
                Plugin.RandoManager.itemDeliveryQueue = new(Plugin.RandoManager.lastItemDeliveryQueue);
                return;
            }

            // If we survived without starving this cycle, paste current queue to saved backup
            if (!malnourished)
            {
                Plugin.RandoManager.lastItemDeliveryQueue = new(Plugin.RandoManager.itemDeliveryQueue);
            }
        }

        /// <summary>
        /// Hacking for passage progress changes under certain settings
        /// </summary>
        public static void ILCycleCompleted(ILContext il)
        {
            ILCursor c = new(il);

            // Fake the "Passage Progress without Survivor" option if needed
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MMF).GetField(nameof(MMF.cfgSurvivorPassageNotRequired))),
                x => x.MatchCallOrCallvirt(typeof(Configurable<bool>).GetProperty(nameof(Configurable<bool>.Value)).GetGetMethod())
                );

            c.EmitDelegate<Func<bool, bool>>((config) =>
            {
                if (Plugin.RandoManager is ManagerArchipelago)
                {
                    return ArchipelagoConnection.PPwS != ArchipelagoConnection.PPwSBehavior.Disabled;
                }
                return config;
            });

            // Conditionally remove the hardcoded Survivor checks on other passages
            // (branch interception at 049f, 04ed, 0570, 0599, 05ea, 06e8, 07bb).
            c.GotoNext(x => x.MatchRet());  // 0480
            for (int i = 0; i < 7; i++)
            {
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdloc(12),
                    x => x.MatchCallOrCallvirt(typeof(WinState.EndgameTracker).GetProperty(nameof(WinState.EndgameTracker.GoalAlreadyFullfilled)).GetGetMethod())
                    );
                static bool BypassHardcodedSurvivorRequirement(bool prev) =>
                    prev || (Plugin.RandoManager is ManagerArchipelago && ArchipelagoConnection.PPwS == ArchipelagoConnection.PPwSBehavior.Bypassed);
                c.EmitDelegate(BypassHardcodedSurvivorRequirement);
            }
        }

        /// <summary>
        /// Detect Outer Expanse ending trigger
        /// </summary>
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

        /// <summary>
        /// Detect Artificer Metropolis ending trigger
        /// </summary>
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

        /// <summary>
        /// Detect Spearmaster Sky Islands ending trigger
        /// </summary>
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
    }
}
