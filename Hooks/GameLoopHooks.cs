using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public static class GameLoopHooks
    {
        private const int SHELTER_ITEMS_PER_CYCLE = 5;

        public static void ApplyHooks()
        {
            On.ProcessManager.PostSwitchMainProcess += OnPostSwitchMainProcess;
            On.RainWorldGame.Update += OnRainWorldGameUpdate;
            On.RainWorldGame.ExitGame += OnExitGame;
            On.RainWorldGame.ContinuePaused += OnContinuePaused;
            On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += OnWorldLoader_ctor;
            On.PlayerProgression.SaveToDisk += OnSaveGame;
            On.SaveState.GhostEncounter += OnGhostEncounter;
            On.SaveState.SessionEnded += OnSessionEnded;
            On.RainWorldGame.ctor += OnRainWorldGameCtor;
            On.HardmodeStart.Update += OnHardmodeStart;
            On.Room.Loaded += AddRoomSpecificScript;
            On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += OnOEEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.Update += OnLCEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.Update += OnSpearEndingUpdate;

            try
            {
                IL.RainWorldGame.ctor += RainWorldGameCtorIL;
                IL.WinState.CycleCompleted += ILCycleCompleted;
                IL.Menu.SlideShow.ctor += SlideShow_ctor;
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
            On.RainWorldGame.ContinuePaused -= OnContinuePaused;
            On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues -= OnWorldLoader_ctor;
            On.PlayerProgression.SaveToDisk -= OnSaveGame;
            On.SaveState.SessionEnded -= OnSessionEnded;
            On.RainWorldGame.ctor -= OnRainWorldGameCtor;
            On.HardmodeStart.Update -= OnHardmodeStart;
            On.Room.Loaded -= AddRoomSpecificScript;
            On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update -= OnOEEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.Update -= OnLCEndingScriptUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.Update -= OnSpearEndingUpdate;
            IL.RainWorldGame.ctor -= RainWorldGameCtorIL;
            IL.WinState.CycleCompleted -= ILCycleCompleted;
            IL.Menu.SlideShow.ctor -= SlideShow_ctor;
        }

        /// <summary>
        /// Handle various events triggered by game process changing
        /// </summary>
        public static void OnPostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == ProcessManager.ProcessID.Game
                && (Plugin.RandoManager is null || !Plugin.RandoManager.isRandomizerActive))
            {
                // If AP is connected, use AP manager
                if (ArchipelagoConnection.SocketConnected) Plugin.RandoManager = new ManagerArchipelago();

                // Default to vanilla manager
                Plugin.RandoManager ??= new ManagerVanilla();

                if (self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat is null)
                {
                    Plugin.Log.LogError("No slugcat selected");
                }
                else
                {
                    // Assign contents of Gourmand's tracker data
                    WinState.GourmandPassageTracker = RandoOptions.UseExpandedFoodQuest ? Constants.GourmandPassageTrackerExpanded : Constants.GourmandPassageTrackerOrig;

                    try
                    {
                        Plugin.RandoManager.StartNewGameSession(self.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat,
                            self.menuSetup.startGameCondition != ProcessManager.MenuSetup.StoryGameInitCondition.New);

                        // Have AP manager grab the first item packet (the one with the full inventory) right away
                        if (Plugin.RandoManager is ManagerArchipelago managerAP) managerAP.TryAquireNextItemPacket();
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

            bool anySleepScreen = ID == ProcessManager.ProcessID.SleepScreen
                || ID == ProcessManager.ProcessID.GhostScreen
                || ID == ProcessManager.ProcessID.KarmaToMaxScreen
                || (ModManager.MSC && ID == MoreSlugcatsEnums.ProcessID.VengeanceGhostScreen);
            if (anySleepScreen)
            {
                // Check for any new passages
                SaveState saveState = self.rainWorld.progression.currentSaveState ?? self.rainWorld.progression.starvedSaveState;
                foreach (string check in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
                {
                    WinState.EndgameID id = new(check, false);

                    // Gourmand passage needs to be fetched with addIfMissing = true for non-Gourmand slugcats
                    if (ModManager.MSC && id == MoreSlugcatsEnums.EndgameID.Gourmand
                        && RandoOptions.UseFoodQuest)
                    {
                        WinState.GourFeastTracker gourTracker = saveState.deathPersistentSaveData.winState.GetTracker(id, true) as WinState.GourFeastTracker;

                        bool fullCompletion = true;
                        for (int i = 0; i < gourTracker.progress.Length; i++)
                        {
                            string type = WinState.GourmandPassageTracker[i].type == AbstractPhysicalObject.AbstractObjectType.Creature
                                ? WinState.GourmandPassageTracker[i].crits[0].value : WinState.GourmandPassageTracker[i].type.value;

                            if (gourTracker.progress[i] > 0) Plugin.RandoManager.GiveLocation($"FoodQuest-{type}");
                            else if (Plugin.RandoManager.LocationExists($"FoodQuest-{type}")) fullCompletion = false;
                        }

                        // Check for Food Quest goal
                        if (fullCompletion) (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.FoodQuest);

                        continue;
                    }

                    if (saveState.deathPersistentSaveData.winState.GetTracker(id, false) is WinState.EndgameTracker tracker)
                    {
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
                                if (prog)
                                {
                                    progressCount++;
                                    Plugin.RandoManager.GiveLocation($"Wanderer-{progressCount}");
                                }
                            }
                        }
                    }
                }

                if (ModManager.Watcher) WatcherIntegration.CheckDetection.Hooks.DetectFixedWarpPointAndRotSpread(saveState);
            }

            orig(self, ID);

            // Check for Pilgrim goal (Echo count is updated during the orig call above).
            // Condition is not exactly the same as pilgrim passage.
            // This checks for any echoes, so Saint can sub in the MS echo if they choose.
            if (anySleepScreen)
            {
                SaveState saveState = self.rainWorld.progression.currentSaveState ?? self.rainWorld.progression.starvedSaveState;
                // Saint and Artificer have one more echo to get
                int echoesNeeded = Plugin.RandoManager.currentSlugcat.value == "Saint"
                    || Plugin.RandoManager.currentSlugcat.value == "Artificer" ? 7 : 6;
                if (saveState.deathPersistentSaveData.ghostsTalkedTo.Count(kvp => kvp.Value >= 2) >= echoesNeeded)
                    (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.Pilgrim);
            }
        }

        /// <summary>
        /// Set story state flags at the start of each session
        /// </summary>
        public static void OnRainWorldGameCtor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            Plugin.Singleton.Game = self;
            orig(self, manager);

            if (!Plugin.RandoManager.isRandomizerActive || !self.IsStorySession) return;

            Plugin.UpdateKarmaLocks();
            self.GetStorySession.saveState.deathPersistentSaveData.karmaCap = Plugin.RandoManager.CurrentMaxKarma;
            if (Plugin.RandoManager.currentSlugcat == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
                WatcherIntegration.Items.UpdateRipple();

            // Ensure found state triggers are set
            self.GetStorySession.saveState.theGlow = Plugin.RandoManager.GivenNeuronGlow;
            self.GetStorySession.saveState.deathPersistentSaveData.theMark = Plugin.RandoManager.GivenMark;
            self.GetStorySession.saveState.hasRobo = Plugin.RandoManager.GivenRobo;
            self.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = Plugin.RandoManager.GivenPebblesOff;
            self.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = Plugin.RandoManager.GivenSpearPearlRewrite;
            self.GetStorySession.saveState.miscWorldSaveData.hasRippleEggWarpAbility = Plugin.RandoManager.GivenRippleEggWarp;

            // If we're spawning in a room with a warp target (likely from Watcher return home), set positon there
            TryMovePlayersToDynamicWarpTarget(self);
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

                for (int i = 0; i < SHELTER_ITEMS_PER_CYCLE; i++)
                {
                    if (Plugin.RandoManager.itemDeliveryQueue.Count == 0) break;

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
                || self.phase != HardmodeStart.Phase.Init)
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
                    grasp.grabbed.Destroy();
                }
            }
        }

        private static void OnWorldLoader_ctor(On.WorldLoader.orig_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues orig,
            WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, SlugcatStats.Timeline timelinePosition, bool singleRoomWorld,
            string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            orig(self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues);
            worldName = worldName.ToUpperInvariant(); // Why are rotted regions in lowercase this game is evil

            if (Plugin.RandoManager is null || !RandoOptions.ColorPickupsWithHints) return;

            // Get all preview-able locations in the new region
            static bool IsColorable(LocationInfo l) => l.IsToken
                || l.kind == LocationInfo.LocationKind.Pearl
                || l.kind == LocationInfo.LocationKind.Shelter
                || l.kind == LocationInfo.LocationKind.Flower;
            LocationInfo[] locations =
            [.. Plugin.RandoManager.GetLocations().Where(l => (IsColorable(l) && l.region == worldName) || l.IsPassage)];

            // If these locations have been fetched already, no need to ask server for them
            if (locations.Select(l => l.internalName).All(SaveManager.ScoutedLocations.ContainsKey)) return;
            Plugin.Log.LogInfo($"Scouting {locations.Length} locations in new region: {worldName}...");

            // Ask server for all the items at these locations
            Task<Dictionary<long, Archipelago.MultiClient.Net.Models.ScoutedItemInfo>> scoutingTask =
                ArchipelagoConnection.Session.Locations.ScoutLocationsAsync(false, [.. locations.Select(l => l.archipelagoID)]);

            // Define callback for when we get the scouted items
            Task.Factory.StartNew(() =>
            {
                try
                {
                    SaveManager.AddScoutedLocations(locations.ToDictionary(l => l.internalName,
                        l => scoutingTask.Result.TryGetValue(l.archipelagoID, out var value)
                            ? value.Flags : Archipelago.MultiClient.Net.Enums.ItemFlags.None));
                    Plugin.Log.LogInfo($"Scouting complete");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Exception while scouting locations: {e}");
                }
            });
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

            // Read and apply a single queued item packet every frame
            if (Plugin.RandoManager is ManagerArchipelago APManager) APManager.TryAquireNextItemPacket();

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

                        Plugin.RandoManager.GiveLocation(locName);
                    }
                }
            }
        }

        private static void OnContinuePaused(On.RainWorldGame.orig_ContinuePaused orig, RainWorldGame self)
        {
            orig(self);

            // Remove and spawn items selected while paused
            if ((MenuExtension.PendingItemsDisplay?.selectedIndices.Count ?? 0) > 0)
            {
                List<Unlock.Item> items = [.. Plugin.RandoManager.itemDeliveryQueue];
                MenuExtension.PendingItemsDisplay.selectedIndices.Sort();
                MenuExtension.PendingItemsDisplay.selectedIndices.Reverse();
                foreach (int index in MenuExtension.PendingItemsDisplay.selectedIndices)
                {
                    // Try to spawn the item, and remove from queue if successful
                    if (self.TryGivePlayerItem(items[index])) items.RemoveAt(index);
                }
                Plugin.RandoManager.itemDeliveryQueue = new(items);
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

        private static void SlideShow_ctor(ILContext il)
        {
            ILCursor c = new(il);

            FieldInfo[] relevantSlideShowIDs = [.. new string[]
            {
                "DreamSpinningTop",
                "DreamRot",
                "DreamVoidWeaver",
                "DreamTerrace",
                "EndingVoidBath"
            }.Select(s => typeof(Watcher.WatcherEnums.SlideShowID).GetField(s))];

            foreach (FieldInfo f in relevantSlideShowIDs)
            {
                c.GotoNext(x => x.MatchLdsfld(f));
                c.GotoNext(MoveType.After, x => x.MatchStfld(typeof(Menu.SlideShow).GetField(nameof(Menu.SlideShow.processAfterSlideShow))));

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SetStartGameCondition);
            }

            static void SetStartGameCondition(Menu.SlideShow self)
            {
                self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
            }
        }

        private static void AddRoomSpecificScript(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (Plugin.RandoManager is null) return;

            if (ModManager.Watcher) WatcherIntegration.RoomSpecificScript.AddRoomSpecificScript(self);
        }

        /// <summary>
        /// Detect Outer Expanse ending trigger
        /// </summary>
        public static void OnOEEndingScriptUpdate(On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.orig_Update orig, MSCRoomSpecificScript.OE_GourmandEnding self, bool eu)
        {
            orig(self, eu);

            // Check for completion via Outer Expanse
            if (self.endTrigger)
            {
                (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SlugTree);
            }
        }

        /// <summary>
        /// Detect Artificer Metropolis ending trigger
        /// </summary>
        public static void OnLCEndingScriptUpdate(On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.orig_Update orig, MSCRoomSpecificScript.LC_FINAL self, bool eu)
        {
            orig(self, eu);

            // Check for completion via killing Chieftain scavenger
            if (self.endingTriggered)
            {
                (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.ScavKing);
            }
        }

        /// <summary>
        /// Detect Spearmaster Sky Islands ending trigger
        /// </summary>
        public static void OnSpearEndingUpdate(On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterEnding.orig_Update orig, MSCRoomSpecificScript.SpearmasterEnding self, bool eu)
        {
            orig(self, eu);

            // Check for completion via delivering Spearmaster's pearl to Comms array
            if (self.SMEndingPhase == MSCRoomSpecificScript.SpearmasterEnding.SMEndingState.PEARLDATA)
            {
                (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.Messenger);
            }
        }

        public static bool TryGivePlayerItem(this RainWorldGame game, Unlock.Item item)
        {
            // Find first living player to give to
            if (game.FirstAlivePlayer.realizedCreature is not Player player)
            {
                Plugin.Log.LogError("Failed to spawn item for player, no valid player found");
                return false;
            }

            // Try to create the object
            AbstractPhysicalObject obj = Plugin.ItemToAbstractObject(item, game.world, player.room.abstractRoom);
            try
            {
                player.room.abstractRoom.AddEntity(obj);
                obj.pos = player.abstractCreature.pos;
                obj.RealizeInRoom();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed to spawn item for player, invalid item?");
                Plugin.Log.LogError(e);
                return false;
            }

            player.room.PlaySound(SoundID.Slugcat_Regurgitate_Item, player.mainBodyChunk);

            // Set position and try to grab
            obj.realizedObject.firstChunk.HardSetPosition(player.bodyChunks[0].pos);
            if (!player.CanIPickThisUp(obj.realizedObject)
                || (player.Grabability(obj.realizedObject) >= Player.ObjectGrabability.TwoHands
                    && player.grasps.Any(g => g is not null)))
            {
                return true;
            }

            player.SlugcatGrab(obj.realizedObject, player.FreeHand());
            return true;
        }

        /// <summary>
        /// Attempts to move all players to the position of a <see cref="PlacedObject.Type.DynamicWarpTarget"/>, if one is present.
        /// </summary>
        /// <returns>True if a dynamic warp target was found, otherwise False.</returns>
        public static bool TryMovePlayersToDynamicWarpTarget(this RainWorldGame game)
        {
            foreach (PlacedObject po in game.FirstAlivePlayer.Room.realizedRoom?.roomSettings.placedObjects)
            {
                if (po.type != PlacedObject.Type.DynamicWarpTarget) continue;

                foreach (AbstractCreature player in game.Players)
                {
                    player.pos.Tile = new RWCustom.IntVector2((int)(po.pos.x / 20f), (int)(po.pos.y / 20f));
                    player.realizedCreature?.firstChunk.HardSetPosition(po.pos);
                }
                return true;
            }

            return false;
        }
    }
}
