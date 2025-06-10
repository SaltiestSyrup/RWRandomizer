using WarpPoint = Watcher.WarpPoint;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class StaticWarps
    {
        /// <summary>Determine if this <see cref="WarpPoint"/> needs a key which is not yet collected.</summary>
        internal static bool MissingKey(this WarpPoint self) => 
            self.Data.nonDynamicWarpPoint && Items.StaticKey.IsMissing(self.room.world.region.name, self.Data.destRegion);

        internal static class Hooks
        {
            internal static void ApplyHooks()
            {
                On.Watcher.WarpPoint.UpdateWarpTear += WarpPoint_UpdateWarpTear;
            }

            internal static void RemoveHooks()
            {
                On.Watcher.WarpPoint.UpdateWarpTear -= WarpPoint_UpdateWarpTear;
            }

            /// <summary>Prevents static warp points from opening up if the key is not collected.</summary>
            private static void WarpPoint_UpdateWarpTear(On.Watcher.WarpPoint.orig_UpdateWarpTear orig, WarpPoint self)
            {
                orig(self);
                if (self.MissingKey() && self.warpTear is { openAnimation: < 0.1f } tear) tear.openAnimation = 0f;
            }
        }
    }
}
