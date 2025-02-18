using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
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
            On.WinState.GetNextEndGame += NextPassageToken;
            On.WinState.ConsumeEndGame += ConsumePassageToken;
            On.PlayerProgression.ReloadLocksList += ReloadLocksList;
            On.Menu.EndgameTokens.ctor += PassageTokens;
            On.Menu.EndgameTokens.Passage += DoPassage;
            On.SaveState.setDenPosition += OnSetDenPosition;
            On.SaveState.GhostEncounter += EchoEncounter;

            try
            {
                IL.Menu.SlugcatSelectMenu.Update += SlugcatSelectMenuUpdateIL;
                IL.MoreSlugcats.CollectiblesTracker.ctor += CreateCollectiblesTrackerIL;
                IL.MoreSlugcats.CutsceneArtificerRobo.GetInput += ArtificerRoboIL;
                IL.PlayerSessionRecord.AddEat += PlayerSessionRecord_AddEat;
                IL.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
                //IL.WinState.CreateAndAddTracker += WinStateCreateTrackerIL;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.RegionGate.customKarmaGateRequirements -= GateRequirements;
            On.WinState.GetNextEndGame -= NextPassageToken;
            On.WinState.ConsumeEndGame -= ConsumePassageToken;
            On.PlayerProgression.ReloadLocksList -= ReloadLocksList;
            On.Menu.EndgameTokens.ctor -= PassageTokens;
            On.Menu.EndgameTokens.Passage -= DoPassage;
            On.SaveState.setDenPosition -= OnSetDenPosition;
            On.SaveState.GhostEncounter -= EchoEncounter;
            IL.Menu.SlugcatSelectMenu.Update -= SlugcatSelectMenuUpdateIL;
            IL.MoreSlugcats.CollectiblesTracker.ctor -= CreateCollectiblesTrackerIL;
            IL.MoreSlugcats.CutsceneArtificerRobo.GetInput -= ArtificerRoboIL;
            IL.PlayerSessionRecord.AddEat -= PlayerSessionRecord_AddEat;
            IL.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
            IL.WinState.CreateAndAddTracker -= WinStateCreateTrackerIL;
        }

        public static void OnSetDenPosition(On.SaveState.orig_setDenPosition orig, SaveState self)
        {
            orig(self);

            if (Plugin.RandoManager is ManagerArchipelago managerAP)
            {
                if (ArchipelagoConnection.useRandomStartRegion)
                {
                    self.denPosition = Plugin.RandoManager.customStartDen;
                }
            }
            else if (Plugin.randomizeSpawnLocation.Value)
            {
                self.denPosition = Plugin.RandoManager.customStartDen;
            }
        }

        // TODO: Need explanation text for when start game button is greyed out
        public static void SlugcatSelectMenuUpdateIL(ILContext il)
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
                    return !Plugin.archipelago.Value
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

        public static void CreateCollectiblesTrackerIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                // Modify all the collectible sprites to read from our data
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker>(nameof(MoreSlugcats.CollectiblesTracker.collectionData)),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker.SaveGameData>(nameof(MoreSlugcats.CollectiblesTracker.SaveGameData.unlockedGolds))
                    );

                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<List<MultiplayerUnlocks.LevelUnlockID>>>(() =>
                {
                    List<MultiplayerUnlocks.LevelUnlockID> output = new List<MultiplayerUnlocks.LevelUnlockID>();
                    foreach (string loc in Plugin.RandoManager.GetLocations())
                    {
                        if (loc.StartsWith("Token-L-"))
                        {
                            // Trim region suffix if present
                            //string[] split = Regex.Split(loc, "-");
                            //string trimmedLoc = split.Length > 2 ? $"{split[0]}-{split[1]}" : loc;

                            if (ExtEnumBase.TryParse(typeof(MultiplayerUnlocks.LevelUnlockID), loc.Substring(8), false, out ExtEnumBase value)
                                && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
                            {
                                output.Add((MultiplayerUnlocks.LevelUnlockID)value);
                            }
                        }
                    }
                    return output;
                });

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker>(nameof(MoreSlugcats.CollectiblesTracker.collectionData)),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker.SaveGameData>(nameof(MoreSlugcats.CollectiblesTracker.SaveGameData.unlockedBlues))
                    );

                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<List<MultiplayerUnlocks.SandboxUnlockID>>>(() =>
                {
                    List<MultiplayerUnlocks.SandboxUnlockID> output = new List<MultiplayerUnlocks.SandboxUnlockID>();
                    foreach (string loc in Plugin.RandoManager.GetLocations())
                    {
                        if (loc.StartsWith("Token-"))
                        {
                            // Trim region suffix if present
                            string[] split = Regex.Split(loc, "-");
                            string trimmedLoc = split.Length > 2 ? $"{split[0]}-{split[1]}" : loc;

                            if (ExtEnumBase.TryParse(typeof(MultiplayerUnlocks.SandboxUnlockID), trimmedLoc.Substring(6), false, out ExtEnumBase value)
                                && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
                            {
                                output.Add((MultiplayerUnlocks.SandboxUnlockID)value);
                            }
                        }
                    }
                    return output;
                });

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker>(nameof(MoreSlugcats.CollectiblesTracker.collectionData)),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker.SaveGameData>(nameof(MoreSlugcats.CollectiblesTracker.SaveGameData.unlockedGreens))
                    );

                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<List<MultiplayerUnlocks.SlugcatUnlockID>>>(() =>
                {
                    List<MultiplayerUnlocks.SlugcatUnlockID> output = new List<MultiplayerUnlocks.SlugcatUnlockID>();
                    foreach (string loc in Plugin.RandoManager.GetLocations())
                    {
                        if (loc.StartsWith("Token-"))
                        {
                            // Trim region suffix if present
                            string[] split = Regex.Split(loc, "-");
                            string trimmedLoc = split.Length > 2 ? $"{split[0]}-{split[1]}" : loc;

                            if (ExtEnumBase.TryParse(typeof(MultiplayerUnlocks.SlugcatUnlockID), trimmedLoc.Substring(6), false, out ExtEnumBase value)
                                && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
                            {
                                output.Add((MultiplayerUnlocks.SlugcatUnlockID)value);
                            }
                        }
                    }
                    return output;
                });

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker>(nameof(MoreSlugcats.CollectiblesTracker.collectionData)),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker.SaveGameData>(nameof(MoreSlugcats.CollectiblesTracker.SaveGameData.unlockedGreys))
                    );

                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<List<MoreSlugcats.ChatlogData.ChatlogID>>>(() =>
                {
                    List<MoreSlugcats.ChatlogData.ChatlogID> output = new List<MoreSlugcats.ChatlogData.ChatlogID>();
                    foreach (string loc in Plugin.RandoManager.GetLocations())
                    {
                        if (loc.StartsWith("Broadcast-")
                            && ExtEnumBase.TryParse(typeof(MoreSlugcats.ChatlogData.ChatlogID), loc.Substring(12), false, out ExtEnumBase value)
                            && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
                        {
                            output.Add((MoreSlugcats.ChatlogData.ChatlogID)value);
                        }
                    }
                    return output;
                });

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker>(nameof(MoreSlugcats.CollectiblesTracker.collectionData)),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker.SaveGameData>(nameof(MoreSlugcats.CollectiblesTracker.SaveGameData.unlockedReds))
                    );

                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<List<MultiplayerUnlocks.SafariUnlockID>>>(() =>
                {
                    List<MultiplayerUnlocks.SafariUnlockID> output = new List<MultiplayerUnlocks.SafariUnlockID>();
                    foreach (string loc in Plugin.RandoManager.GetLocations())
                    {
                        if (loc.StartsWith("Token-S-"))
                        {
                            // Trim region suffix if present
                            //string[] split = Regex.Split(loc, "-");
                            //string trimmedLoc = split.Length > 2 ? $"{split[0]}-{split[1]}" : loc;

                            if (ExtEnumBase.TryParse(typeof(MultiplayerUnlocks.SafariUnlockID), loc.Substring(8), false, out ExtEnumBase value)
                                && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
                            {
                                output.Add((MultiplayerUnlocks.SafariUnlockID)value);
                            }
                        }
                    }
                    return output;
                });

                c.GotoNext(MoveType.After,
                    x => x.MatchLdfld<RainWorld>(nameof(RainWorld.regionRedTokens)),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<MoreSlugcats.CollectiblesTracker>(nameof(MoreSlugcats.CollectiblesTracker.displayRegions)),
                    x => x.MatchLdloc(out _),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchBlt(out _)
                    );

                // Pearl tracker
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldloc, 5);
                c.Emit(OpCodes.Ldarg, 5);
                c.EmitDelegate<Action<MoreSlugcats.CollectiblesTracker, RainWorld, int, SlugcatStats.Name>>((self, rainWorld, i, saveSlot) =>
                {
                    // Pearls
                    List<DataPearl.AbstractDataPearl.DataPearlType> foundPearls = new List<DataPearl.AbstractDataPearl.DataPearlType>();
                    List<GhostWorldPresence.GhostID> foundEchoes = new List<GhostWorldPresence.GhostID>();
                    foreach (string loc in Plugin.RandoManager.GetLocations())
                    {
                        if (loc.StartsWith("Pearl-"))
                        {
                            // Trim region suffix if present
                            string[] split = Regex.Split(loc, "-");
                            string trimmedLoc = split.Length > 2 ? $"{split[0]}-{split[1]}" : loc;

                            if (ExtEnumBase.TryParse(typeof(DataPearl.AbstractDataPearl.DataPearlType), trimmedLoc.Substring(6), false, out ExtEnumBase value)
                                && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
                            {
                                foundPearls.Add((DataPearl.AbstractDataPearl.DataPearlType)value);
                            }
                        }

                        if (loc.StartsWith("Echo-")
                            && ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), loc.Substring(5), false, out ExtEnumBase value1)
                            && (Plugin.RandoManager.IsLocationGiven(loc) ?? false))
                        {
                            foundEchoes.Add((GhostWorldPresence.GhostID)value1);
                        }
                    }

                    if (!ExtCollectibleTrackerComptability.Enabled)
                    {
                        for (int j = 0; j < rainWorld.regionDataPearls[self.displayRegions[i]].Count; j++)
                        {
                            if (rainWorld.regionDataPearlsAccessibility[self.displayRegions[i]][j].Contains(saveSlot))
                            {
                                self.spriteColors[self.displayRegions[i]].Add(Color.white);
                                if (foundPearls.Contains(rainWorld.regionDataPearls[self.displayRegions[i]][j]))
                                {
                                    self.sprites[self.displayRegions[i]].Add(new FSprite("ctOn", true));
                                }
                                else
                                {
                                    self.sprites[self.displayRegions[i]].Add(new FSprite("ctOff", true));
                                }
                            }
                        }
                    }

                    if (GhostWorldPresence.GetGhostID(self.displayRegions[i].ToUpper()) != GhostWorldPresence.GhostID.NoGhost
                        && World.CheckForRegionGhost(saveSlot, self.displayRegions[i].ToUpper()))
                    {
                        self.spriteColors[self.displayRegions[i]].Add(RainWorld.SaturatedGold);
                        if (foundEchoes.Contains(GhostWorldPresence.GetGhostID(self.displayRegions[i].ToUpper())))
                        {
                            self.sprites[self.displayRegions[i]].Add(new FSprite("ctOn", true));
                        }
                        else
                        {
                            self.sprites[self.displayRegions[i]].Add(new FSprite("ctOff", true));
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for CollectiblesTracker");
                Plugin.Log.LogError(e);
            }
        }

        public static void GateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            if (!Plugin.RandoManager.isRandomizerActive)
            {
                orig(self);
                return;
            }

            // All gates can be unlocked by simply updating their entry in gateStatus Dict
            if (!(Plugin.RandoManager.IsGateOpen(self.room.abstractRoom.name) ?? false)
                && self.room.abstractRoom.name != "GATE_OE_SU") // OE_SU needs to stay open to avoid softlocks
            {
                self.karmaRequirements[0] = RegionGate.GateRequirement.DemoLock;
                self.karmaRequirements[1] = RegionGate.GateRequirement.DemoLock;
            }
            else if (Plugin.startMinKarma.Value) // Reduce requirement if the karma cap was forced lower
            {
                int karmaCap = Plugin.Singleton.rainWorld.progression.currentSaveState.deathPersistentSaveData.karmaCap;
                if (int.TryParse(self.karmaRequirements[0].value, out int oldReq1)
                    && oldReq1 > karmaCap)
                {
                    self.karmaRequirements[0] = new RegionGate.GateRequirement($"{karmaCap + 1}");
                }

                if (int.TryParse(self.karmaRequirements[1].value, out int oldReq2)
                    && oldReq2 > karmaCap)
                {
                    self.karmaRequirements[1] = new RegionGate.GateRequirement($"{karmaCap + 1}");
                }
            }
            else if (self.room.abstractRoom.name == "GATE_UW_LC"
                //&& (currentSlugcat == SlugcatStats.Name.White || currentSlugcat == SlugcatStats.Name.Yellow)
                && Plugin.allowMetroForOthers.Value)
            {
                // Force open Metropolis Gate
                self.karmaRequirements[0] = RegionGate.GateRequirement.FiveKarma;
                self.karmaRequirements[1] = RegionGate.GateRequirement.FiveKarma;
            }

            orig(self);
        }

        // Overwrites the default logic
        // Unless gateUnlocks hasn't been populated yet
        public static void ReloadLocksList(On.PlayerProgression.orig_ReloadLocksList orig, PlayerProgression self)
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

        #region Karma ladder hacking
        public static void PassageTokens(On.Menu.EndgameTokens.orig_ctor orig, Menu.EndgameTokens self, Menu.Menu menu, Menu.MenuObject owner, Vector2 pos, FContainer container, Menu.KarmaLadder ladder)
        {
            orig(self, menu, owner, pos, container, ladder);
            if (!Plugin.givePassageUnlocks.Value) return;

            // We won't be needing these
            foreach (Menu.EndgameTokens.Token token in self.tokens)
            {
                token.symbolSprite.RemoveFromContainer();
                token.circleSprite.RemoveFromContainer();
                token.glowSprite.RemoveFromContainer();
            }

            Plugin.Singleton.passageTokensUI = new List<FakeEndgameToken>();
            int index = 0;
            foreach (WinState.EndgameID id in Plugin.RandoManager.GetPassageTokensStatus().Keys)
            {
                if (id == MoreSlugcats.MoreSlugcatsEnums.EndgameID.Gourmand) continue;

                if ((Plugin.RandoManager.HasPassageToken(id) ?? false)
                    && (menu as KarmaLadderScreen).winState.GetTracker(id, true).consumed == false)
                {
                    Plugin.Singleton.passageTokensUI.Add(new FakeEndgameToken(menu, self, Vector2.zero, id, container, index));
                    self.subObjects.Add(Plugin.Singleton.passageTokensUI.Last());
                    index++;
                }
            }
        }

        public static void DoPassage(On.Menu.EndgameTokens.orig_Passage orig, Menu.EndgameTokens self, WinState.EndgameID ID)
        {
            orig(self, ID);
            if (!Plugin.givePassageUnlocks.Value) return;

            // I said NO!
            foreach (Menu.EndgameTokens.Token token in self.tokens)
            {
                token.symbolSprite.RemoveFromContainer();
                token.circleSprite.RemoveFromContainer();
                token.glowSprite.RemoveFromContainer();
            }

            // I'm doing MY tokens instead!
            for (int i = 0; i < Plugin.Singleton.passageTokensUI.Count; i++)
            {
                if (Plugin.Singleton.passageTokensUI[i].id == ID)
                {
                    Plugin.Singleton.passageTokensUI[i].Activate();
                    return;
                }
            }
        }
        #endregion

        // Overwrites the default logic
        public static WinState.EndgameID NextPassageToken(On.WinState.orig_GetNextEndGame orig, WinState self)
        {
            if (!Plugin.RandoManager.isRandomizerActive || !Plugin.givePassageUnlocks.Value) return orig(self);

            foreach (var passage in Plugin.RandoManager.GetPassageTokensStatus())
            {
                if (passage.Value
                    && !self.GetTracker(passage.Key, true).consumed)
                {
                    return passage.Key;
                }
            }
            return null;
        }

        // Overwrites the default logic
        public static void ConsumePassageToken(On.WinState.orig_ConsumeEndGame orig, WinState self)
        {
            if (!Plugin.RandoManager.isRandomizerActive || !Plugin.givePassageUnlocks.Value)
            {
                orig(self);
                return;
            }

            foreach (var passage in Plugin.RandoManager.GetPassageTokensStatus())
            {
                if (passage.Value
                    && !self.GetTracker(passage.Key, true).consumed)
                {
                    self.GetTracker(passage.Key, true).consumed = true;
                    return;
                }
            }
        }

        public static void EchoEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
        {
            orig(self, ghost, rainWorld);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            self.deathPersistentSaveData.karmaCap = Plugin.RandoManager.CurrentMaxKarma;
            self.deathPersistentSaveData.karma = self.deathPersistentSaveData.karmaCap;

            Plugin.RandoManager.GiveLocation("Echo-" + ghost.value);

            Plugin.Singleton.game.rainWorld.progression.SaveProgressionAndDeathPersistentDataOfCurrentState(false, false);
        }

        public static void ArtificerRoboIL(ILContext il)
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
        public static void HUD_InitSinglePlayerHud(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality")));
            c.MoveAfterLabels();
            c.EmitDelegate<Func<bool, bool>>(YesItIsMeGourmand);
        }

        /// <summary>
        /// All non-Gourmands to progress the food quest and collect the relevant checks.
        /// </summary>
        public static void PlayerSessionRecord_AddEat(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            for (int i = 0; i < 2; i++)
            {
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality")));
                c.MoveAfterLabels();
                c.EmitDelegate<Func<bool, bool>>(YesItIsMeGourmand);
            }
        }

        public static void WinStateCreateTrackerIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.EndgameID).GetField(nameof(MoreSlugcatsEnums.EndgameID.Gourmand))),
                x => x.MatchCallOrCallvirt(typeof(ExtEnum<WinState.EndgameID>).GetMethod("op_Equality"))
            );
            c.MoveAfterLabels();

            c.EmitDelegate<Func<bool, bool>>(YesItIsMeGourmand);

            c.EmitDelegate<Action>(() =>
            {
                Plugin.Log.LogDebug("Adding Gourmand Tracker");
            });
        }

        public static bool YesItIsMeGourmand(bool prev) => (Plugin.RandoManager is ManagerArchipelago && ArchipelagoConnection.foodQuestForAll) || prev;
    }
}
