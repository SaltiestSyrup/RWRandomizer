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
            On.SLOrcacleState.FromString += SLOrcacleStateOnFromString;

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
            On.SLOrcacleState.FromString -= SLOrcacleStateOnFromString;

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
        private static void OnEatNeuron(On.OracleSwarmer.orig_BitByPlayer orig, OracleSwarmer self, Creature.Grasp grasp, bool eu)
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
        private static void OnEatNeuron(On.SLOracleSwarmer.orig_BitByPlayer orig, SLOracleSwarmer self, Creature.Grasp grasp, bool eu)
        {
            orig(self, grasp, eu);
            if (!Plugin.RandomizerActive) return;

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
            if (!Plugin.RandomizerActive) return;
            
            // Remove unearned glowing effect
            if (!Plugin.RandoManager.GivenNeuronGlow)
            {
                player.room.game.GetStorySession.saveState.theGlow = false;
                player.glowing = false;
            }

            Plugin.RandoManager.GiveLocation("Eat_Neuron");
        }
        
        /// <summary>
        /// If LttM dies, some checks involving her become impossible. This makes sure she always has at least one neuron
        /// </summary>
        private static void SLOrcacleStateOnFromString(On.SLOrcacleState.orig_FromString orig, SLOrcacleState self, string s)
        {
            orig(self, s);
            if (Plugin.RandomizerActive && self.neuronsLeft == 0) self.neuronsLeft = 1;
        }

        /// <summary>
        /// Detect gifting a neuron to LttM
        /// </summary>
        private static void OnGiftNeuron(On.SLOracleBehavior.orig_ConvertingSSSwarmer orig, SLOracleBehavior self)
        {
            orig(self);
            if (!Plugin.RandomizerActive) return;

            Plugin.RandoManager.GiveLocation("Gift_Neuron");
        }

        /// <summary>
        /// Detect Pebbles (and intact LttM) giving mark and revert effects of such
        /// </summary>
        private static void OnSSOracleBehaviorUpdate(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (!Plugin.RandomizerActive) return;

            // Pebbles gives the mark
            if (self.action == SSOracleBehavior.Action.General_GiveMark && self.inActionCounter == 300)
            {
                //Logger.LogDebug($"Gave the mark! Iterator ID: {self.oracle.ID}");
                // No karma increases >:(
                self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap = Plugin.RandoManager.CurrentMaxKarma;
                self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karma = Plugin.RandoManager.CurrentMaxKarma;
                foreach (var camera in self.oracle.room.game.cameras)
                {
                    camera.hud.karmaMeter?.UpdateGraphic();
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
        private static void SSOracleBehaviorUpdateIL(ILContext il)
        {
            ILCursor c = new(il);

            // Check if player has a robot at 0DDB
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.myRobot)))
                );

            c.EmitDelegate(AllPlayersHaveRobot);
            return;

            static bool AllPlayersHaveRobot(AncientBot foundRobot) => Plugin.RandomizerActive;
        }

        /// <summary>
        /// Modify Pebbles to give the mark when he otherwise wouldn't
        /// </summary>
        private static void PebblesMeetWhiteUpdateIL(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(x =>
                x.MatchLdsfld(typeof(SSOracleBehavior.Action).GetField(nameof(SSOracleBehavior.Action.General_MarkTalk))));
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld(
                    typeof(SSOracleBehavior.Action).GetField(nameof(SSOracleBehavior.Action.General_MarkTalk))));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(ForceGiveMark);
            return;

            static SSOracleBehavior.Action ForceGiveMark(SSOracleBehavior.Action origAction, SSOracleBehavior.SubBehavior self)
            {
                if (!Plugin.RandomizerActive) return origAction;

                self.owner.afterGiveMarkAction = SSOracleBehavior.Action.General_MarkTalk;
                return SSOracleBehavior.Action.General_GiveMark;
            }
        }

        /// <summary>
        /// Modify Pebbles to give the mark when he otherwise wouldn't for Monk / Gourm
        /// </summary>
        private static void PebblesMeetYellowOrGourmandUpdateIL(ILContext il)
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
            c.EmitDelegate(() => !Plugin.RandomizerActive);
        }

        /// <summary>
        /// Make Pebbles act correctly for Arty
        /// </summary>
        private static void PebblesMeetArtyUpdateIL(ILContext il)
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

            // Check if player has the mark at 00CF
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark)))
                );

            // Make sure check is given on the first meeting, and only once
            c.EmitDelegate(ShouldPebblesNotGiveMark);

            // Before assigning afterGiveMarkAction at 0116
            c.GotoNext(
                MoveType.Before,
                x => x.MatchStfld(typeof(SSOracleBehavior).GetField(nameof(SSOracleBehavior.afterGiveMarkAction)))
                );

            // Throw Arty out after trying to give mark if no robot
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(ThrowOutIfNoRobo);
            return;

            static Player AssignDefaultPlayerIfNoRobot(Player foundPlayerWithRobot, SSOracleBehavior.SSOracleMeetArty self)
            {
                if (!Plugin.RandomizerActive) return foundPlayerWithRobot;
                return foundPlayerWithRobot ?? self.oracle.room.game.FirstRealizedPlayer;
            }
            
            static bool ShouldPebblesNotGiveMark(bool hasTheMark)
            {
                return Plugin.RandoManager?.IsLocationGiven("Meet_FP") is true;
            }

            static SSOracleBehavior.Action ThrowOutIfNoRobo(SSOracleBehavior.Action origNextAction, SSOracleBehavior.SSOracleMeetArty self)
            {
                if (!Plugin.RandomizerActive
                    || self.oracle.room.game.GetStorySession.saveState.hasRobo) return origNextAction;

                self.Deactivate();
                return SSOracleBehavior.Action.ThrowOut_ThrowOut;
            }
        }

        /// <summary>
        /// Detect Rivulet taking Energy Cell from Pebbles and handle randomizer weirdness in certain conditions
        /// </summary>
        /// <param name="il"></param>
        private static void RotCoreRoomUpdateIL(ILContext il)
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
            return;

            static bool ReplaceWithLocationCheck(bool energyTaken)
            {
                if (!Plugin.RandomizerActive) return energyTaken;
                return RandoOptions.UseEnergyCell ? Plugin.RandoManager.IsLocationGiven("Kill_FP") ?? false : energyTaken;
            }

            static bool EnergyCellCheck(MSCRoomSpecificScript.RM_CORE_EnergyCell self)
            {
                if (!Plugin.RandomizerActive || !RandoOptions.UseEnergyCell) return false;

                Plugin.RandoManager.GiveLocation("Kill_FP");

                // If power is not supposed to be off yet, turn it back on
                if (!Plugin.RandoManager.GivenPebblesOff)
                {
                    self.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = false;
                }

                self.myEnergyCell = null;
                self.ReloadRooms();
                return true;
            }
        }

        /// <summary>
        /// Detect meeting LttM for the first time with the mark
        /// </summary>
        private static void MoonMarkUpdate(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
        {
            orig(self, eu);
            if (!Plugin.RandomizerActive) return;

            // Meeting for the first time
            if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.playerEncountersWithMark > 0)
            {
                Plugin.RandoManager.GiveLocation("Meet_LttM");
            }
        }

        /// <summary>
        /// Fix LttM wake up cutscene for Hunter to not break without the mark
        /// </summary>
        private static void ILMoonWakeUpUpdate(ILContext il)
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
        private static void MoonWakeUpUpdate(On.SLOracleWakeUpProcedure.orig_Update orig, SLOracleWakeUpProcedure self, bool eu)
        {
            if (self.phase == SLOracleWakeUpProcedure.Phase.Done)
            {
                Plugin.RandoManager?.GiveLocation("Save_LttM");
                Plugin.ArchipelagoManager?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.HelpingHand);
            }

            orig(self, eu);
        }

        /// <summary>
        /// Fix Riv ending cutscene to behave properly without the mark
        /// </summary>
        private static void OracleCtorIL(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.theMark)))
                );

            // Tell LttM we have the mark if this should be riv ending scene
            c.EmitDelegate<Func<bool, bool>>(hasMark => 
                hasMark || 
                (Plugin.RandomizerActive
                 && ModManager.MSC 
                 && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Rivulet 
                 && Plugin.Singleton.Game.IsMoonActive()));
        }

        /// <summary>
        /// Detect Rivulet LttM ending trigger
        /// </summary>
        private static void OnMoonSpecialEvent(On.SLOracleBehaviorHasMark.orig_SpecialEvent orig, SLOracleBehaviorHasMark self, string eventName)
        {
            orig(self, eventName);

            // Check for completion via visiting LttM after placing the Rarefaction cell
            if (eventName == "RivEndingFade")
            {
                Plugin.ArchipelagoManager?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SaveMoon);
            }
        }

        /// <summary>
        /// Allow Pebbles to do a violence on Arty if they don't have the drone, and detect Pebbles killing Inv
        /// </summary>
        private static void IteratorThrowOutBehaviorIL(ILContext il)
        {
            ILCursor c = new(il);

            // Add an extra condition for Artificer actually having the Citizen ID drone to not be killed by Pebbles
            ILLabel jump = null;
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.StoryCharacter)).GetGetMethod()),
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer))),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchBrfalse(out jump)
                );

            c.EmitDelegate(() => !Plugin.RandomizerActive || Plugin.RandoManager.GivenRobo);
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
                    Plugin.RandoManager?.GiveLocation("Meet_FP");
                }
            });
        }

        /// <summary>
        /// Modify Iterators to use speech sounds if mark is not acquired
        /// </summary>
        private static void DialogueAddMessage(On.HUD.DialogBox.orig_NewMessage_string_float_float_int orig, HUD.DialogBox self, string text, float xOrientation, float yPos, int extraLinger)
        {
            // Act as normal if conditions met
            bool shouldIteratorSpeak = !Plugin.RandomizerActive || Plugin.RandoManager.GivenMark
                || (ModManager.MSC && Plugin.RandoManager.currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint);
            bool noRoomExists = (self.hud.owner as Player)?.room is null;
            if (shouldIteratorSpeak || noRoomExists)
            {
                orig(self, text, xOrientation, yPos, extraLinger);
                return;
            }

            bool foundIteratorTarget = false;
            Room room = (self.hud.owner as Player).room;
            for (int i = 0; i < room.physicalObjects.Length; i++)
            {
                for (int j = 0; j < room.physicalObjects[i].Count; j++)
                {
                    // If this object is an SSOracle and they are talking
                    if (room.physicalObjects[i][j] is Oracle oracle
                        && oracle.oracleBehavior is SSOracleBehavior oracleBehavior
                        && oracleBehavior.currSubBehavior is SSOracleBehavior.TalkBehavior oracleTalkBehavior)
                    {
                        foundIteratorTarget = true;
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
                        break;
                    }
                }
            }

            // Play the dialogue if this wasn't an iterator talking
            if (!foundIteratorTarget) orig(self, text, xOrientation, yPos, extraLinger);
        }
    }
}
