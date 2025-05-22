using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class MiscHooks
    {
        public static void ApplyHooks()
        {
            On.RegionGate.customKarmaGateRequirements += OnGateRequirements;
            On.PlayerProgression.ReloadLocksList += OnReloadLocksList;
            On.SaveState.setDenPosition += OnSetDenPosition;
            On.SaveState.GhostEncounter += EchoEncounter;
            On.MoreSlugcats.MoreSlugcats.OnInit += MoreSlugcats_OnInit;
            //On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;

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
            On.RegionGate.customKarmaGateRequirements -= OnGateRequirements;
            On.PlayerProgression.ReloadLocksList -= OnReloadLocksList;
            On.SaveState.setDenPosition -= OnSetDenPosition;
            On.SaveState.GhostEncounter -= EchoEncounter;
            On.MoreSlugcats.MoreSlugcats.OnInit -= MoreSlugcats_OnInit;
            //On.ItemSymbol.SpriteNameForItem -= ItemSymbol_SpriteNameForItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;

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
            ILCursor c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdstr("STORY")
                );

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldstr, Plugin.RandoManager == null ? "STORY" : "RANDOMIZER");
        }

        /// <summary>
        /// Set a custom starting den when applicable
        /// </summary>
        private static void OnSetDenPosition(On.SaveState.orig_setDenPosition orig, SaveState self)
        {
            orig(self);

            if (Options.RandomizeSpawnLocation)
            {
                if (Plugin.RandoManager.customStartDen.Equals("NONE"))
                {
                    Plugin.Log.LogError("Tried to set starting den while custom den unset");
                    Plugin.Singleton.notifQueue.Enqueue("ERROR: Failed to set correct starting den");
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
            ILCursor c = new ILCursor(il);

            FieldInfo[] flags = new FieldInfo[3]
            {
                typeof(SlugcatSelectMenu).GetField(nameof(SlugcatSelectMenu.redIsDead)),
                typeof(SlugcatSelectMenu).GetField(nameof(SlugcatSelectMenu.artificerIsDead)),
                typeof(SlugcatSelectMenu).GetField(nameof(SlugcatSelectMenu.saintIsDead))
            };

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
                c.EmitDelegate<Func<SlugcatSelectMenu, bool>>(OverrideIsDead);
                c.Emit(OpCodes.Brfalse, jump);
            }

            bool OverrideIsDead(SlugcatSelectMenu menu)
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
            try
            {
                ILCursor c1 = new ILCursor(il);

                ILLabel resultJump = null;
                c1.GotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(SlugcatSelectMenu).GetField(nameof(SlugcatSelectMenu.slugcatPageIndex))),
                    x => x.MatchCallOrCallvirt(typeof(SlugcatSelectMenu).GetMethod(nameof(SlugcatSelectMenu.colorFromIndex))),
                    x => x.MatchCallOrCallvirt(typeof(SlugcatSelectMenu).GetMethod(nameof(SlugcatSelectMenu.SlugcatUnlocked))),
                    x => x.MatchLdcI4(0),
                    x => x.MatchCeq(),
                    x => x.MatchBr(out resultJump)
                    );

                // Move the label a step back, to ldc.i4.1
                ILCursor c2 = new ILCursor(il);
                c2.GotoLabel(resultJump, MoveType.Before);
                c2.Index--;
                resultJump = c2.MarkLabel();

                c1.Emit(OpCodes.Ldarg_0);
                // When AP is enabled, start game button should only be available if AP is connected and the correct slugcat is chosen
                c1.EmitDelegate<Func<SlugcatSelectMenu, bool>>((self) =>
                {
                    return !Options.archipelago.Value
                        || (Plugin.RandoManager is ManagerArchipelago manager
                            && manager.locationsLoaded
                            && manager.currentSlugcat == self.colorFromIndex(self.slugcatPageIndex)
                            && ModManager.MSC == ArchipelagoConnection.IsMSC);
                });
                c1.Emit(OpCodes.Brfalse, resultJump);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for SlugcatSelectMenuUpdate");
                Plugin.Log.LogError(e);
            }
        }

        /// <summary>
        /// Sets gate requirements based on currently found gate items
        /// </summary>
        private static void OnGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            if (Plugin.RandoManager.isRandomizerActive)
            {
                self.karmaRequirements = Plugin.GetGateRequirement(self.room.abstractRoom.name);
            }
            orig(self);
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

                    Plugin.defaultGateRequirements.Add(split[0], new RegionGate.GateRequirement[2]
                    {
                        new RegionGate.GateRequirement(split[1]),
                        new RegionGate.GateRequirement(split[2])
                    });
                }
            }
        }

        /// <summary>
        /// Act as if Monk style karma gates is set if player YAML needs it
        /// </summary>
        private static void CanUseUnlockedGatesIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILLabel jump = null;
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(SlugcatStats.Name).GetField(nameof(SlugcatStats.Name.Yellow))),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchBrtrue(out jump)
                );

            c.EmitDelegate<Func<bool>>(() =>
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
            ILCursor c = new ILCursor(il);

            c.GotoNext(
                MoveType.Before,
                x => x.MatchCallOrCallvirt(typeof(World).GetProperty(nameof(World.game)).GetGetMethod()),
                x => x.MatchLdfld(typeof(RainWorldGame).GetField(nameof(RainWorldGame.rainWorld))),
                x => x.MatchLdfld(typeof(RainWorld).GetField(nameof(RainWorld.safariMode)))
                );

            // Modify echo spawning conditions based on settings
            c.Emit(OpCodes.Ldloc_0); // ghostID
            c.Emit(OpCodes.Ldloc_2); // flag for if echo should be spawned
            c.EmitDelegate<Func<World, GhostWorldPresence.GhostID, bool, bool>>(CustomEchoLogic);
            c.Emit(OpCodes.Stloc_2);

            c.Emit(OpCodes.Ldarg_0); // We interuppted after a ldarg_0, so put that back before we leave

            bool CustomEchoLogic(World self, GhostWorldPresence.GhostID ghostID, bool spawnEcho)
            {
                // Use default logic if karma cap >= 5 or we're not in a mode where this applies
                if (!Plugin.RandoManager.isRandomizerActive
                    || !(Plugin.RandoManager is ManagerArchipelago)
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
            ILCursor c = new ILCursor(il);
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
            ILCursor c = new ILCursor(il);
            // After myRobot load at 0018
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.myRobot)))
                );
            // Skip cutscene if we saw it instead of if robo is present
            c.Emit(OpCodes.Pop);
            c.EmitDelegate<Func<bool>>(() => hasSeenArtyStart);

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
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand))),
                x => x.MatchCallOrCallvirt(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))
                );
            c.MoveAfterLabels();
            c.EmitDelegate<Func<bool, bool>>(YesItIsMeGourmand);
        }

        /// <summary>
        /// Allows all non-Gourmands to progress the food quest and collect the relevant checks.
        /// </summary>
        private static void PlayerSessionRecord_AddEat(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            for (int i = 0; i < 2; i++)
            {
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Gourmand))),
                    x => x.MatchCallOrCallvirt(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))
                    );
                c.MoveAfterLabels();
                c.EmitDelegate<Func<bool, bool>>(YesItIsMeGourmand);
            }

            // Allow popcorn seeds to count as popcorn plants (argument interception at 0231).
            c.GotoNext(x => x.MatchCallOrCallvirt(typeof(WinState).GetMethod(nameof(WinState.GourmandPassageRequirementAtIndex))));  // 0221
            c.GotoNext(MoveType.After, x => x.MatchLdfld(typeof(AbstractPhysicalObject).GetField(nameof(AbstractPhysicalObject.type))));  // 022c

            AbstractPhysicalObject.AbstractObjectType TreatSeedsAsCobs(AbstractPhysicalObject.AbstractObjectType prev)
            {
                return (ModManager.MSC && prev == DLCSharedEnums.AbstractObjectType.Seed) ? AbstractPhysicalObject.AbstractObjectType.SeedCob : prev;
            }

            c.EmitDelegate<Func<AbstractPhysicalObject.AbstractObjectType, AbstractPhysicalObject.AbstractObjectType>>(TreatSeedsAsCobs);

        }

        [Obsolete("Not currently applied")]
        private static void WinStateCreateTrackerIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.EndgameID).GetField(nameof(MoreSlugcatsEnums.EndgameID.Gourmand))),
                x => x.MatchCallOrCallvirt(typeof(ExtEnum<WinState.EndgameID>).GetMethod("op_Equality"))
            );
            c.MoveAfterLabels();

            c.EmitDelegate<Func<bool, bool>>(YesItIsMeGourmand);
        }

        private static bool YesItIsMeGourmand(bool prev) => Options.UseFoodQuest || prev;

        /// <summary>
        /// Allow Spearmaster to eat mushrooms for the food quest, and detect spearing a neuron for Eat Neuron check
        /// </summary>
        private static void SpearmasterMushroomAddEat(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(x => x.MatchIsinst<Mushroom>());
            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(UpdatableAndDeletable).GetMethod(nameof(UpdatableAndDeletable.Destroy))));

            void Delegate(Spear self, PhysicalObject obj)
            {
                // Previous IL has already checked whether it's a live Spearmaster needle.
                if (self.room?.game.GetStorySession?.playerSessionRecords is PlayerSessionRecord[] records)
                {
                    records[((self.thrownBy as Player).abstractCreature.state as PlayerState).playerNumber].AddEat(obj);
                }
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<Spear, PhysicalObject>>(Delegate);

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
            ILCursor c = new ILCursor(il);

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
            WinState.GourmandTrackerData[] data = new WinState.GourmandTrackerData[]
            {
                new WinState.GourmandTrackerData(AbstractPhysicalObject.AbstractObjectType.SeedCob, null),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.Centipede, CreatureTemplate.Type.SmallCentipede }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.VultureGrub }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.SmallNeedleWorm, CreatureTemplate.Type.BigNeedleWorm }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.GreenLizard }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.BlueLizard }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.PinkLizard }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.WhiteLizard }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.RedLizard }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { DLCSharedEnums.CreatureTemplateType.SpitLizard }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { DLCSharedEnums.CreatureTemplateType.ZoopLizard }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { MoreSlugcatsEnums.CreatureTemplateType.TrainLizard }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.BigSpider }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.SpitterSpider }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { DLCSharedEnums.CreatureTemplateType.MotherSpider }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.Vulture }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.KingVulture }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { DLCSharedEnums.CreatureTemplateType.MirosVulture }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.LanternMouse }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.CicadaA, CreatureTemplate.Type.CicadaB }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { DLCSharedEnums.CreatureTemplateType.Yeek }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.DropBug }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.MirosBird }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.Scavenger, DLCSharedEnums.CreatureTemplateType.ScavengerElite, MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.DaddyLongLegs, CreatureTemplate.Type.BrotherLongLegs, DLCSharedEnums.CreatureTemplateType.TerrorLongLegs, MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.PoleMimic }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.TentaclePlant }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { CreatureTemplate.Type.BigEel }),
                new WinState.GourmandTrackerData(null, new CreatureTemplate.Type[] { DLCSharedEnums.CreatureTemplateType.Inspector }),
            };

            unexpanded = WinState.GourmandPassageTracker.ToArray();
            expanded = unexpanded.Concat(data).ToArray();
        }

        internal static WinState.GourmandTrackerData[] unexpanded;
        internal static WinState.GourmandTrackerData[] expanded;

        /// <summary>
        /// Add a sprite for SeedCobs to use in ItemSymbols.
        /// </summary>
        [Obsolete("Currently not needed since Watcher added their own version of sprites we had added")]
        private static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            //if (itemType == AbstractPhysicalObject.AbstractObjectType.SeedCob) return "Symbol_SeedCob";
            return orig(itemType, intData);
        }

        /// <summary>
        /// Add color definitions for symbols that need them
        /// </summary>
        private static Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intdata)
        {
            if (itemType == AbstractPhysicalObject.AbstractObjectType.SeedCob) return new Color(0.4117f, 0.1608f, 0.2275f);
            return orig(itemType, intdata);
        }
    }
}
