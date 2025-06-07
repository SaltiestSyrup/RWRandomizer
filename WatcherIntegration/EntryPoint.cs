using System;
using System.Reflection;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class EntryPoint
    {
        /// <summary>A <see cref="BindingFlags"/> which matches everything.</summary>
        internal static BindingFlags bfAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        internal static void ApplyHooks()
        {
            CheckDetection.Hooks.ApplyHooks();
            DynamicWarpTargetting.Hooks.ApplyHooks();
            StaticWarps.Hooks.ApplyHooks();
            Completion.Hooks.ApplyHooks();
        }

        internal static void RemoveHooks()
        {
            CheckDetection.Hooks.RemoveHooks();
            DynamicWarpTargetting.Hooks.RemoveHooks();
            StaticWarps.Hooks.RemoveHooks();
            Completion.Hooks.RemoveHooks();
        }
        internal static string Region(this string self) => self?.Split('_')[0].ToUpperInvariant();
    }
}
