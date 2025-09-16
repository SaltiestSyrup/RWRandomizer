using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class MiscHooks
    {
        public static void ApplyHooks()
        {
            On.Menu.MainMenu.ExitButtonPressed += OnMainMenuExitButtonPressed;
            On.RegionGate.customKarmaGateRequirements += OnGateRequirements;
            On.PlayerProgression.ReloadLocksList += OnReloadLocksList;
            On.SaveState.setDenPosition += OnSetDenPosition;
            On.SaveState.GhostEncounter += EchoEncounter;
            On.MoreSlugcats.MoreSlugcats.OnInit += MoreSlugcats_OnInit;
            On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
            On.ScavengerAI.CollectScore_PhysicalObject_bool += OnScavengerAICollectScore;

            try
            {
                // In order for used passages to always save, they need to consider their consumed status
                Func<Func<WinState.EndgameTracker, bool>, WinState.EndgameTracker, bool> ProgressHook = (orig, tracker) =>
                {
                    return orig(tracker) || tracker.consumed;
                };

                _ = new Hook(typeof(WinState.BoolArrayTracker)
                    .GetProperty(nameof(WinState.BoolArrayTracker.AnyProgressToSave))
                    .GetGetMethod(),
                    ProgressHook);

                _ = new Hook(typeof(WinState.ListTracker)
                    .GetProperty(nameof(WinState.ListTracker.AnyProgressToSave))
                    .GetGetMethod(),
                    ProgressHook);

                _ = new Hook(typeof(WinState.FloatTracker)
                    .GetProperty(nameof(WinState.FloatTracker.AnyProgressToSave))
                    .GetGetMethod(),
                    ProgressHook);

                _ = new Hook(typeof(WinState.IntegerTracker)
                    .GetProperty(nameof(WinState.IntegerTracker.AnyProgressToSave))
                    .GetGetMethod(),
                    ProgressHook);

                IL.Menu.MainMenu.ctor += MainMenuCtorIL;
                IL.Menu.SlugcatSelectMenu.Update += SlugcatSelectMenuUpdateIL;
                IL.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectOverrideDeadCheckIL;
                IL.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectOverrideDeadCheckIL;
                IL.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += AddMSCRoomSpecificScriptIL;
                IL.MoreSlugcats.CutsceneArtificer.Update += CutsceneArtificerUpdateIL;
                IL.PlayerSessionRecord.AddEat += PlayerSessionRecord_AddEat;
                IL.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
                //IL.WinState.CreateAndAddTracker += WinStateCreateTrackerIL;
                IL.Spear.HitSomethingWithoutStopping += SpearmasterMushroomAddEat;
                IL.MoreSlugcats.GourmandMeter.UpdatePredictedNextItem += ILFoodQuestUpdateNextPredictedItem;
                IL.DeathPersistentSaveData.CanUseUnlockedGates += CanUseUnlockedGatesIL;
                IL.World.SpawnGhost += ILSpawnGhost;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.Menu.MainMenu.ExitButtonPressed -= OnMainMenuExitButtonPressed;
            On.RegionGate.customKarmaGateRequirements -= OnGateRequirements;
            On.PlayerProgression.ReloadLocksList -= OnReloadLocksList;
            On.SaveState.setDenPosition -= OnSetDenPosition;
            On.SaveState.GhostEncounter -= EchoEncounter;
            On.MoreSlugcats.MoreSlugcats.OnInit -= MoreSlugcats_OnInit;
            On.ItemSymbol.SpriteNameForItem -= ItemSymbol_SpriteNameForItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
            On.ScavengerAI.CollectScore_PhysicalObject_bool -= OnScavengerAICollectScore;

            IL.Menu.MainMenu.ctor -= MainMenuCtorIL;
            IL.Menu.SlugcatSelectMenu.Update -= SlugcatSelectMenuUpdateIL;
            IL.Menu.SlugcatSelectMenu.ContinueStartedGame -= SlugcatSelectOverrideDeadCheckIL;
            IL.Menu.SlugcatSelectMenu.UpdateStartButtonText -= SlugcatSelectOverrideDeadCheckIL;
            IL.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += AddMSCRoomSpecificScriptIL;
            IL.MoreSlugcats.CutsceneArtificer.Update -= CutsceneArtificerUpdateIL;
            IL.PlayerSessionRecord.AddEat -= PlayerSessionRecord_AddEat;
            IL.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
            //IL.WinState.CreateAndAddTracker -= WinStateCreateTrackerIL;
            IL.Spear.HitSomethingWithoutStopping -= SpearmasterMushroomAddEat;
            IL.MoreSlugcats.GourmandMeter.UpdatePredictedNextItem -= ILFoodQuestUpdateNextPredictedItem;
            IL.DeathPersistentSaveData.CanUseUnlockedGates -= CanUseUnlockedGatesIL;
            IL.World.SpawnGhost -= ILSpawnGhost;
        }

        /// <summary>
        /// Change button text on main menu to indicate randomizer is active
        /// </summary>
        private static void MainMenuCtorIL(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdstr("STORY")
                );

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldstr, "RANDOMIZER");
        }

        /// <summary>
        /// Cleanly disconnect any active AP connection before quitting application
        /// </summary>
        private static void OnMainMenuExitButtonPressed(On.Menu.MainMenu.orig_ExitButtonPressed orig, MainMenu self)
        {
            if (!ArchipelagoConnection.SocketConnected)
            {
                orig(self);
                return;
            }

            ArchipelagoConnection.Session.Socket.SocketClosed += QuitAfterDisconnect;
            ArchipelagoConnection.Disconnect(true);

            void QuitAfterDisconnect(string reason)
            {
                orig(self);
            }
        }

        /// <summary>
        /// Set a custom starting den when applicable
        /// </summary>
        private static void OnSetDenPosition(On.SaveState.orig_setDenPosition orig, SaveState self)
        {
            orig(self);

            if (RandoOptions.RandomizeSpawnLocation)
            {
                if (Plugin.RandoManager.customStartDen.Equals(""))
                {
                    Plugin.Log.LogError("Tried to set starting den while custom den unset");
                    Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText("ERROR: Failed to set correct starting den", Color.red));
                    return;
                }
                self.denPosition = Plugin.RandoManager.customStartDen;
            }
        }

        /// <summary>
        /// Stop game from going to statistics page instead of the game if there is a randomizer save.
        /// This hook is applied to both <see cref="SlugcatSelectMenu.UpdateStartButtonText"/> and <see cref="SlugcatSelectMenu.ContinueStartedGame"/>
        /// </summary>
        private static void SlugcatSelectOverrideDeadCheckIL(ILContext il)
        {
            ILCursor c = new(il);

            FieldInfo[] flags =
            [
                typeof(SlugcatSelectMenu).GetField(nameof(SlugcatSelectMenu.redIsDead)),
                typeof(SlugcatSelectMenu).GetField(nameof(SlugcatSelectMenu.artificerIsDead)),
                typeof(SlugcatSelectMenu).GetField(nameof(SlugcatSelectMenu.saintIsDead))
            ];

            // The check is the same for all 3 cases, so just loop through them
            for (int i = 0; i < flags.Length; i++)
            {
                ILLabel jump = null;
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(flags[i]),
                    x => x.MatchBrfalse(out jump)
                    );

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(OverrideIsDead);
                c.Emit(OpCodes.Brfalse, jump);
            }

            static bool OverrideIsDead(SlugcatSelectMenu menu)
            {
                SlugcatStats.Name slugcat = menu.slugcatPages[menu.slugcatPageIndex].slugcatNumber;
                int saveSlot = menu.manager.rainWorld.options.saveSlot;
                return !((Plugin.RandoManager is ManagerArchipelago) || SaveManager.IsThereASavedGame(slugcat, saveSlot));
            }
        }

        // TODO: Need explanation text for when start game button is greyed out
        /// <summary>
        /// Disable start game button if proper conditions are not met
        /// </summary>
        private static void SlugcatSelectMenuUpdateIL(ILContext il)
        {
            ILCursor c1 = new(il);

            // Check slugcat unlocked at 0362
            c1.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(SlugcatSelectMenu).GetMethod(nameof(SlugcatSelectMenu.SlugcatUnlocked)))
                );
            c1.Emit(OpCodes.Ldarg_0);
            c1.EmitDelegate(CanPlaySlugcat);

            static bool CanPlaySlugcat(bool orig, SlugcatSelectMenu self)
            {
                if (!RandoOptions.archipelago.Value) return orig;

                return ArchipelagoConnection.SocketConnected
                    && ArchipelagoConnection.Slugcat == self.colorFromIndex(self.slugcatPageIndex);
            }
        }

        /// <summary>
        /// Sets gate requirements based on currently found gate items
        /// </summary>
        private static void OnGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            orig(self);
            if (Plugin.RandoManager.isRandomizerActive)
            {
                self.karmaRequirements = Plugin.GetGateRequirement(self.room.abstractRoom.name);
            }
        }

        /// <summary>
        /// Sets gate requirements for map data based on currently found gate items
        /// </summary>
        private static void OnReloadLocksList(On.PlayerProgression.orig_ReloadLocksList orig, PlayerProgression self)
        {
            orig(self);
            if (Plugin.defaultGateRequirements.Count == 0)
            {
                foreach (string gate in self.karmaLocks)
                {
                    string[] split = Regex.Split(gate, " : ");

                    if (Plugin.defaultGateRequirements.ContainsKey(split[0])) continue;

                    Plugin.defaultGateRequirements.Add(split[0],
                    [
                        new(split[1]),
                        new(split[2])
                    ]);
                }
            }
        }

        /// <summary>
        /// Act as if Monk style karma gates is set if player YAML needs it
        /// </summary>
        private static void CanUseUnlockedGatesIL(ILContext il)
        {
            ILCursor c = new(il);

            ILLabel jump = null;
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(SlugcatStats.Name).GetField(nameof(SlugcatStats.Name.Yellow))),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchBrtrue(out jump)
                );

            c.EmitDelegate(() =>
            {
                if (Plugin.RandoManager is ManagerArchipelago)
                {
                    return ArchipelagoConnection.gateBehavior != Plugin.GateBehavior.OnlyKey;
                }
                return false;
            });
            c.Emit(OpCodes.Brtrue, jump);
        }

        /// <summary>
        /// Modify Echo spawning conditions based on setting
        /// </summary>
        private static void ILSpawnGhost(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(
                MoveType.Before,
                x => x.MatchCallOrCallvirt(typeof(World).GetProperty(nameof(World.game)).GetGetMethod()),
                x => x.MatchLdfld(typeof(RainWorldGame).GetField(nameof(RainWorldGame.rainWorld))),
                x => x.MatchLdfld(typeof(RainWorld).GetField(nameof(RainWorld.safariMode)))
                );

            // Modify echo spawning conditions based on settings
            c.Emit(OpCodes.Ldloc_0); // ghostID
            c.Emit(OpCodes.Ldloc_2); // flag for if echo should be spawned
            c.EmitDelegate(CustomEchoLogic);
            c.Emit(OpCodes.Stloc_2);

            c.Emit(OpCodes.Ldarg_0); // We interuppted after a ldarg_0, so put that back before we leave

            static bool CustomEchoLogic(World self, GhostWorldPresence.GhostID ghostID, bool spawnEcho)
            {
                // Use default logic if karma cap >= 5 or we're not in a mode where this applies
                if (!Plugin.RandoManager.isRandomizerActive
                    || Plugin.RandoManager is not ManagerArchipelago
                    || self.game.GetStorySession?.saveState.deathPersistentSaveData.karmaCap >= 4)
                {
                    return spawnEcho;
                }
                // Use default logic if in Expedition
                if (ModManager.Expedition && self.game.rainWorld.ExpeditionMode)
                {
                    return spawnEcho;
                }
                // Echoes that don't require karma should use default logic
                if (ghostID == GhostWorldPresence.GhostID.UW
                    || ghostID == GhostWorldPresence.GhostID.SB
                    || ModManager.MSC
                    && (ghostID == MoreSlugcatsEnums.GhostID.LC
                    || ghostID == MoreSlugcatsEnums.GhostID.MS))
                {
                    return spawnEcho;
                }

                // Create some locals to improve readability below
                int karma = self.game.GetStorySession.saveState.deathPersistentSaveData.karma;
                int cap = self.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap;
                bool reinforced = self.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma;
                int encounterIndex = self.game.GetStorySession.saveState.deathPersistentSaveData.ghostsTalkedTo.ContainsKey(ghostID) ?
                    self.game.GetStorySession.saveState.deathPersistentSaveData.ghostsTalkedTo[ghostID] : 0;
                bool isArtificer = ModManager.MSC && self.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer;
                bool isSaint = ModManager.MSC && self.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint;

                // How should we treat echoes when below 5 max karma?
                switch (ArchipelagoConnection.echoDifficulty)
                {
                    case ArchipelagoConnection.EchoLowKarmaDifficulty.Impossible:
                        // Disable spawn entirely
                        return false;
                    case ArchipelagoConnection.EchoLowKarmaDifficulty.WithFlower:
                        // Require a karma flower and at karma cap
                        if (isArtificer)
                        {
                            // This IS default logic for Artificer
                            return spawnEcho;
                        }
                        if (isSaint)
                        {
                            // Saint will have overwritten logic, so we need to add it again
                            return encounterIndex < 2 && karma == cap && reinforced;
                        }
                        return spawnEcho && reinforced;
                    case ArchipelagoConnection.EchoLowKarmaDifficulty.MaxKarma:
                        // Just require karma cap
                        if (isArtificer)
                        {
                            // Artificer will have set to false, logic must be redone
                            return encounterIndex == 1 && karma == cap;
                        }
                        if (isSaint)
                        {
                            // Saint will have overwritten logic, so we need to add it again
                            return encounterIndex < 2 && karma == cap;
                        }
                        // This is default logic for most slugcats, so just return orig
                        return spawnEcho;
                    case ArchipelagoConnection.EchoLowKarmaDifficulty.Vanilla:
                    default:
                        // Vanilla logic
                        return spawnEcho;
                }
            }
        }

        /// <summary>
        /// Reset karma increase from Echo encounter and give location
        /// </summary>
        private static void EchoEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
        {
            orig(self, ghost, rainWorld);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            self.deathPersistentSaveData.karmaCap = Plugin.RandoManager.CurrentMaxKarma;
            self.deathPersistentSaveData.karma = self.deathPersistentSaveData.karmaCap;

            Plugin.RandoManager.GiveLocation("Echo-" + ghost.value);

            Plugin.Singleton.rainWorld.progression.SaveProgressionAndDeathPersistentDataOfCurrentState(false, false);
        }

        /// <summary>
        /// Completely skip Artificer robo cutscene
        /// </summary>
        private static void AddMSCRoomSpecificScriptIL(ILContext il)
        {
            ILCursor c = new(il);
            // Check if room is GW_A25 at 018F
            c.GotoNext(x => x.MatchLdstr("GW_A25"));
            // After hasRobo load at 01D8
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(SaveState).GetField(nameof(SaveState.hasRobo)))
                );
            // Tell check that we always have robo
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_1);
        }

        private static bool hasSeenArtyStart = false;
        /// <summary>
        /// Skip Artificer intro cutscene if player has already seen it
        /// </summary>
        private static void CutsceneArtificerUpdateIL(ILContext il)
        {
            ILCursor c = new(il);
            // After myRobot load at 0018
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.myRobot)))
                );
            // Skip cutscene if we saw it instead of if robo is present
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => hasSeenArtyStart);

            // Jump further into method to dodge first call to Destroy()
            c.GotoNext(x => x.MatchLdsfld(typeof(CutsceneArtificer.Phase).GetField(nameof(CutsceneArtificer.Phase.End))));
            // Before call to Destroy at 043A
            c.GotoNext(
                MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(typeof(UpdatableAndDeletable).GetMethod(nameof(UpdatableAndDeletable.Destroy)))
                );
            c.MoveAfterLabels();
            // Mark cutscene as seen
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CutsceneArtificer>>(self =>
            {
                hasSeenArtyStart = true;
                RainWorldGame.ForceSaveNewDenLocation(self.room.game, "GW_A24", true);
                Plugin.Log.LogDebug("Saved cutscene status");
            });
        }

        /// <summary>
        /// Allow the food quest to appear on the HUD for any Slugcat.
        /// </summary>
        private static void HUD_InitSinglePlayerHud(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand))),
                x => x.MatchCallOrCallvirt(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))
                );
            c.MoveAfterLabels();
            c.EmitDelegate(YesItIsMeGourmand);
        }

        /// <summary>
        /// Allows all non-Gourmands to progress the food quest and collect the relevant checks.
        /// </summary>
        private static void PlayerSessionRecord_AddEat(ILContext il)
        {
            ILCursor c = new(il);
            for (int i = 0; i < 2; i++)
            {
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand))),
                    x => x.MatchCallOrCallvirt(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))
                    );
                c.MoveAfterLabels();
                c.EmitDelegate(YesItIsMeGourmand);
            }

            // Allow popcorn seeds to count as popcorn plants (argument interception at 0231).
            c.GotoNext(x => x.MatchCallOrCallvirt(typeof(WinState).GetMethod(nameof(WinState.GourmandPassageRequirementAtIndex))));  // 0221
            c.GotoNext(MoveType.After, x => x.MatchLdfld(typeof(AbstractPhysicalObject).GetField(nameof(AbstractPhysicalObject.type))));  // 022c

            AbstractPhysicalObject.AbstractObjectType TreatSeedsAsCobs(AbstractPhysicalObject.AbstractObjectType prev)
            {
                return (ModManager.MSC && prev == DLCSharedEnums.AbstractObjectType.Seed) ? AbstractPhysicalObject.AbstractObjectType.SeedCob : prev;
            }

            c.EmitDelegate(TreatSeedsAsCobs);
        }

        [Obsolete("Not currently applied")]
        private static void WinStateCreateTrackerIL(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.EndgameID).GetField(nameof(MoreSlugcatsEnums.EndgameID.Gourmand))),
                x => x.MatchCallOrCallvirt(typeof(ExtEnum<WinState.EndgameID>).GetMethod("op_Equality"))
            );
            c.MoveAfterLabels();

            c.EmitDelegate(YesItIsMeGourmand);
        }

        private static bool YesItIsMeGourmand(bool prev) => RandoOptions.UseFoodQuest || prev;

        /// <summary>
        /// Allow Spearmaster to eat mushrooms for the food quest, and detect spearing a neuron for Eat Neuron check
        /// </summary>
        private static void SpearmasterMushroomAddEat(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(x => x.MatchIsinst<Mushroom>());
            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(UpdatableAndDeletable).GetMethod(nameof(UpdatableAndDeletable.Destroy))));

            static void Delegate(Spear self, PhysicalObject obj)
            {
                // Previous IL has already checked whether it's a live Spearmaster needle.
                if (self.room?.game.GetStorySession?.playerSessionRecords is PlayerSessionRecord[] records)
                {
                    records[((self.thrownBy as Player).abstractCreature.state as PlayerState).playerNumber].AddEat(obj);
                }
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate(Delegate);

            // Detect spearing neuron for Eat_Neuron check
            c.GotoNext(x => x.MatchIsinst<OracleSwarmer>());
            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(UpdatableAndDeletable).GetMethod(nameof(UpdatableAndDeletable.Destroy))));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Spear>>((self) =>
            {
                IteratorHooks.EatenNeuron(self.thrownBy as Player);
            });
        }

        /// <summary>
        /// Filter the next predicted food quest item to be accessible to the current slugcat
        /// </summary>
        private static void ILFoodQuestUpdateNextPredictedItem(ILContext il)
        {
            ILCursor c = new(il);

            ILLabel jump = null;
            c.GotoNext(
                MoveType.After,
                //x => x.MatchLdfld(typeof(GourmandMeter).GetField(nameof(GourmandMeter.CurrentProgress))),
                x => x.MatchLdloc(2),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchLdcI4(0),
                x => x.MatchBgt(out jump)
                );

            c.Emit(OpCodes.Ldloc_2); // i
            c.EmitDelegate<Func<int, bool>>((i) =>
            {
                if (Plugin.RandoManager is ManagerArchipelago && ArchipelagoConnection.foodQuest == ArchipelagoConnection.FoodQuestBehavior.Expanded)
                {
                    return (ArchipelagoConnection.foodQuestAccessibility & (1L << i)) != 0;
                }
                // Returns whether or not the current slugcat can eat this food
                return Constants.SlugcatFoodQuestAccessibility[Plugin.RandoManager.currentSlugcat][i];
            });
            c.Emit(OpCodes.Brfalse, jump);
        }

        /// <summary>
        /// Create two arrays of food quest items for normal and expanded food quest.
        /// </summary>
        private static void MoreSlugcats_OnInit(On.MoreSlugcats.MoreSlugcats.orig_OnInit orig)
        {
            orig();
            // Order must match APWorld.
            WinState.GourmandTrackerData[] data =
            [
                new(AbstractPhysicalObject.AbstractObjectType.SeedCob, null),
                new(null, [CreatureTemplate.Type.Centipede, CreatureTemplate.Type.SmallCentipede]),
                new(null, [CreatureTemplate.Type.VultureGrub]),
                new(null, [CreatureTemplate.Type.SmallNeedleWorm, CreatureTemplate.Type.BigNeedleWorm]),
                new(null, [CreatureTemplate.Type.GreenLizard]),
                new(null, [CreatureTemplate.Type.BlueLizard]),
                new(null, [CreatureTemplate.Type.PinkLizard]),
                new(null, [CreatureTemplate.Type.WhiteLizard]),
                new(null, [CreatureTemplate.Type.RedLizard]),
                new(null, [DLCSharedEnums.CreatureTemplateType.SpitLizard]),
                new(null, [DLCSharedEnums.CreatureTemplateType.ZoopLizard]),
                new(null, [MoreSlugcatsEnums.CreatureTemplateType.TrainLizard]),
                new(null, [CreatureTemplate.Type.BigSpider]),
                new(null, [CreatureTemplate.Type.SpitterSpider]),
                new(null, [DLCSharedEnums.CreatureTemplateType.MotherSpider]),
                new(null, [CreatureTemplate.Type.Vulture]),
                new(null, [CreatureTemplate.Type.KingVulture]),
                new(null, [DLCSharedEnums.CreatureTemplateType.MirosVulture]),
                new(null, [CreatureTemplate.Type.LanternMouse]),
                new(null, [CreatureTemplate.Type.CicadaA, CreatureTemplate.Type.CicadaB]),
                new(null, [DLCSharedEnums.CreatureTemplateType.Yeek]),
                new(null, [CreatureTemplate.Type.DropBug]),
                new(null, [CreatureTemplate.Type.MirosBird]),
                new(null, [CreatureTemplate.Type.Scavenger, DLCSharedEnums.CreatureTemplateType.ScavengerElite, MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing]),
                new(null, [CreatureTemplate.Type.DaddyLongLegs, CreatureTemplate.Type.BrotherLongLegs, DLCSharedEnums.CreatureTemplateType.TerrorLongLegs, MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy]),
                new(null, [CreatureTemplate.Type.PoleMimic]),
                new(null, [CreatureTemplate.Type.TentaclePlant]),
                new(null, [CreatureTemplate.Type.BigEel]),
                new(null, [DLCSharedEnums.CreatureTemplateType.Inspector]),
            ];

            unexpanded = [.. WinState.GourmandPassageTracker];
            expanded = [.. unexpanded, .. data];
        }

        internal static WinState.GourmandTrackerData[] unexpanded;
        internal static WinState.GourmandTrackerData[] expanded;

        /// <summary>
        /// Add sprite name definitions for custom icon symbols
        /// </summary>
        private static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            return itemType.value switch
            {
                "KarmaFlower" => "Symbol_KarmaFlower",
                _ => orig(itemType, intData)
            };
        }

        /// <summary>
        /// Add color definitions for symbols that need them
        /// </summary>
        private static Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intdata)
        {
            return itemType.value switch
            {
                "SeedCob" => new Color(0.4117f, 0.1608f, 0.2275f),
                "KarmaFlower" => new Color(0.9059f, 0.8745f, 0.5647f),
                _ => orig(itemType, intdata)
            };
        }

        /// <summary>
        /// Restrict scavengers from taking certain objects from the ground on their own
        /// to avoid needed items for checks disappearing.
        /// </summary>
        private static int OnScavengerAICollectScore(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            int origValue = orig(self, obj, weaponFiltered);

            // Items are allowed to be a part of social events
            if (self.scavenger.room?.socialEventRecognizer.ItemOwnership(obj) is not null) return origValue;

            bool setNoValue = false;
            // Do not take unpicked flowers
            setNoValue |= RandoOptions.UseKarmaFlowerChecks
                && obj is KarmaFlower flower
                && flower.growPos is not null;
            // Do not take unique data pearls
            setNoValue |= RandoOptions.UsePearlChecks
                && obj is DataPearl pearl
                && DataPearl.PearlIsNotMisc(pearl.AbstractPearl.dataPearlType)
                && pearl.grabbedBy.Count == 0; // Allowed to value already carried pearls

            return setNoValue ? 0 : origValue;
        }
    }
}
