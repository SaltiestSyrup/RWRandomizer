using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class MiscHooks
    {
        public static void ApplyHooks()
        {
            On.RegionGate.customKarmaGateRequirements += GateRequirements;
            On.PlayerProgression.ReloadLocksList += ReloadLocksList;
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
                IL.MoreSlugcats.CutsceneArtificerRobo.GetInput += ArtificerRoboIL;
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
            On.RegionGate.customKarmaGateRequirements -= GateRequirements;
            On.PlayerProgression.ReloadLocksList -= ReloadLocksList;
            On.SaveState.setDenPosition -= OnSetDenPosition;
            On.SaveState.GhostEncounter -= EchoEncounter;
            On.MoreSlugcats.MoreSlugcats.OnInit -= MoreSlugcats_OnInit;
            //On.ItemSymbol.SpriteNameForItem -= ItemSymbol_SpriteNameForItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;

            IL.Menu.MainMenu.ctor -= MainMenuCtorIL;
            IL.Menu.SlugcatSelectMenu.Update -= SlugcatSelectMenuUpdateIL;
            IL.MoreSlugcats.CutsceneArtificerRobo.GetInput -= ArtificerRoboIL;
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

        // TODO: Need explanation text for when start game button is greyed out
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

        private static void GateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            if (!Plugin.RandoManager.isRandomizerActive)
            {
                orig(self);
                return;
            }

            string gateName = self.room.abstractRoom.name;
            bool hasKeyForGate = Plugin.RandoManager.IsGateOpen(gateName) ?? false;

            // Consider these gates as always unlocked
            if (gateName == "GATE_OE_SU"
                || (gateName == "GATE_SL_MS"
                    && ModManager.MSC
                    && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Rivulet))
            {
                hasKeyForGate = true;
            }

            // Change default Metropolis gate karma
            if (gateName == "GATE_UW_LC" && Options.ForceOpenMetropolis)
            {
                self.karmaRequirements[0] = RegionGate.GateRequirement.FiveKarma;
                self.karmaRequirements[1] = RegionGate.GateRequirement.FiveKarma;
            }

            // Decide gate behavior
            Plugin.GateBehavior gateBehavior;
            if (Plugin.RandoManager is ManagerArchipelago)
            {
                gateBehavior = ArchipelagoConnection.gateBehavior;
            }
            else if (Options.StartMinimumKarma)
            {
                gateBehavior = Plugin.GateBehavior.OnlyKey;
            }
            else
            {
                gateBehavior = Plugin.GateBehavior.KeyAndKarma;
            }

            // Apply behavior
            switch (gateBehavior)
            {
                case Plugin.GateBehavior.OnlyKey:
                    if (hasKeyForGate)
                    {
                        self.karmaRequirements[0] = RegionGate.GateRequirement.OneKarma;
                        self.karmaRequirements[1] = RegionGate.GateRequirement.OneKarma;
                    }
                    else
                    {
                        self.karmaRequirements[0] = RegionGate.GateRequirement.DemoLock;
                        self.karmaRequirements[1] = RegionGate.GateRequirement.DemoLock;
                    }
                    break;
                case Plugin.GateBehavior.KeyAndKarma:
                    if (!hasKeyForGate)
                    {
                        self.karmaRequirements[0] = RegionGate.GateRequirement.DemoLock;
                        self.karmaRequirements[1] = RegionGate.GateRequirement.DemoLock;
                    }
                    break;
                case Plugin.GateBehavior.KeyOrKarma:
                    if (hasKeyForGate)
                    {
                        self.karmaRequirements[0] = RegionGate.GateRequirement.OneKarma;
                        self.karmaRequirements[1] = RegionGate.GateRequirement.OneKarma;
                    }
                    break;
                case Plugin.GateBehavior.OnlyKarma:
                    // Nothing to be done here, use vanilla mechanics
                    break;
            }

            orig(self);
        }

        private static void CanUseUnlockedGatesIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Act as if Monk style karma gates is set if player YAML needs it
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

        // Overwrites the default logic
        // Unless gateUnlocks hasn't been populated yet
        private static void ReloadLocksList(On.PlayerProgression.orig_ReloadLocksList orig, PlayerProgression self)
        {
            if (!Plugin.RandoManager.isRandomizerActive || Plugin.RandoManager.GetGatesStatus().Count == 0)
            {
                orig(self);
                return;
            }

            string[] vanillaLocks;

            string path = AssetManager.ResolveFilePath(string.Concat(new string[]
            {
                "World",
                Path.DirectorySeparatorChar.ToString(),
                "Gates",
                Path.DirectorySeparatorChar.ToString(),
                "locks.txt"
            }));

            if (File.Exists(path))
            {
                vanillaLocks = File.ReadAllLines(path);

                for (int i = 0; i < vanillaLocks.Length; i++)
                {
                    // If we don't have the gate stored, or it should be locked, lock it
                    if (!(Plugin.RandoManager.IsGateOpen(Regex.Split(vanillaLocks[i], " : ")[0]) ?? false)
                            && Regex.Split(vanillaLocks[i], " : ")[0] != "GATE_OE_SU") // OE_SU needs to stay open to avoid softlocks
                    {
                        // Split the gate apart and set the values to locked
                        string[] split = Regex.Split(vanillaLocks[i], " : ");

                        split[1] = RegionGate.GateRequirement.DemoLock.value;
                        split[2] = RegionGate.GateRequirement.DemoLock.value;

                        // Rejoin the string and assign it
                        vanillaLocks[i] = "";
                        foreach (string s in split)
                        {
                            vanillaLocks[i] += s + " : ";
                        }
                        vanillaLocks[i] = vanillaLocks[i].Substring(0, vanillaLocks[i].Length - 3);
                    }
                }

                self.karmaLocks = vanillaLocks;
                return;
            }

            self.karmaLocks = new string[0];
        }

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

            c.Emit(OpCodes.Ldarg_0); // We interuppted after a ldarg_0, so put that back before we leave
        }

        private static void EchoEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
        {
            orig(self, ghost, rainWorld);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            self.deathPersistentSaveData.karmaCap = Plugin.RandoManager.CurrentMaxKarma;
            self.deathPersistentSaveData.karma = self.deathPersistentSaveData.karmaCap;

            Plugin.RandoManager.GiveLocation("Echo-" + ghost.value);

            Plugin.Singleton.game.rainWorld.progression.SaveProgressionAndDeathPersistentDataOfCurrentState(false, false);
        }

        private static void ArtificerRoboIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdsfld(typeof(MoreSlugcats.CutsceneArtificerRobo.Phase).GetField(nameof(MoreSlugcats.CutsceneArtificerRobo.Phase.ActivateRobo))),
                    x => x.MatchStfld(typeof(MoreSlugcats.CutsceneArtificerRobo).GetField(nameof(MoreSlugcats.CutsceneArtificerRobo.phase)))
                    );

                c.Index--;
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldsfld, typeof(MoreSlugcats.CutsceneArtificerRobo.Phase).GetField(nameof(MoreSlugcats.CutsceneArtificerRobo.Phase.End)));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for ArtificerRoboIL");
                Plugin.Log.LogError(e);
            }
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
        /// All non-Gourmands to progress the food quest and collect the relevant checks.
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
        /// Allow Spearmaster to eat mushrooms for the food quest.
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
        }

        // Filter the next predicted food item to be accessible to the current slugcat
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

        private static Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intdata)
        {
            if (itemType == AbstractPhysicalObject.AbstractObjectType.SeedCob) return new Color(0.4117f, 0.1608f, 0.2275f);
            return orig(itemType, intdata);
        }
    }
}
