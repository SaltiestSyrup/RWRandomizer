using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;
using Perks = RainWorldRandomizer.ManagerBase.ExpeditionPerks;

namespace RainWorldRandomizer
{
    public static class PlayerHooks
    {
        public static void ApplyHooks()
        {
            On.Player.ThrownSpear += OnThrownSpear;
            On.Player.Regurgitate += OnRegurgitate;
            On.Player.Update += OnPlayerUpdate;
            On.Player.NewRoom += OnNewRoom;
            On.RedsIllness.RedsCycles += OnRedsCycles;
            On.Player.ClassMechanicsSaint += OnClassMechanicsSaint;
            On.SlugcatStats.ctor += OnSlugcatStatsCtor;

            try
            {
                IL.Player.GrabUpdate += ILPlayerGrabUpdate;
                IL.PlayerGraphics.Update += ILPlayerGraphicsUpdate;
                IL.Player.ctor += PlayerCtorIL;
                IL.Player.Grabability += PlayerGrababilityIL;
                IL.Explosion.Update += ExplosionUpdateIL;
                IL.Player.Tongue.Shoot += PlayerTongueShootIL;
                IL.Player.ClassMechanicsArtificer += ClassMechanicsArtificerIL;
                IL.Player.GraspsCanBeCrafted += OverrideItemCrafting;
                IL.Player.SpitUpCraftedObject += OverrideItemCrafting;
                IL.Watcher.WarpPoint.SuckInCreatures += WarpPoint_SuckInCreatures;

                _ = new Hook(typeof(Player).GetProperty(nameof(Player.isRivulet)).GetGetMethod(), OverrideIsRivulet);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.Player.ThrownSpear -= OnThrownSpear;
            On.Player.Regurgitate -= OnRegurgitate;
            On.Player.Update -= OnPlayerUpdate;
            On.Player.NewRoom -= OnNewRoom;
            On.RedsIllness.RedsCycles -= OnRedsCycles;
            On.Player.ClassMechanicsSaint -= OnClassMechanicsSaint;
            On.SlugcatStats.ctor -= OnSlugcatStatsCtor;
            IL.Player.GrabUpdate -= ILPlayerGrabUpdate;
            IL.PlayerGraphics.Update -= ILPlayerGraphicsUpdate;
            IL.Player.ctor -= PlayerCtorIL;
            IL.Player.Grabability -= PlayerGrababilityIL;
            IL.Explosion.Update -= ExplosionUpdateIL;
            IL.Player.Tongue.Shoot -= PlayerTongueShootIL;
            IL.Player.ClassMechanicsArtificer -= ClassMechanicsArtificerIL;
            IL.Player.GraspsCanBeCrafted -= OverrideItemCrafting;
            IL.Player.SpitUpCraftedObject -= OverrideItemCrafting;
            IL.Watcher.WarpPoint.SuckInCreatures -= WarpPoint_SuckInCreatures;
        }

        /// <summary>
        /// Modify spear damage based on current multiplier 
        /// </summary>
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
                || !RandoOptions.ItemStomachDelivery
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
                    if (Plugin.RandoManager?.currentSlugcat == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
                        (Plugin.RandoManager as ManagerArchipelago)?.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.TrueEnding);
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
                if (RandoOptions.ItemStomachDelivery && Plugin.RandoManager.itemDeliveryQueue.Count > 0)
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
                if (RandoOptions.ItemStomachDelivery && Plugin.RandoManager.itemDeliveryQueue.Count > 0)
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
                if (RandoOptions.ItemStomachDelivery && Plugin.RandoManager.itemDeliveryQueue.Count > 0)
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
                if (Plugin.Singleton.Game?.GetStorySession?.saveState is not null)
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

        // ----- EXPEDITION PERKS -----
        /// <summary>
        /// Make sure expedition perks received during play activate instantly
        /// </summary>
        public static void RefreshExpeditionPerks()
        {
            RainWorldGame game = Plugin.Singleton.Game;
            if (game is null) return;

            game.session.characterStats = new SlugcatStats(game.StoryCharacter, game.session.characterStats.malnourished);
            if (ModManager.CoopAvailable) game.GetStorySession.CreateJollySlugStats(game.session.characterStats.malnourished);
            if (Plugin.RandoManager.HasExpeditionPerk(Perks.BackSpear))
            {
                foreach (AbstractCreature crit in game.AlivePlayers)
                {
                    if (crit.realizedCreature is not Player player || player.spearOnBack is not null) continue;
                    player.spearOnBack = new Player.SpearOnBack(player);
                }
            }
        }

        /// <summary>
        /// Add back spear slot if player has <see cref="Perks.BackSpear"/>
        /// </summary>
        private static void PlayerCtorIL(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(x => x.MatchLdstr("unl-backspear")); // 0A72
            c.GotoPrev(MoveType.AfterLabel,
                x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.Expedition)))
                ); // 0A5A

            ILLabel jump = c.DefineLabel();

            c.EmitDelegate(HasBackSpearPerk);
            c.Emit(OpCodes.Brtrue, jump);

            c.GotoNext(x => x.MatchLdarg(0)); // 0A7E
            c.MarkLabel(jump);

            static bool HasBackSpearPerk() => Plugin.RandoManager?.HasExpeditionPerk(Perks.BackSpear) is true;
        }

        /// <summary>
        /// Make dual wielding possible if player has <see cref="Perks.DualWielding"/>
        /// </summary>
        private static void PlayerGrababilityIL(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(x => x.MatchLdstr("unl-dualwield")); // 0020
            c.GotoPrev(MoveType.AfterLabel,
                x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.Expedition)))
                ); // 0008

            ILLabel jump = c.DefineLabel();

            c.EmitDelegate(HasDualWieldingPerk);
            c.Emit(OpCodes.Brtrue, jump);

            c.GotoNext(x => x.MatchLdcI4(1)); // 002C
            c.MarkLabel(jump);

            static bool HasDualWieldingPerk() => Plugin.RandoManager?.HasExpeditionPerk(Perks.DualWielding) is true;
        }

        /// <summary>
        /// Make explosions do no damage if player has <see cref="Perks.ExplosionResistance"/>
        /// </summary>
        private static void ExplosionUpdateIL(ILContext il)
        {
            ILCursor c = new(il);

            // All checks in this function can conveniently be modified the same way
            for (int i = 0; i < 4; i++)
            {
                c.GotoNext(MoveType.After,
                    x => x.MatchLdstr("unl-explosionimmunity"),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchBrfalse(out _)); // 0673 & 06F9 & 0816 & 091E

                ILLabel jump = c.MarkLabel();

                c.GotoPrev(MoveType.AfterLabel,
                    x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.Expedition)))); // 0646 & 06CC & 07E9 & 08F1

                c.EmitDelegate(HasExplosionImmunityPerk);
                c.Emit(OpCodes.Brtrue, jump);
            }

            static bool HasExplosionImmunityPerk() => Plugin.RandoManager?.HasExpeditionPerk(Perks.ExplosionResistance) is true;
        }

        /// <summary>
        /// Cancel tongue input if player has <see cref="Perks.ExplosiveJump"/> or <see cref="Perks.ExplosiveParry"/> and is pressing grab
        /// </summary>
        private static void PlayerTongueShootIL(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                    x => x.MatchLdstr("unl-explosivejump"),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchBrfalse(out _)); // 0047

            ILLabel jump = c.MarkLabel();

            c.GotoPrev(MoveType.AfterLabel,
                x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.Expedition)))); // 0015

            c.EmitDelegate(HasExplosivePerk);
            c.Emit(OpCodes.Brtrue, jump);

            static bool HasExplosivePerk()
            {
                if (Plugin.RandoManager is null) return false;
                return Plugin.RandoManager.HasExpeditionPerk(Perks.ExplosiveParry) || Plugin.RandoManager.HasExpeditionPerk(Perks.ExplosiveJump);
            }
        }

        /// <summary>
        /// Allow explosive jump or parry to trigger if player has <see cref="Perks.ExplosiveJump"/> or <see cref="Perks.ExplosiveParry"/>, respectively
        /// </summary>
        private static void ClassMechanicsArtificerIL(ILContext il)
        {
            ILCursor c = new(il);

            // Let Arty code run if either perk is aquired
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(Expedition.ExpeditionGame).GetProperty(nameof(Expedition.ExpeditionGame.explosivejump)).GetGetMethod())); // 0029
            c.EmitDelegate(HasAnyExplosivePerk);


            // Additionally require the perk in order to trigger jump
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.pyroJumpped)))); // 01B8
            c.EmitDelegate(DoesNotHaveExplosiveJump);
            // -> brtrue


            // Additionally require the perk in order to trigger parry
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld(typeof(Player).GetField(nameof(Player.submerged)))); // 0808
            c.EmitDelegate(DoesNotHaveExplosiveParry);
            // -> brtrue

            static bool HasAnyExplosivePerk(bool origValue)
            {
                if (origValue) return true;
                if (Plugin.RandoManager is null) return false;
                return Plugin.RandoManager.HasExpeditionPerk(Perks.ExplosiveParry)
                    || Plugin.RandoManager.HasExpeditionPerk(Perks.ExplosiveJump);
            }
            static bool DoesNotHaveExplosiveJump(bool origValue) => origValue || !ShouldHaveJump();
            static bool DoesNotHaveExplosiveParry(bool origValue) => origValue || !ShouldHaveParry();

            static bool ShouldHaveJump() => Plugin.Singleton.Game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer
                || Plugin.RandoManager?.HasExpeditionPerk(Perks.ExplosiveJump) is true;
            static bool ShouldHaveParry() => Plugin.Singleton.Game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer
                || Plugin.RandoManager?.HasExpeditionPerk(Perks.ExplosiveParry) is true;
        }

        /// <summary>
        /// Allows slugcat to craft items if player has <see cref="Perks.ItemCrafting"/>
        /// </summary>
        private static void OverrideItemCrafting(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                    x => x.MatchLdstr("unl-crafting"),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchBrfalse(out _)); // 0076

            ILLabel jump = c.MarkLabel();

            c.GotoPrev(MoveType.AfterLabel,
                x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.Expedition)))); // 0044

            c.EmitDelegate(HasCraftingPerk);
            c.Emit(OpCodes.Brtrue, jump);

            static bool HasCraftingPerk() => Plugin.RandoManager?.HasExpeditionPerk(Perks.ItemCrafting) is true;
        }

        /// <summary>
        /// Set lung factor if player has <see cref="Perks.Aquatic"/>, and set movement stats if player has <see cref="Perks.Agility"/>
        /// </summary>
        private static void OnSlugcatStatsCtor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
        {
            orig(self, slugcat, malnourished);
            if (Plugin.RandoManager is null) return;

            if (Plugin.RandoManager.HasExpeditionPerk(Perks.Aquatic))
            {
                self.lungsFac = 0.15f;
            }
            if (Plugin.RandoManager.HasExpeditionPerk(Perks.Agility))
            {
                if (malnourished)
                {
                    self.runspeedFac = 1.27f;
                    self.poleClimbSpeedFac = 1.1f;
                    self.corridorClimbSpeedFac = 1.2f;
                }
                else
                {
                    self.runspeedFac = 1.75f;
                    self.poleClimbSpeedFac = 1.8f;
                    self.corridorClimbSpeedFac = 1.6f;
                }
            }
        }

        private static bool OverrideIsRivulet(Func<Player, bool> orig, Player self) => orig(self) || Plugin.RandoManager?.HasExpeditionPerk(Perks.Agility) is true;

        /// <summary>
        /// When getting sucked into a warp, drop the spear on a player's back if possible to avoid double-spawning on the other side
        /// </summary>
        private static void WarpPoint_SuckInCreatures(ILContext il)
        {
            ILCursor c = new(il);

            // LoseAllGrasps call at 021C
            c.GotoNext(x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.LoseAllGrasps))));

            c.Emit(OpCodes.Dup);
            c.EmitDelegate(DropBackSpear);

            static void DropBackSpear(Creature crit) => (crit as Player)?.spearOnBack?.DropSpear();
        }
    }
}
