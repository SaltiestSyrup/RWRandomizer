using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using WPOT = Watcher.WatcherEnums.PlacedObjectType;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class CheckDetection
    {
        internal static class Hooks
        {
            internal static void Apply()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered += DetectSpinningTop;
                On.Watcher.SpinningTop.CanRaiseRippleLevel += Dont;
                IL.Room.Loaded += SpinningTopKeyCheck;
            }

            internal static void Unapply()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered -= DetectSpinningTop;
                On.Watcher.SpinningTop.CanRaiseRippleLevel -= Dont;
                IL.Room.Loaded -= SpinningTopKeyCheck;
            }

            /// <summary>Prevent Ripple from being raised automatically.</summary>
            private static bool Dont(On.Watcher.SpinningTop.orig_CanRaiseRippleLevel orig, Watcher.SpinningTop self) => false;

            internal static void DetectSpinningTop(On.Watcher.SpinningTop.orig_MarkSpinningTopEncountered orig, Watcher.SpinningTop self)
            {
                orig(self);
                string loc = $"SpinningTop-{self.room.abstractRoom.name.Region()}";
                if (Plugin.RandoManager.IsLocationGiven(loc) == false) Plugin.RandoManager.GiveLocation(loc);
            }

            internal static void DetectStaticWarpPoint(SaveState saveState)
            {
                foreach (var point in saveState.deathPersistentSaveData.newlyDiscoveredWarpPoints)
                {
                    string loc = $"Warp-{point.Key.Split(':')[0].ToUpperInvariant()}";
                    if (Plugin.RandoManager.IsLocationGiven(loc) == false) Plugin.RandoManager.GiveLocation(loc);
                }
            }
            /// <summary>Prevent Spinning Top from spawning if the key is not collected (unless that setting is disabled).</summary>
            private static void SpinningTopKeyCheck(ILContext il)
            {
                ILCursor c = new ILCursor(il);
                // Branch interception at 1d71 (roomSettings.placedObjects[num10].type == WatcherEnums.PlacedObjectType.SpinningTopSpot).
                c.GotoNext(x => x.MatchLdsfld(typeof(WPOT).GetField(nameof(WPOT.SpinningTopSpot))));  // 1d67
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ExtEnum<PlacedObject.Type>).GetMethod("op_Equality")));  // 1d6c

                c.Emit(OpCodes.Ldarg_0);  // Room this
                c.Emit(OpCodes.Ldloc, 27);  // int num10 (iteration variable)
                bool Delegate(bool orig, Room self, int index)
                {
                    if (!orig || !Settings.spinningTopKeys) return orig;
                    string dest = (self.roomSettings.placedObjects[index].data as SpinningTopData).destRegion;
                    return !Items.StaticKey.IsMissing(self.world.name, dest);
                }
                c.EmitDelegate<Func<bool, Room, int, bool>>(Delegate);
            }
        }
    }
}
