using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class EntryPoint
    {
        internal static void Apply()
        {
            CheckDetection.Hooks.Apply();
            DynamicWarpTargetting.Hooks.Apply();
            StaticWarps.Hooks.Apply();
            Plugin.Log.LogDebug("Watcher integration hooks applied");

            // debug hooks
            On.Watcher.WarpPoint.NewWorldLoaded += WarpPoint_NewWorldLoaded;
        }

        private static void WarpPoint_NewWorldLoaded(On.Watcher.WarpPoint.orig_NewWorldLoaded orig, Watcher.WarpPoint self)
        {
            try { orig(self); } catch (Exception e) { Plugin.Log.LogError(e); }
        }

        internal static void Unapply()
        {
            CheckDetection.Hooks.Unapply();
            DynamicWarpTargetting.Hooks.Unapply();
            StaticWarps.Hooks.Unapply();
        }
        internal static string Region(this string self) => self?.Split('_')[0].ToUpperInvariant();
    }
}
