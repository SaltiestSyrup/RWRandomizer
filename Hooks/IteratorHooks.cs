using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Reflection;

namespace RainWorldRandomizer
{
    public static class IteratorHooks
    {
        public static void ApplyHooks()
        {
            On.OracleSwarmer.BitByPlayer += OnEatNeuron;
            On.SLOracleSwarmer.BitByPlayer += OnEatNeuron;
            On.SLOracleBehavior.ConvertingSSSwarmer += OnGiftNeuron;
            On.SSOracleBehavior.Update += OnSSOracleBehaviorUpdate;
            On.SLOracleBehaviorHasMark.Update += MoonMarkUpdate;
            On.SLOracleWakeUpProcedure.Update += MoonWakeUpUpdate;
            On.SLOracleBehaviorHasMark.SpecialEvent += OnMoonSpecialEvent;
            On.HUD.DialogBox.NewMessage_string_float_float_int += DialogueAddMessage;

            try
            {
                IL.SSOracleBehavior.Update += SSOracleBehaviorUpdateIL;
                IL.SSOracleBehavior.SSOracleMeetWhite.Update += PebblesMeetWhiteUpdateIL;
                IL.SSOracleBehavior.SSOracleMeetYellow.Update += PebblesMeetYellowOrGourmandUpdateIL;
                IL.SSOracleBehavior.SSOracleMeetGourmand.Update += PebblesMeetYellowOrGourmandUpdateIL;
                IL.SSOracleBehavior.SSOracleMeetArty.Update += PebblesMeetArtyUpdateIL;
                IL.SSOracleBehavior.ThrowOutBehavior.Update += IteratorThrowOutBehaviorIL;
                IL.SLOracleWakeUpProcedure.Update += ILMoonWakeUpUpdate;
                IL.MoreSlugcats.MSCRoomSpecificScript.RM_CORE_EnergyCell.Update += RotCoreRoomUpdateIL;
                IL.Oracle.ctor += OracleCtorIL;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.OracleSwarmer.BitByPlayer -= OnEatNeuron;
            On.SLOracleSwarmer.BitByPlayer -= OnEatNeuron;
            On.SLOracleBehavior.ConvertingSSSwarmer -= OnGiftNeuron;
            On.SSOracleBehavior.Update -= OnSSOracleBehaviorUpdate;
            On.SLOracleBehaviorHasMark.Update -= MoonMarkUpdate;
            On.SLOracleWakeUpProcedure.Update -= MoonWakeUpUpdate;
            On.SLOracleBehaviorHasMark.SpecialEvent -= OnMoonSpecialEvent;
            On.HUD.DialogBox.NewMessage_string_float_float_int -= DialogueAddMessage;

            IL.SSOracleBehavior.Update -= SSOracleBehaviorUpdateIL;
            IL.SSOracleBehavior.SSOracleMeetWhite.Update -= PebblesMeetWhiteUpdateIL;
            IL.SSOracleBehavior.SSOracleMeetYellow.Update -= PebblesMeetYellowOrGourmandUpdateIL;
            IL.SSOracleBehavior.SSOracleMeetGourmand.Update -= PebblesMeetYellowOrGourmandUpdateIL;
            IL.SSOracleBehavior.SSOracleMeetArty.Update -= PebblesMeetArtyUpdateIL;
            IL.SSOracleBehavior.ThrowOutBehavior.Update -= IteratorThrowOutBehaviorIL;
            IL.SLOracleWakeUpProcedure.Update -= ILMoonWakeUpUpdate;
            IL.MoreSlugcats.MSCRoomSpecificScript.RM_CORE_EnergyCell.Update -= RotCoreRoomUpdateIL;
            IL.Oracle.ctor -= OracleCtorIL;
        }

        /// <summary>
        /// Detect eating of generic neuron
        /// </summary>
        static void OnEatNeuron(On.OracleSwarmer.orig_BitByPlayer orig, OracleSwarmer self, Creature.Grasp grasp, bool eu)
        {
            orig(self, grasp, eu);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            if (self.bites < 1)
            {
                EatenNeuron(grasp.grabber as Player);
            }
        }

        /// <summary>
        /// Detect eating of LttM neuron
        /// </summary>
        static void OnEatNeuron(On.SLOracleSwarmer.orig_BitByPlayer orig, SLOracleSwarmer self, Creature.Grasp grasp, bool eu)
        {
            orig(self, grasp, eu);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            if (self.bites < 1)
            {
                EatenNeuron(grasp.grabber as Player);
            }
        }

        /// <summary>
        /// Revert the normal effects of eating a neuron and award check
        /// </summary>
        public static void EatenNeuron(Player player)
        {
            // Remove unearned glowing effect
            if (!Plugin.RandoManager.GivenNeuronGlow)
            {
                player.room.game.GetStorySession.saveState.theGlow = false;
                player.glowing = false;
            }

            Plugin.RandoManager.GiveLocation("Eat_Neuron");
        }

        /// <summary>
        /// Detect gifting a neuron to LttM
        /// </summary>
        static void OnGiftNeuron(On.SLOracleBehavior.orig_ConvertingSSSwarmer orig, SLOracleBehavior self)
        {
            orig(self);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            Plugin.RandoManager.GiveLocation("Gift_Neuron");
        }

        /// <summary>
        /// Detect Pebbles (and intact LttM) giving mark and revert effects of such
        /// </summary>
        static void OnSSOracleBehaviorUpdate(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            if (!Plugin.RandoManager.isRandomizerActive)
            {
                orig(self, eu);
                return;
            }

            orig(self, eu);

            // Pebbles gives the mark
            if (self.action == SSOracleBehavior.Action.General_GiveMark && self.inActionCounter == 300)
            {
                //Logger.LogDebug($"Gave the mark! Iterator ID: {self.oracle.ID}");
                // No karma increases >:(
                self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap = Plugin.RandoManager.CurrentMaxKarma;
                self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karma = Plugin.RandoManager.CurrentMaxKarma;
                for (int num2 = 0; num2 < self.oracle.room.game.cameras.Length; num2++)
                {
                    self.oracle.room.game.cameras[num2].hud.karmaMeter?.UpdateGraphic();
                }

                // Reset the mark if not unlocked yet
                if (!Plugin.RandoManager.GivenMark)
                {
                    self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark = false;
                    //self.afterGiveMarkAction = SSOracleBehavior.Action.ThrowOut_ThrowOut;
                }

                if (self.oracle.ID == Oracle.OracleID.SS)
                {
                    Plugin.RandoManager.GiveLocation("Meet_FP");
                }
                else if (ModManager.MSC && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                {
                    Plugin.RandoManager.GiveLocation("Meet_LttM_Spear");
                }
            }
        }

        /// <summary>
        /// Make Pebbles not ignore Artificer if they don't have a robot
        /// </summary>
        /// <param name="il"></param>
        static void SSOracleBehaviorUpdateIL(ILContext il)
        {
            ILCursor c = new(il);

            // Check if player has a robot at 0DDB
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.myRobot)))
                );

            c.EmitDelegate(AllPlayersHaveRobot);

            static bool AllPlayersHaveRobot(AncientBot foundRobot) => true;
        }

        /// <summary>
        /// Hack Pebbles to give the mark when he otherwise wouldn't
        /// </summary>
        // TODO: Rewrite Pebbles meet white hook, goto is volatile
        static void PebblesMeetWhiteUpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdfld(typeof(StoryGameSession).GetField(nameof(StoryGameSession.saveState))),
                    x => x.MatchLdfld(typeof(SaveState).GetField(nameof(SaveState.deathPersistentSaveData))),
                    x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark))),
                    x => x.MatchBrfalse(out _),
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchLdcI4(40),
                    x => x.MatchBle(out _),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(SSOracleBehavior.SubBehavior).GetField(nameof(SSOracleBehavior.SubBehavior.owner))),
                    x => x.MatchLdsfld(typeof(SSOracleBehavior.Action).GetField(nameof(SSOracleBehavior.Action.General_MarkTalk)))
                    );

                c.MoveAfterLabels();

                // Force pebbles to 'give the mark' to the player regardless of them already having it
                c.Emit(OpCodes.Pop);
                //c.Emit(OpCodes.Ldarg_0);
                //c.Emit(OpCodes.Ldfld, typeof(SSOracleBehavior.SubBehavior).GetField(nameof(SSOracleBehavior.SubBehavior.owner)));
                c.Emit(OpCodes.Ldsfld, typeof(SSOracleBehavior.Action).GetField(nameof(SSOracleBehavior.Action.General_GiveMark)));

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchCallOrCallvirt(typeof(SSOracleBehavior).GetMethod(nameof(SSOracleBehavior.NewAction)))
                    );

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(SSOracleBehavior.SubBehavior).GetField(nameof(SSOracleBehavior.SubBehavior.owner)));
                c.Emit(OpCodes.Ldsfld, typeof(SSOracleBehavior.Action).GetField(nameof(SSOracleBehavior.Action.General_MarkTalk)));
                c.Emit(OpCodes.Stfld, typeof(SSOracleBehavior).GetField(nameof(SSOracleBehavior.afterGiveMarkAction)));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for PebblesUpdateWhite");
                Plugin.Log.LogError(e);
            }
        }

        /// <summary>
        /// Hack Pebbles to give the mark when he otherwise wouldn't for Monk / Gourm
        /// </summary>
        static void PebblesMeetYellowOrGourmandUpdateIL(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(
                x => x.MatchLdfld<SSOracleBehavior.SubBehavior>(nameof(SSOracleBehavior.SubBehavior.owner)),
                x => x.MatchLdfld<SSOracleBehavior>(nameof(SSOracleBehavior.playerEnteredWithMark))
                );

            c.MoveAfterLabels();

            // Force this check to always return false
            c.Index += 2;
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(() => false);
        }

        /// <summary>
        /// Make Pebbles act correctly for Arty
        /// </summary>
        static void PebblesMeetArtyUpdateIL(ILContext il)
        {
            ILCursor c = new(il);

            // Before assigning the player at 0041
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(OracleBotResync).GetMethod(nameof(OracleBotResync.PlayerWithRobot)))
                );

            // If the player doesn't have the robot yet, make sure Pebbles doesn't ignore them
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(AssignDefaultPlayerIfNoRobot);

            static Player AssignDefaultPlayerIfNoRobot(Player foundPlayerWithRobot, SSOracleBehavior.SSOracleMeetArty self)
            {
                if (foundPlayerWithRobot is not null) return foundPlayerWithRobot;
                return self.oracle.room.game.FirstRealizedPlayer;
            }

            // Check if player has the mark at 00CF
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark)))
                );

            // Make sure check is given on the first meeting, and only once
            c.EmitDelegate(ShouldPebblesNotGiveMark);

            static bool ShouldPebblesNotGiveMark(bool hasTheMark)
            {
                return Plugin.RandoManager.IsLocationGiven("Meet_FP") is true;
            }


            // Before assigning afterGiveMarkAction at 0116
            c.GotoNext(
                MoveType.Before,
                x => x.MatchStfld(typeof(SSOracleBehavior).GetField(nameof(SSOracleBehavior.afterGiveMarkAction)))
                );

            // Throw Arty out after trying to give mark if no robot
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(ThrowOutIfNoRobo);

            static SSOracleBehavior.Action ThrowOutIfNoRobo(SSOracleBehavior.Action origNextAction, SSOracleBehavior.SSOracleMeetArty self)
            {
                if (self.oracle.room.game.GetStorySession.saveState.hasRobo) return origNextAction;

                self.Deactivate();
                return SSOracleBehavior.Action.ThrowOut_ThrowOut;
            }
        }

        /// <summary>
        /// Detect Rivulet taking Energy Cell from Pebbles and handle randomizer weirdness in certain conditions
        /// </summary>
        /// <param name="il"></param>
        static void RotCoreRoomUpdateIL(ILContext il)
        {
            ILCursor c = new(il);

            // Make the game think the power is still on if we turned it off
            while (c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(SaveState).GetField(nameof(SaveState.miscWorldSaveData))),
                x => x.MatchLdfld(typeof(MiscWorldSaveData).GetField(nameof(MiscWorldSaveData.pebblesEnergyTaken)))
                ))
            {
                c.EmitDelegate(ReplaceWithLocationCheck);
            }

            static bool ReplaceWithLocationCheck(bool energyTaken)
            {
                return RandoOptions.UseEnergyCell ? Plugin.RandoManager.IsLocationGiven("Kill_FP") ?? false : energyTaken;
            }

            ILCursor c1 = new(il);

            // Skip over code for giving player the Mass Rarefaction cell
            c1.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(HUD.TextPrompt).GetMethod(nameof(HUD.TextPrompt.AddMessage),
                    [typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool)]))
                );

            ILLabel jump = c1.MarkLabel();

            c1.GotoPrev(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(MSCRoomSpecificScript.RM_CORE_EnergyCell)
                    .GetField(nameof(MSCRoomSpecificScript.RM_CORE_EnergyCell.myEnergyCell), BindingFlags.NonPublic | BindingFlags.Instance)),
                x => x.MatchCallOrCallvirt(typeof(Room).GetMethod(nameof(Room.RemoveObject)))
                );
            c1.MoveAfterLabels();

            c1.Emit(OpCodes.Ldarg_0);
            c1.EmitDelegate(EnergyCellCheck);
            c1.Emit(OpCodes.Brtrue, jump);

            static bool EnergyCellCheck(MSCRoomSpecificScript.RM_CORE_EnergyCell self)
            {
                if (!RandoOptions.UseEnergyCell) return false;

                Plugin.RandoManager.GiveLocation("Kill_FP");

                // If power is not supposed to be off yet, turn it back on
                if (!Plugin.RandoManager.GivenPebblesOff)
                {
                    (self.room.game.session as StoryGameSession).saveState.miscWorldSaveData.pebblesEnergyTaken = false;
                }

                self.myEnergyCell = null;
                self.ReloadRooms();
                return true;
            }
        }

        /// <summary>
        /// Detect meeting LttM for the first time with the mark
        /// </summary>
        static void MoonMarkUpdate(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
        {
            orig(self, eu);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            // Meeting for the first time
            if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.playerEncountersWithMark > 0)
            {
                Plugin.RandoManager.GiveLocation("Meet_LttM");
            }
        }

        /// <summary>
        /// Fix LttM wake up cutscene for Hunter to not break without the mark
        /// </summary>
        static void ILMoonWakeUpUpdate(ILContext il)
        {
            ILCursor c = new(il);

            // Replace every instance of casting to SLOracleBehaviorHasMark with the base class
            while (c.TryGotoNext(
                MoveType.Before,
                x => x.MatchIsinst(typeof(SLOracleBehaviorHasMark))
                ))
            {
                Instruction jump = c.Next.Next;
                c.Emit(OpCodes.Isinst, typeof(SLOracleBehavior));
                c.Emit(OpCodes.Br, jump);
                c.Index++;
            }
        }

        /// <summary>
        /// Detect Hunter reviving LttM
        /// </summary>
        static void MoonWakeUpUpdate(On.SLOracleWakeUpProcedure.orig_Update orig, SLOracleWakeUpProcedure self, bool eu)
        {
            if (self.phase == SLOracleWakeUpProcedure.Phase.Done)
            {
                Plugin.RandoManager.GiveLocation("Save_LttM");
                (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.HelpingHand);
            }

            orig(self, eu);
        }

        /// <summary>
        /// Fix Riv ending cutscene to behave properly without the mark
        /// </summary>
        static void OracleCtorIL(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark)))
                );

            // Tell LttM we have the mark if this should be riv ending scene
            c.EmitDelegate<Func<bool, bool>>((hasMark) =>
            {
                return hasMark ||
                    (ModManager.MSC
                    && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Rivulet
                    && Plugin.Singleton.Game.IsMoonActive());
            });
        }

        /// <summary>
        /// Detect Rivulet LttM ending trigger
        /// </summary>
        static void OnMoonSpecialEvent(On.SLOracleBehaviorHasMark.orig_SpecialEvent orig, SLOracleBehaviorHasMark self, string eventName)
        {
            orig(self, eventName);

            // Check for completion via visiting LttM after placing the Rarefaction cell
            if (eventName == "RivEndingFade")
            {
                (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SaveMoon);
            }
        }

        /// <summary>
        /// Allow Pebbles to do a violence on Arty if they don't have the drone, and detect Pebbles killing Inv
        /// </summary>
        static void IteratorThrowOutBehaviorIL(ILContext il)
        {
            ILCursor c = new(il);

            // Add an extra condition for Artificer actually having the Citizen ID drone to not be killed by Pebbles
            ILLabel jump = null;
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.StoryCharacter)).GetGetMethod()),
                //x => x.MatchLdfld(typeof(StoryGameSession).GetField(nameof(StoryGameSession.saveStateNumber))),
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer))),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchBrfalse(out jump)
                );

            c.EmitDelegate(() => { return Plugin.RandoManager.GivenRobo; });
            c.Emit(OpCodes.Brfalse, jump);


            ILCursor c1 = new(il);

            // Inv's Meet FP check is given when killed by FP
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(SSOracleBehavior.SubBehavior).GetProperty(nameof(SSOracleBehavior.SubBehavior.player)).GetGetMethod()),
                x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.Die)))
                );
            c.EmitDelegate(() =>
            {
                if (ModManager.MSC && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                {
                    Plugin.RandoManager.GiveLocation("Meet_FP");
                }
            });
        }

        /// <summary>
        /// Modify Iterators to use speech sounds if mark is not aquired
        /// </summary>
        static void DialogueAddMessage(On.HUD.DialogBox.orig_NewMessage_string_float_float_int orig, HUD.DialogBox self, string text, float xOrientation, float yPos, int extraLinger)
        {
            // Swap Pebbles dialogue for gibberish if mark not obtained
            if (Plugin.RandoManager.GivenMark
                || (ModManager.MSC && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint))
            {
                orig(self, text, xOrientation, yPos, extraLinger);
                return;
            }

            // Don't try to play sound if player doesn't exist
            if (self.hud.owner is null
                || self.hud.owner is not Player player
                || player.room is null)
            {
                return;
            }

            Room room = player.room;
            for (int i = 0; i < room.physicalObjects.Length; i++)
            {
                for (int j = 0; j < room.physicalObjects[i].Count; j++)
                {
                    // If this object is an SSOracle and they are talking
                    if (room.physicalObjects[i][j] is Oracle oracle
                        && oracle.oracleBehavior is SSOracleBehavior oracleBehavior
                        && oracleBehavior.currSubBehavior is SSOracleBehavior.TalkBehavior oracleTalkBehavior)
                    {
                        SoundID sound;
                        int pause;

                        // Use random identity appropriate chatter
                        if (ModManager.MSC && oracleBehavior.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                        {
                            switch (UnityEngine.Random.Range(0, 4))
                            {
                                case 0:
                                    sound = SoundID.SL_AI_Talk_1;
                                    pause = 100;
                                    break;
                                case 1:
                                    sound = SoundID.SL_AI_Talk_2;
                                    pause = 200;
                                    break;
                                case 2:
                                    sound = SoundID.SL_AI_Talk_3;
                                    pause = 200;
                                    break;
                                case 3:
                                default:
                                    sound = SoundID.SL_AI_Talk_4;
                                    pause = 100;
                                    break;
                            }
                        }
                        else
                        {
                            switch (UnityEngine.Random.Range(0, 4))
                            {
                                case 0:
                                    sound = SoundID.SS_AI_Talk_1;
                                    pause = 100;
                                    break;
                                case 1:
                                    sound = SoundID.SS_AI_Talk_2;
                                    pause = 200;
                                    break;
                                case 2:
                                    sound = SoundID.SS_AI_Talk_3;
                                    pause = 200;
                                    break;
                                case 3:
                                default:
                                    sound = SoundID.SS_AI_Talk_4;
                                    pause = 100;
                                    break;
                            }
                        }


                        oracleBehavior.voice = oracle.room.PlaySound(sound, oracle.firstChunk);
                        oracleBehavior.voice.requireActiveUpkeep = true;
                        if (oracleBehavior.conversation is not null)
                        {
                            oracleBehavior.conversation.waitForStill = true;
                        }
                        oracleTalkBehavior.communicationPause = pause;
                        return;
                    }
                }
            }
        }
    }
}
