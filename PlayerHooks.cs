using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public static class PlayerHooks
    {
        public static void ApplyHooks()
        {
            On.Player.Regurgitate += OnRegurgitate;
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
            On.Player.Regurgitate -= OnRegurgitate;
            On.RedsIllness.RedsCycles -= OnRedsCycles;
            On.Player.ClassMechanicsSaint -= OnClassMechanicsSaint;
            IL.Player.GrabUpdate -= ILPlayerGrabUpdate;
            IL.PlayerGraphics.Update -= ILPlayerGraphicsUpdate;
        }

        public static void OnRegurgitate(On.Player.orig_Regurgitate orig, Player self)
        {
            if (!Plugin.isRandomizerActive
                || Plugin.Singleton.ItemShelterDelivery
                || Plugin.Singleton.itemDeliveryQueue.Count == 0)
            {
                orig(self);
                return;
            }

            AbstractPhysicalObject tempObject = null;

            // If there's already something swallowed, store it as a temp variable
            if (self.objectInStomach != null)
            {
                tempObject = self.objectInStomach;
            }
            // Swap in queued object

            //Logger.LogDebug($"Stored item: {itemDeliveryQueue.Peek().id}, {itemDeliveryQueue.Peek().type.value}");
            self.objectInStomach = Plugin.ItemToAbstractObject(Plugin.Singleton.itemDeliveryQueue.Dequeue(), self.room);
            //Logger.LogDebug($"Converted object: {self.objectInStomach?.type}");

            orig(self);

            // If we stored an already swallowed object, put it back now
            if (tempObject != null)
            {
                self.objectInStomach = tempObject;
            }
        }

        public static void ILPlayerGrabUpdate(ILContext il)
        {
            try
            {
                // Substitution function
                AbstractPhysicalObject objectReplace(AbstractPhysicalObject objectInstomach, Player player)
                {
                    if (objectInstomach != null)
                    {
                        return objectInstomach;
                    }
                    if (!Plugin.Singleton.ItemShelterDelivery && Plugin.Singleton.itemDeliveryQueue.Count > 0)
                    {
                        return Plugin.ItemToAbstractObject(Plugin.Singleton.itemDeliveryQueue.Peek(), player.room);
                    }

                    return null;
                }

                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdloc(11),
                    x => x.MatchLdcI4(-1),
                    x => x.MatchBgt(out ILLabel label),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<Player>(nameof(Player.objectInStomach))
                    );

                c.Emit(OpCodes.Ldarg_0);
                // If we have a waiting item to be delivered, act as if there is an item in stomach
                c.EmitDelegate((Func<AbstractPhysicalObject, Player, AbstractPhysicalObject>)objectReplace);

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchAdd(),
                    x => x.MatchStfld<Player>(nameof(Player.swallowAndRegurgitateCounter)),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<Player>(nameof(Player.objectInStomach))
                    );

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Func<AbstractPhysicalObject, Player, AbstractPhysicalObject>)objectReplace);

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<Player>(typeof(Player).GetProperty(nameof(Player.isGourmand)).GetGetMethod().Name),
                    x => x.MatchBrfalse(out _),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<Player>(nameof(Player.objectInStomach))
                    );

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Func<AbstractPhysicalObject, Player, AbstractPhysicalObject>)objectReplace);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for PlayerGrabUpdate");
                Plugin.Log.LogError(e);
            }
        }

        public static void ILPlayerGraphicsUpdate(ILContext il)
        {
            try
            {
                // Substitution function
                AbstractPhysicalObject objectReplace(AbstractPhysicalObject objectInstomach, PlayerGraphics playerGraphics)
                {
                    if (objectInstomach != null)
                    {
                        return objectInstomach;
                    }
                    if (!Plugin.Singleton.ItemShelterDelivery && Plugin.Singleton.itemDeliveryQueue.Count > 0)
                    {
                        return Plugin.ItemToAbstractObject(Plugin.Singleton.itemDeliveryQueue.Peek(), playerGraphics.player.room);
                    }

                    return null;
                }

                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<PlayerGraphics>(nameof(PlayerGraphics.player)),
                    x => x.MatchLdfld<Player>(nameof(Player.objectInStomach))
                    );

                c.Emit(OpCodes.Ldarg_0);
                // If we have a waiting item to be delivered, act as if there is an item in stomach
                c.EmitDelegate((Func<AbstractPhysicalObject, PlayerGraphics, AbstractPhysicalObject>)objectReplace);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for PlayerGraphicsUpdate");
                Plugin.Log.LogError(e);
            }
        }

        public static int OnRedsCycles(On.RedsIllness.orig_RedsCycles orig, bool extracycles)
        {
            int origResult = orig(extracycles);

            int bonusCycles = ModManager.MMF && MoreSlugcats.MMF.cfgHunterBonusCycles != null
                ? MoreSlugcats.MMF.cfgHunterBonusCycles.Value : 5;
            int baseCycles = extracycles ? origResult - bonusCycles : origResult;

            // If the save hasn't been initialized, read the file to count cycles
            if (!Plugin.isRandomizerActive)
            {
                int countedCycles = SaveManager.CountRedsCycles(Plugin.Singleton.rainWorld.options.saveSlot);
                if (countedCycles == -1)
                {
                    return origResult;
                }

                return baseCycles + (countedCycles * bonusCycles);
            }

            return baseCycles + (Plugin.Singleton.hunterBonusCyclesGiven * bonusCycles);
        }

        public static void OnClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            orig(self);

            if (self.room.game.GetStorySession.saveState.deathPersistentSaveData.ripPebbles
                && !Plugin.Singleton.IsCheckGiven("Ascend_FP"))
            {
                Plugin.Singleton.GiveCheck("Ascend_FP");
            }

            if (self.room.game.GetStorySession.saveState.deathPersistentSaveData.ripMoon
                && !Plugin.Singleton.IsCheckGiven("Ascend_LttM"))
            {
                Plugin.Singleton.GiveCheck("Ascend_LttM");
            }
        }
    }
}
