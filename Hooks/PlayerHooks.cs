using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;

namespace RainWorldRandomizer
{
    public static class PlayerHooks
    {
        public static void ApplyHooks()
        {
            On.SlugcatStats.ctor += OnSlugcatStatsCtor;
            On.Player.Jump += OnJump;
            On.Player.ThrownSpear += OnThrownSpear;
            On.Player.Regurgitate += OnRegurgitate;
            On.Player.Update += OnPlayerUpdate;
            On.Player.NewRoom += OnNewRoom;
            On.RedsIllness.RedsCycles += OnRedsCycles;
            On.Player.ClassMechanicsSaint += OnClassMechanicsSaint;

            try
            {
                IL.Player.GrabUpdate += ILPlayerGrabUpdate;
                IL.PlayerGraphics.Update += ILPlayerGraphicsUpdate;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.SlugcatStats.ctor -= OnSlugcatStatsCtor;
            On.Player.Jump -= OnJump;
            On.Player.ThrownSpear -= OnThrownSpear;
            On.Player.Regurgitate -= OnRegurgitate;
            On.Player.Update -= OnPlayerUpdate;
            On.Player.NewRoom -= OnNewRoom;
            On.RedsIllness.RedsCycles -= OnRedsCycles;
            On.Player.ClassMechanicsSaint -= OnClassMechanicsSaint;
            IL.Player.GrabUpdate -= ILPlayerGrabUpdate;
            IL.PlayerGraphics.Update -= ILPlayerGraphicsUpdate;
        }

        /// <summary>
        /// Apply movement speed multiplier on stat creation
        /// </summary>
        private static void OnSlugcatStatsCtor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
        {
            orig(self, slugcat, malnourished);
            if (Plugin.RandoManager is null) return;
            self.runspeedFac *= Plugin.RandoManager.MovementSpeedMultiplier;
            self.poleClimbSpeedFac *= Plugin.RandoManager.MovementSpeedMultiplier;
            self.corridorClimbSpeedFac *= Plugin.RandoManager.MovementSpeedMultiplier;
        }

        /// <summary>
        /// Update movement speed multiplier while in-game
        /// </summary>
        public static void UpdatePlayerMoveSpeed()
        {
            RainWorldGame game = Plugin.Singleton.Game;
            if (game is null) return;

            // Recreate SlugcatStats to update move speed multiplier
            bool malnourished = game.session.characterStats.malnourished;
            game.session.characterStats = new SlugcatStats(game.StoryCharacter, malnourished);
            if (ModManager.CoopAvailable) game.GetStorySession?.CreateJollySlugStats(malnourished);

            foreach (AbstractCreature crit in game.Players)
            {
                if (crit.realizedCreature is not Player player) continue;
                player.initRunSpeedFac = game.session.characterStats.runspeedFac;
            }
        }

        private static void OnJump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);
            self.jumpBoost += 1.2f * Plugin.RandoManager.MovementSpeedMultiplier;
        }

        /// <summary>
        /// Modify spear damage based on current multiplier 
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="spear"></param>
        private static void OnThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            if (Plugin.RandoManager is not null) spear.spearDamageBonus *= Plugin.RandoManager.SpearDamageMultiplier;
        }

        /// <summary>
        /// Swap regurgitate item for queued item if applicable
        /// </summary>
        public static void OnRegurgitate(On.Player.orig_Regurgitate orig, Player self)
        {
            if (!Plugin.RandoManager.isRandomizerActive
                || RandoOptions.ItemShelterDelivery
                || Plugin.RandoManager.itemDeliveryQueue.Count == 0)
            {
                orig(self);
                return;
            }

            self.objectInStomach ??= Plugin.ItemToAbstractObject(Plugin.RandoManager.itemDeliveryQueue.Dequeue(), self.room);
            orig(self);
        }

        /// <summary>
        /// Detect Void Sea ending trigger
        /// </summary>
        public static void OnPlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            // Check for completion via Void Sea
            if (self.room is Room room)
            {
                if (room.abstractRoom.name == "SB_L01"
                    && self.firstChunk.pos.y < -500f)
                {
                    (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.Ascension);
                }
                else if (room.abstractRoom.name == "HR_FINAL"
                    && self.firstChunk.pos.y > room.PixelHeight + 500f)
                {
                    (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.Rubicon);
                }
            }
        }

        /// <summary>
        /// Convince game we have an item in stomach if there is a queued item
        /// </summary>
        public static void ILPlayerGrabUpdate(ILContext il)
        {
            // Substitution function
            static AbstractPhysicalObject objectReplace(AbstractPhysicalObject objectInstomach, Player player)
            {
                if (objectInstomach != null)
                {
                    return objectInstomach;
                }
                if (!RandoOptions.ItemShelterDelivery && Plugin.RandoManager.itemDeliveryQueue.Count > 0)
                {
                    return Plugin.ItemToAbstractObject(Plugin.RandoManager.itemDeliveryQueue.Peek(), player.room);
                }

                return null;
            }

            ILCursor c = new(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(11),
                x => x.MatchLdcI4(-1),
                x => x.MatchBgt(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.objectInStomach))
                );

            c.Emit(OpCodes.Ldarg_0);
            // If we have a waiting item to be delivered, act as if there is an item in stomach
            c.EmitDelegate(objectReplace);

            c.GotoNext(
                MoveType.After,
                x => x.MatchAdd(),
                x => x.MatchStfld<Player>(nameof(Player.swallowAndRegurgitateCounter)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.objectInStomach))
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(objectReplace);

            // Make item delivery spit up faster
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.swallowAndRegurgitateCounter))),
                x => x.MatchLdcI4(110)
                );

            c.EmitDelegate<Func<int, int>>((origTime) =>
            {
                if (!RandoOptions.ItemShelterDelivery && Plugin.RandoManager.itemDeliveryQueue.Count > 0)
                {
                    // This time needs to be longer than the 90 ticks swallowing an item takes
                    return 95;
                }
                return origTime;
            });

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<Player>(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod().Name),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.objectInStomach))
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(objectReplace);
        }

        /// <summary>
        /// Convince graphics we have an item in stomach if there is a queued item
        /// </summary>
        public static void ILPlayerGraphicsUpdate(ILContext il)
        {
            // Substitution function
            static AbstractPhysicalObject objectReplace(AbstractPhysicalObject objectInstomach, PlayerGraphics playerGraphics)
            {
                if (objectInstomach != null)
                {
                    return objectInstomach;
                }
                if (!RandoOptions.ItemShelterDelivery && Plugin.RandoManager.itemDeliveryQueue.Count > 0)
                {
                    return Plugin.ItemToAbstractObject(Plugin.RandoManager.itemDeliveryQueue.Peek(), playerGraphics.player.room);
                }

                return null;
            }

            ILCursor c = new(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PlayerGraphics>(nameof(PlayerGraphics.player)),
                x => x.MatchLdfld<Player>(nameof(Player.objectInStomach))
                );

            c.Emit(OpCodes.Ldarg_0);
            // If we have a waiting item to be delivered, act as if there is an item in stomach
            c.EmitDelegate(objectReplace);
        }

        public static void OnNewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            ArchipelagoConnection.TrySendCurrentRoomPacket(newRoom.abstractRoom.name);
        }

        /// <summary>
        /// Modify Hunter's remaining cycle count
        /// </summary>
        public static int OnRedsCycles(On.RedsIllness.orig_RedsCycles orig, bool extracycles)
        {
            int origResult = orig(extracycles);

            if (Plugin.RandoManager is null) return int.MaxValue;

            // Remove cycle limit completely for Archipelago
            if (Plugin.RandoManager is ManagerArchipelago)
            {
                if (Plugin.Singleton.Game != null)
                {
                    return Plugin.Singleton.Game.GetStorySession.saveState.cycleNumber + 1;
                }
                // If this is isn't in game there's not an easy way to get the cycle count
                // Will need to hook individual cases to fix this
                return int.MaxValue;
            }

            int bonusCycles = ModManager.MMF && MoreSlugcats.MMF.cfgHunterBonusCycles != null
                ? MoreSlugcats.MMF.cfgHunterBonusCycles.Value : 5;
            int baseCycles = extracycles ? origResult - bonusCycles : origResult;

            // If the save hasn't been initialized, read the file to count cycles
            //if (!Plugin.RandoManager.isRandomizerActive)
            //{
            //    int countedCycles = SaveManager.CountRedsCycles(Plugin.Singleton.rainWorld.options.saveSlot);
            //    if (countedCycles == -1)
            //    {
            //        return origResult;
            //    }

            //    return baseCycles + (countedCycles * bonusCycles);
            //}

            return baseCycles + (Plugin.RandoManager.HunterBonusCyclesGiven * bonusCycles);
        }

        /// <summary>
        /// Detect Saint ascending iterators
        /// </summary>
        public static void OnClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            orig(self);

            if (self.room.game.GetStorySession.saveState.deathPersistentSaveData.ripPebbles)
            {
                Plugin.RandoManager.GiveLocation("Ascend_FP");
            }

            if (self.room.game.GetStorySession.saveState.deathPersistentSaveData.ripMoon)
            {
                Plugin.RandoManager.GiveLocation("Ascend_LttM");

                // These would become impossible for Saint after LttM is gone, so send them now
                Plugin.RandoManager.GiveLocation("Eat_Neuron");
                Plugin.RandoManager.GiveLocation("FoodQuest-SSOracleSwarmer");
            }
        }
    }
}
