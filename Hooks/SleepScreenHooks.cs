using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class SleepScreenHooks
    {
        public static void ApplyHooks()
        {
            On.WinState.GetNextEndGame += NextPassageToken;
            On.WinState.ConsumeEndGame += ConsumePassageToken;
            On.Menu.EndgameTokens.ctor += OnEndgameTokensCtor;
            On.Menu.EndgameTokens.Passage += DoPassage;
            On.Menu.KarmaLadder.ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool += OnKarmaLadderCtor;

            try
            {
                IL.MoreSlugcats.CollectiblesTracker.ctor += CreateCollectiblesTrackerIL;
                IL.Menu.EndgameTokens.ctor += EngameTokensCtorIL;
                IL.Menu.SleepAndDeathScreen.AddPassageButton += AddPassageButtonIL;
                IL.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreenGetDataFromGameIL;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.WinState.GetNextEndGame -= NextPassageToken;
            On.WinState.ConsumeEndGame -= ConsumePassageToken;
            On.Menu.EndgameTokens.ctor -= OnEndgameTokensCtor;
            On.Menu.EndgameTokens.Passage += DoPassage;
            On.Menu.KarmaLadder.ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool -= OnKarmaLadderCtor;

            IL.MoreSlugcats.CollectiblesTracker.ctor -= CreateCollectiblesTrackerIL;
            IL.Menu.EndgameTokens.ctor -= EngameTokensCtorIL;
            IL.Menu.SleepAndDeathScreen.AddPassageButton -= AddPassageButtonIL;
            IL.Menu.SleepAndDeathScreen.GetDataFromGame -= SleepAndDeathScreenGetDataFromGameIL;
        }

        // ----- COLLECTIBLES -----

        /// <summary>
        /// Hacks the collectible tracker to display check completion status for tokens, as well as adding pearls and echoes to it
        /// </summary>
        private static void CreateCollectiblesTrackerIL(ILContext il)
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
                // TODO: Figure out why there's an extra pearl displayed in future GW
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

        // ----- PASSAGE TOKENS -----
        
        /// <summary>
        /// Stores fake passage token UI elements
        /// </summary>
        private static ConditionalWeakTable<EndgameTokens, List<FakeEndgameToken>> passageTokensUI = new ConditionalWeakTable<EndgameTokens, List<FakeEndgameToken>>();

        /// <summary>
        /// Replace normal passage token list with custom one that contains tokens collected from items rather than tokens from completed passages
        /// </summary>
        private static void OnEndgameTokensCtor(On.Menu.EndgameTokens.orig_ctor orig, EndgameTokens self, Menu.Menu menu, MenuObject owner, Vector2 pos, FContainer container, KarmaLadder ladder)
        {
            orig(self, menu, owner, pos, container, ladder);
            if (!Options.GivePassageItems) return;

            // We won't be needing these
            foreach (EndgameTokens.Token token in self.tokens)
            {
                token.symbolSprite.RemoveFromContainer();
                token.circleSprite.RemoveFromContainer();
                token.glowSprite.RemoveFromContainer();
            }

            List<FakeEndgameToken> fakePassageTokens = new List<FakeEndgameToken>();
            int index = 0;
            foreach (WinState.EndgameID id in Plugin.RandoManager.GetPassageTokensStatus().Keys)
            {
                if (id == MoreSlugcats.MoreSlugcatsEnums.EndgameID.Gourmand) continue;

                if ((Plugin.RandoManager.HasPassageToken(id) ?? false)
                    && (menu as KarmaLadderScreen).winState.GetTracker(id, true).consumed == false)
                {
                    fakePassageTokens.Add(new FakeEndgameToken(menu, self, Vector2.zero, id, container, index));
                    self.subObjects.Add(fakePassageTokens.Last());
                    index++;
                }
            }
            passageTokensUI.Add(self, fakePassageTokens);

            
        }

        /// <summary>
        /// Make passage button appear if any token items have been found
        /// </summary>
        private static void EngameTokensCtorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(0),
                x => x.MatchBrfalse(out _)
                );

            c.Index--;

            c.EmitDelegate<Func<bool, bool>>((flag) =>
            {
                return flag || Plugin.RandoManager.GetPassageTokensStatus().Values.Any(v => v);
            });
        }

        /// <summary>
        /// Force passage button to display for Hunter and Saint
        /// </summary>
        private static void AddPassageButtonIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Remove check for Hunter / Saint to let them use passages
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(KarmaLadderScreen).GetField(nameof(KarmaLadderScreen.saveState))),
                x => x.MatchBrfalse(out _)
                );

            c.Index--;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }

        /// <summary>
        /// Extra override to allow Hunter to use passages
        /// </summary>
        private static void SleepAndDeathScreenGetDataFromGameIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(SlugcatStats.Name).GetField(nameof(SlugcatStats.Name.Red))),
                x => x.MatchCallOrCallvirt(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Inequality"))
                );

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 1);
        }

        /// <summary>
        /// Override normal UI update when passage triggered in favor of custom one
        /// </summary>
        private static void DoPassage(On.Menu.EndgameTokens.orig_Passage orig, EndgameTokens self, WinState.EndgameID ID)
        {
            orig(self, ID);
            if (!Options.GivePassageItems) return;

            foreach (EndgameTokens.Token token in self.tokens)
            {
                token.symbolSprite.RemoveFromContainer();
                token.circleSprite.RemoveFromContainer();
                token.glowSprite.RemoveFromContainer();
            }

            List<FakeEndgameToken> fakePassageTokens = passageTokensUI.GetOrCreateValue(self);
            for (int i = 0; i < fakePassageTokens.Count; i++)
            {
                if (fakePassageTokens[i].id == ID)
                {
                    fakePassageTokens[i].Activate();
                    return;
                }
            }
        }

        /// <summary>
        /// Tell the game to consider the passage token we choose instead of the normal logic
        /// </summary>
        private static WinState.EndgameID NextPassageToken(On.WinState.orig_GetNextEndGame orig, WinState self)
        {
            if (!Plugin.RandoManager.isRandomizerActive || !Options.GivePassageItems) return orig(self);

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

        /// <summary>
        /// Tell the game to consume the passage token we choose instead of the normal logic
        /// </summary>
        private static void ConsumePassageToken(On.WinState.orig_ConsumeEndGame orig, WinState self)
        {
            if (!Plugin.RandoManager.isRandomizerActive || !Options.GivePassageItems)
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

        // ----- KARMA LADDER -----

        /// <summary>
        /// Tell karma ladder not to showcase a karma increase when meeting an echo
        /// </summary>
        private static void OnKarmaLadderCtor(On.Menu.KarmaLadder.orig_ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool orig,
            KarmaLadder self, Menu.Menu menu, MenuObject owner, Vector2 pos, HUD.HUD hud, IntVector2 displayKarma, bool reinforced)
        {
            (menu as KarmaLadderScreen).preGhostEncounterKarmaCap = Plugin.RandoManager.CurrentMaxKarma;
            orig(self, menu, owner, pos, hud, displayKarma, reinforced);
        }
    }
}
