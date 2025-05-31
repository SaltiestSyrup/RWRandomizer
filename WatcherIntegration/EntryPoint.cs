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
        }

        internal static void Unapply()
        {
            CheckDetection.Hooks.Unapply();
            DynamicWarpTargetting.Hooks.Unapply();
            StaticWarps.Hooks.Unapply();
        }
        internal static string Region(this string self) => self?.Split('_')[0].ToLowerInvariant();
    }
}
