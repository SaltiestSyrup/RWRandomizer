using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
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
            On.Menu.SleepAndDeathScreen.Update += OnSleepAndDeathScreenUpdate;
            On.Menu.SleepAndDeathScreen.UpdateInfoText += OnSleepAndDeathScreenUpdateInfoText;
            On.Menu.SleepAndDeathScreen.Singal += OnSleepAndDeathScreenSingal;
            On.Menu.EndgameTokens.Passage += DoPassage;
            On.Menu.KarmaLadder.ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool += OnKarmaLadderCtor;

            try
            {
                IL.MoreSlugcats.CollectiblesTracker.ctor += CreateCollectiblesTrackerIL;
                IL.Menu.EndgameTokens.ctor += EndgameTokensCtorIL;
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
            On.Menu.SleepAndDeathScreen.Update -= OnSleepAndDeathScreenUpdate;
            On.Menu.SleepAndDeathScreen.UpdateInfoText -= OnSleepAndDeathScreenUpdateInfoText;
            On.Menu.SleepAndDeathScreen.Singal -= OnSleepAndDeathScreenSingal;
            On.Menu.EndgameTokens.Passage -= DoPassage;
            On.Menu.KarmaLadder.ctor_Menu_MenuObject_Vector2_HUD_IntVector2_bool -= OnKarmaLadderCtor;

            IL.MoreSlugcats.CollectiblesTracker.ctor -= CreateCollectiblesTrackerIL;
            IL.Menu.EndgameTokens.ctor -= EndgameTokensCtorIL;
            IL.Menu.SleepAndDeathScreen.AddPassageButton -= AddPassageButtonIL;
            IL.Menu.SleepAndDeathScreen.GetDataFromGame -= SleepAndDeathScreenGetDataFromGameIL;
        }

        // ----- COLLECTIBLES -----

        /// <summary>
        /// Hacks the collectible tracker to display check completion status for tokens, as well as adding pearls and echoes to it
        /// </summary>
        private static void CreateCollectiblesTrackerIL(ILContext il)
        {
            ILCursor c = new(il);

            // After label at 0071
            c.GotoNext(MoveType.After,
                x => x.MatchNewobj(typeof(List<string>)),
                x => x.MatchStfld(typeof(CollectiblesTracker.SaveGameData).GetField(nameof(CollectiblesTracker.SaveGameData.regionsVisited)))
                );
            c.MoveAfterLabels();

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(CollectiblesTracker).GetField(nameof(CollectiblesTracker.collectionData)));
            // Overwrite mined save data info with our own
            c.EmitDelegate<Action<CollectiblesTracker.SaveGameData>>((collectionData) =>
            {
                collectionData.unlockedGolds = [.. FoundTokensOfType<MultiplayerUnlocks.LevelUnlockID>().Cast<MultiplayerUnlocks.LevelUnlockID>()];
                collectionData.unlockedBlues = [.. FoundTokensOfType<MultiplayerUnlocks.SandboxUnlockID>().Cast<MultiplayerUnlocks.SandboxUnlockID>()];
                if (ModManager.MSC)
                {
                    collectionData.unlockedGreens = [.. FoundTokensOfType<MultiplayerUnlocks.SlugcatUnlockID>().Cast<MultiplayerUnlocks.SlugcatUnlockID>()];
                    collectionData.unlockedGreys = [.. FoundTokensOfType<ChatlogData.ChatlogID>().Cast<ChatlogData.ChatlogID>()];
                    collectionData.unlockedReds = [.. FoundTokensOfType<MultiplayerUnlocks.SafariUnlockID>().Cast<MultiplayerUnlocks.SafariUnlockID>()];
                }
            });

            // After label at 07FA
            //  The entry into the for loop here is pointed to multiple times,
            //  all the way up after the "has visited region?" check.
            //  But we need to move after the label to avoid getting caught in the MSC requirement,
            //  so the EmitDelegate re-does some checks to compensate.
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<RainWorld>(nameof(RainWorld.regionRedTokens)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CollectiblesTracker>(nameof(CollectiblesTracker.displayRegions)),
                x => x.MatchLdloc(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchBlt(out _)
                );
            c.MoveAfterLabels();

            // Pearl and Echo tracker
            // TODO: Figure out why there's an extra pearl displayed in future GW
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.Emit(OpCodes.Ldloc, 5);
            c.Emit(OpCodes.Ldarg, 5);
            c.EmitDelegate(AddPearlsAndEchoesToTracker);
        }

        /// <summary>
        /// Return list of all token locations of the given type that have been checked
        /// </summary>
        private static List<ExtEnumBase> FoundTokensOfType<T>() where T : ExtEnumBase
        {
            List<ExtEnumBase> output = [];
            string startPattern = typeof(T).Name switch
            {
                "LevelUnlockID" => "Token-L-",
                "SafariUnlockID" => "Token-S-",
                "ChatlogID" => "Broadcast-",
                _ => "Token-",
            };
            foreach (string loc in Plugin.RandoManager.GetLocationNames())
            {
                if (loc.StartsWith(startPattern))
                {
                    string trimmedLoc = loc;
                    // Trim region suffix if present
                    if (startPattern.Equals("Token-"))
                    {
                        string[] split = Regex.Split(loc, "-");
                        trimmedLoc = split.Length > 2 ? $"{split[0]}-{split[1]}" : loc;
                    }

                    if (ExtEnumBase.TryParse(typeof(T), trimmedLoc.Substring(startPattern.Length), false, out ExtEnumBase value)
                        && Plugin.RandoManager.IsLocationGiven(loc) is true)
                    {
                        output.Add(value);
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Adds Pearls and Echoes to the Collectible Tracker. Inserted into IL by <see cref="CreateCollectiblesTrackerIL"/>
        /// </summary>
        private static void AddPearlsAndEchoesToTracker(CollectiblesTracker self, RainWorld rainWorld, int i, SlugcatStats.Name saveSlot)
        {
            // The position in IL this is placed necessitates a duplicate check for if the region has been visited
            if (self.collectionData is null || !self.collectionData.regionsVisited.Contains(self.displayRegions[i]))
            {
                return;
            }

            // Find pearls and Echoes to place on tracker
            List<DataPearl.AbstractDataPearl.DataPearlType> foundPearls = [];
            List<GhostWorldPresence.GhostID> foundEchoes = [];
            foreach (string loc in Plugin.RandoManager.GetLocationNames())
            {
                if (loc.StartsWith("Pearl-"))
                {
                    // Trim region suffix if present
                    string[] split = Regex.Split(loc, "-");
                    string trimmedLoc = split.Length > 2 ? $"{split[0]}-{split[1]}" : loc;

                    if (ExtEnumBase.TryParse(typeof(DataPearl.AbstractDataPearl.DataPearlType), trimmedLoc.Substring(6), false, out ExtEnumBase value)
                        && Plugin.RandoManager.IsLocationGiven(loc) is true)
                    {
                        foundPearls.Add((DataPearl.AbstractDataPearl.DataPearlType)value);
                    }
                }

                if (loc.StartsWith("Echo-")
                    && ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), loc.Substring(5), false, out ExtEnumBase value1)
                    && Plugin.RandoManager.IsLocationGiven(loc) is true)
                {
                    foundEchoes.Add((GhostWorldPresence.GhostID)value1);
                }
            }

            // Add Pearls
            // Extended Collectibles Tracker has its own pearl implementation, prefer that one
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

            // Add Echoes
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
        }

        // ----- PASSAGE TOKENS -----

        /// <summary>
        /// Stores fake passage token UI elements
        /// </summary>
        private static ConditionalWeakTable<EndgameTokens, List<FakeEndgameToken>> passageTokensUI = new();

        // Add a button to SleepAndDeathScreen allowing free passage to the starting shelter
        private static ConditionalWeakTable<SleepAndDeathScreen, SimpleButton> passageHomeButton = new();
        public static SimpleButton GetPassageHomeButton(this SleepAndDeathScreen self)
        {
            if (passageHomeButton.TryGetValue(self, out SimpleButton button))
            {
                return button;
            }
            return null;
        }
        public static void CreatePassageHomeButton(this SleepAndDeathScreen self)
        {
            SimpleButton button = new(self, self.pages[0], self.Translate("RETURN HOME"), "RETURN_HOME",
                new Vector2(self.LeftHandButtonsPosXAdd, 60f), new Vector2(110f, 30f));
            passageHomeButton.Add(self, button);
            self.pages[0].subObjects.Add(button);
            button.lastPos = button.pos;
        }

        /// <summary>
        /// Replace normal passage token list with custom one that contains tokens collected from items rather than tokens from completed passages.
        /// Also adds the "Passage to Home" button
        /// </summary>
        private static void OnEndgameTokensCtor(On.Menu.EndgameTokens.orig_ctor orig, EndgameTokens self, Menu.Menu menu, MenuObject owner, Vector2 pos, FContainer container, KarmaLadder ladder)
        {
            orig(self, menu, owner, pos, container, ladder);
            if (!RandoOptions.GivePassageItems) return;

            // We won't be needing these
            foreach (EndgameTokens.Token token in self.tokens)
            {
                token.symbolSprite.RemoveFromContainer();
                token.circleSprite.RemoveFromContainer();
                token.glowSprite.RemoveFromContainer();
            }

            List<FakeEndgameToken> fakePassageTokens = [];
            int index = 0;
            foreach (WinState.EndgameID id in Plugin.RandoManager.GetPassageTokensStatus().Keys)
            {
                if (id == MoreSlugcatsEnums.EndgameID.Gourmand) continue;

                if (Plugin.RandoManager.HasPassageToken(id) is true
                    && (menu as KarmaLadderScreen).winState.GetTracker(id, true).consumed == false)
                {
                    fakePassageTokens.Add(new FakeEndgameToken(menu, self, Vector2.zero, id, container, index));
                    self.subObjects.Add(fakePassageTokens.Last());
                    index++;
                }
            }
            passageTokensUI.Add(self, fakePassageTokens);

            // Skip adding button if Riv in Bitter Aerie
            SaveState state = (menu as SleepAndDeathScreen).saveState;
            if (ModManager.MSC
                && state.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet
                && state.miscWorldSaveData.moonHeartRestored
                && !state.deathPersistentSaveData.altEnding)
            {
                return;
            }

            // Add passage to home button
            (menu as SleepAndDeathScreen).CreatePassageHomeButton();
        }

        /// <summary>
        /// Update "Passage to Home" button on menu update
        /// </summary>
        private static void OnSleepAndDeathScreenUpdate(On.Menu.SleepAndDeathScreen.orig_Update orig, SleepAndDeathScreen self)
        {
            orig(self);
            SimpleButton button = self.GetPassageHomeButton();
            if (button is null) return;

            button.buttonBehav.greyedOut = self.ButtonsGreyedOut || self.goalMalnourished;
            button.black = Mathf.Max(0f, button.black - 0.0125f);
        }

        private static string OnSleepAndDeathScreenUpdateInfoText(On.Menu.SleepAndDeathScreen.orig_UpdateInfoText orig, SleepAndDeathScreen self)
        {
            string origResult = orig(self);

            if (self.selectedObject is SimpleButton button
                && button.signalText.Equals("RETURN_HOME"))
            {
                return self.Translate("Fast travel to the shelter you started the campaign in");
            }
            return origResult;
        }

        /// <summary>
        /// Detect "Passage to Home" button signal and initiate passage
        /// </summary>
        private static void OnSleepAndDeathScreenSingal(On.Menu.SleepAndDeathScreen.orig_Singal orig, SleepAndDeathScreen self, MenuObject sender, string message)
        {
            orig(self, sender, message);

            if (message is not null && message.Equals("RETURN_HOME"))
            {
                // Set startup condition
                self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.FastTravel;

                // Find den to travel to
                string customDen = Plugin.RandoManager.customStartDen;
                if (!RandoOptions.RandomizeSpawnLocation || customDen.Equals("NONE"))
                {
                    customDen = Constants.SlugcatDefaultStartingDen[self.saveState.saveStateNumber];
                }

                // Set required fields to ensure proper transition
                self.manager.menuSetup.regionSelectRoom = customDen;
                self.manager.rainWorld.progression.miscProgressionData.menuRegion = Regex.Split(customDen, "_")[0];
                RainWorld.ShelterBeforePassage = self.manager.rainWorld.progression.ShelterOfSaveGame(self.saveState.saveStateNumber);
                RainWorld.ShelterAfterPassage = self.manager.menuSetup.regionSelectRoom;

                // Initiate proccess switch
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                self.PlaySound(SoundID.MENU_Passage_Button);
            }
        }

        /// <summary>
        /// Make passage button appear if any token items have been found
        /// </summary>
        private static void EndgameTokensCtorIL(ILContext il)
        {
            ILCursor c = new(il);

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
            ILCursor c = new(il);

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
            ILCursor c = new(il);

            ILLabel yesEndgamesJump = c.DefineLabel();

            // Before check if slugcat is Hunter at 0581
            c.GotoNext(x => x.MatchLdsfld(typeof(SlugcatStats.Name).GetField(nameof(SlugcatStats.Name.Red))));
            c.GotoPrev(MoveType.AfterLabel, x => x.MatchLdarg(1));

            // Unconditional jump to spawn tokens code
            c.Emit(OpCodes.Br, yesEndgamesJump);

            // Define jump position to 059C
            c.GotoNext(x => x.MatchBrtrue(out _));
            c.GotoNext(MoveType.After, x => x.MatchBrtrue(out _));
            c.MarkLabel(yesEndgamesJump);
        }

        /// <summary>
        /// Override normal UI update when passage triggered in favor of custom one
        /// </summary>
        private static void DoPassage(On.Menu.EndgameTokens.orig_Passage orig, EndgameTokens self, WinState.EndgameID ID)
        {
            orig(self, ID);
            if (!RandoOptions.GivePassageItems) return;

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
            if (Plugin.RandoManager?.isRandomizerActive is not true || !RandoOptions.GivePassageItems) return orig(self);

            foreach (var passage in Plugin.RandoManager.GetPassageTokensStatus())
            {
                if (passage.Value && !self.GetTracker(passage.Key, true).consumed)
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
            if (!Plugin.RandoManager.isRandomizerActive || !RandoOptions.GivePassageItems)
            {
                orig(self);
                return;
            }

            foreach (var passage in Plugin.RandoManager.GetPassageTokensStatus())
            {
                if (passage.Value && !self.GetTracker(passage.Key, true).consumed)
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
