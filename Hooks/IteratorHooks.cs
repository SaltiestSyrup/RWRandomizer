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
                IL.SSOracleBehavior.SSOracleMeetWhite.Update += PebblesMeetWhiteUpdateIL;
                IL.SSOracleBehavior.SSOracleMeetYellow.Update += PebblesMeetYellowOrGourmandUpdateIL;
                IL.SSOracleBehavior.SSOracleMeetGourmand.Update += PebblesMeetYellowOrGourmandUpdateIL;
                IL.SSOracleBehavior.SSOracleMeetArty.Update += PebblesMeetArtiUpdateIL;
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

            IL.SSOracleBehavior.SSOracleMeetWhite.Update -= PebblesMeetWhiteUpdateIL;
            IL.SSOracleBehavior.SSOracleMeetYellow.Update -= PebblesMeetYellowOrGourmandUpdateIL;
            IL.SSOracleBehavior.SSOracleMeetGourmand.Update -= PebblesMeetYellowOrGourmandUpdateIL;
            IL.SSOracleBehavior.SSOracleMeetArty.Update -= PebblesMeetArtiUpdateIL;
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
        static void EatenNeuron(Player player)
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
        /// Hack Pebbles to give the mark when he otherwise wouldn't
        /// </summary>
        static void PebblesMeetWhiteUpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
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
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdfld<SSOracleBehavior.SubBehavior>(nameof(SSOracleBehavior.SubBehavior.owner)),
                    x => x.MatchLdfld<SSOracleBehavior>(nameof(SSOracleBehavior.playerEnteredWithMark))
                    );

                c.MoveAfterLabels();

                // Force this check to always return false
                c.Index += 2;
                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<bool>>(() => false);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for PebblesUpdateYellow");
                Plugin.Log.LogError(e);
            }
        }

        /// <summary>
        /// Hack Pebbles to behave properly in strange randomizer circumstances for Artificer
        /// </summary>
        /// <param name="il"></param>
        static void PebblesMeetArtiUpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    MoveType.Before,
                    x => x.MatchStfld(typeof(SaveState).GetField(nameof(SaveState.hasRobo)))
                    );

                c.Emit(OpCodes.Pop); // Only set robo if it has been given
                c.EmitDelegate<Func<bool>>(() => { return Plugin.RandoManager.GivenRobo; });

                // ------
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(SSOracleBehavior.SubBehavior).GetField(nameof(SSOracleBehavior.SubBehavior.owner))),
                    x => x.MatchLdsfld(typeof(MoreSlugcats.MoreSlugcatsEnums.SSOracleBehaviorAction).GetField(nameof(MoreSlugcats.MoreSlugcatsEnums.SSOracleBehaviorAction.MeetArty_Talking))),
                    x => x.MatchStfld(typeof(SSOracleBehavior).GetField(nameof(SSOracleBehavior.afterGiveMarkAction)))
                    );

                // Throw Arty out after trying to give mark if no robot
                c.Index--;
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldsfld, typeof(SSOracleBehavior.Action).GetField(nameof(SSOracleBehavior.Action.ThrowOut_ThrowOut)));

                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Callvirt, typeof(SSOracleBehavior.SubBehavior).GetMethod(nameof(SSOracleBehavior.SubBehavior.Deactivate)));

                // ------
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(SSOracleBehavior.SSOracleMeetArty).GetField(nameof(SSOracleBehavior.SSOracleMeetArty.player), BindingFlags.NonPublic | BindingFlags.Instance)),
                    x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.myRobot))),
                    x => x.MatchLdflda(out _),
                    x => x.MatchInitobj(out _)
                    );

                ILLabel jump = c.MarkLabel();

                // Add a null check for this.player.myRobot
                c.Index -= 5;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(SSOracleBehavior.SSOracleMeetArty).GetField(nameof(SSOracleBehavior.SSOracleMeetArty.player), BindingFlags.NonPublic | BindingFlags.Instance));
                c.Emit(OpCodes.Ldfld, typeof(Player).GetField(nameof(Player.myRobot)));
                c.EmitDelegate<Func<MoreSlugcats.AncientBot, bool>>(r => { return r == null; });
                c.Emit(OpCodes.Brfalse, jump);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for PebblesMeetArtiUpdateIL");
                Plugin.Log.LogError(e);
            }
        }

        /// <summary>
        /// Detect Rivulet taking Energy Cell from Pebbles and handle randomizer weirdness in certain conditions
        /// </summary>
        /// <param name="il"></param>
        static void RotCoreRoomUpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);

                // Make the game think the power is still on if we turned it off
                while(c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(UpdatableAndDeletable).GetField(nameof(UpdatableAndDeletable.room))),
                    x => x.MatchLdfld(typeof(Room).GetField(nameof(Room.game))),
                    x => x.MatchLdfld(typeof(RainWorldGame).GetField(nameof(RainWorldGame.session))),
                    x => x.MatchIsinst(typeof(StoryGameSession)),
                    x => x.MatchLdfld(typeof(StoryGameSession).GetField(nameof(StoryGameSession.saveState))),
                    x => x.MatchLdfld(typeof(SaveState).GetField(nameof(SaveState.miscWorldSaveData))),
                    x => x.MatchLdfld(typeof(MiscWorldSaveData).GetField(nameof(MiscWorldSaveData.pebblesEnergyTaken)))
                    ))
                {
                    //c.Index--;
                    c.EmitDelegate<Func<bool, bool>>((energyTaken) =>
                    {
                        if (Options.UseEnergyCell)
                        {
                            return Plugin.RandoManager.IsLocationGiven("Kill_FP") ?? false;
                        }
                        return energyTaken;
                    });
                }

                ILCursor c1 = new ILCursor(il);

                // Skip over code for giving player the Mass Rarefaction cell
                c1.GotoNext(
                    MoveType.After,
                    x => x.MatchCallOrCallvirt(typeof(HUD.TextPrompt).GetMethod(nameof(HUD.TextPrompt.AddMessage), 
                        new Type[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool) }))
                    );

                ILLabel jump = c1.MarkLabel();

                c1.GotoPrev(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(MoreSlugcats.MSCRoomSpecificScript.RM_CORE_EnergyCell).GetField(nameof(MoreSlugcats.MSCRoomSpecificScript.RM_CORE_EnergyCell.myEnergyCell), BindingFlags.NonPublic | BindingFlags.Instance)),
                    x => x.MatchCallOrCallvirt(typeof(Room).GetMethod(nameof(Room.RemoveObject)))
                    );
                c1.MoveAfterLabels();

                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate<Func<MoreSlugcats.MSCRoomSpecificScript.RM_CORE_EnergyCell, bool>>(self =>
                {
                    if (Options.UseEnergyCell)
                    {
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
                    else
                    {
                        return false;
                    }
                });
                c1.Emit(OpCodes.Brtrue, jump);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for RotCoreRoomUpdateIL");
                Plugin.Log.LogError(e);
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
        /// <param name="il"></param>
        static void ILMoonWakeUpUpdate(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);

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
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
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
            }

            orig(self, eu);
        }

        /// <summary>
        /// Fix Riv ending cutscene to behave properly without the mark
        /// </summary>
        /// <param name="il"></param>
        static void OracleCtorIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

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
            if (Plugin.RandoManager is ManagerArchipelago managerAP
                && !managerAP.gameCompleted
                && eventName == "RivEndingFade")
            {
                managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SaveMoon);
            }
        }

        /// <summary>
        /// Allow Pebbles to do a violence on Arty if they don't have the drone, and detect Pebbles killing Inv
        /// </summary>
        /// <param name="il"></param>
        static void IteratorThrowOutBehaviorIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);

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

                c.EmitDelegate<Func<bool>>(() => { return Plugin.RandoManager.GivenRobo; });
                c.Emit(OpCodes.Brfalse, jump);


                ILCursor c1 = new ILCursor(il);

                // Inv's Meet FP check is given when killed by FP
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchCallOrCallvirt(typeof(SSOracleBehavior.SubBehavior).GetProperty(nameof(SSOracleBehavior.SubBehavior.player)).GetGetMethod()),
                    x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.Die)))
                    );
                c.EmitDelegate<Action>(() =>
                {
                    if (ModManager.MSC && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                    {
                        Plugin.RandoManager.GiveLocation("Meet_FP");
                    }
                });
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for IteratorThrowOutBehaviorIL");
                Plugin.Log.LogError(e);
            }
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

            Room room = Plugin.Singleton.Game.session.Players[0].realizedCreature.room;
            for (int i = 0; i < room.physicalObjects.Length; i++)
            {
                for (int j = 0; j < room.physicalObjects[i].Count; j++)
                {
                    // If this object is Five Pebbles and he is talking
                    if (room.physicalObjects[i][j] is Oracle
                        && ((Oracle)room.physicalObjects[i][j]).oracleBehavior is SSOracleBehavior oracle
                        && oracle.currSubBehavior is SSOracleBehavior.TalkBehavior)
                    {
                        SoundID sound;
                        int pause = 0;

                        if (ModManager.MSC && oracle.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
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
                        

                        oracle.voice = oracle.oracle.room.PlaySound(sound, oracle.oracle.firstChunk);
                        oracle.voice.requireActiveUpkeep = true;
                        if (oracle.conversation != null)
                        {
                            oracle.conversation.waitForStill = true;
                        }
                        ((SSOracleBehavior.TalkBehavior)oracle.currSubBehavior).communicationPause = pause;
                        return;
                    }
                }
            }
        }
    }
}
