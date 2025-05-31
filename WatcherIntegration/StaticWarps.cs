using System.Collections.Generic;
using System.Linq;
using WarpPoint = Watcher.WarpPoint;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class StaticWarps
    {

        /// <summary>Compute this warp point's key name.  Returns null if this warp point cannot have a key.</summary>
        internal static string KeyName(this WarpPoint self)
        {
            string region = self.room.world.region.name;
            if (Region.IsSentientRotRegion(region) || region == "WRSA" || region == "WAUA") return null;
            string[] endpoints = new string[] { region.ToUpperInvariant(), self.Data.destRegion.ToUpperInvariant() };
            return $"StaticWarp-{string.Join("-", endpoints.OrderBy(x => x))}";
        }

        internal static class Hooks
        {
            internal static void Apply()
            {
                On.Watcher.WarpPoint.UpdateWarpTear += WarpPoint_UpdateWarpTear;
            }

            internal static void Unapply()
            {
                On.Watcher.WarpPoint.UpdateWarpTear -= WarpPoint_UpdateWarpTear;
            }

            /// <summary>Prevents static warp points from opening up if the key is not collected.</summary>
            private static void WarpPoint_UpdateWarpTear(On.Watcher.WarpPoint.orig_UpdateWarpTear orig, WarpPoint self)
            {
                orig(self);
                if (self.KeyName() is string keyName
                    && Items.CollectedStaticKeys?.Contains(keyName) == false 
                    && self.Data.nonDynamicWarpPoint
                    && self.warpTear is Watcher.WarpTear tear
                    && tear.openAnimation < 0.1f) tear.openAnimation = 0f;
            }
        }
    }
}
