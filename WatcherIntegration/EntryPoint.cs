﻿using System;
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

        internal static void TryGiveLocation(string loc)
        {
            switch (Plugin.RandoManager.IsLocationGiven(loc))
            {
                case null: Plugin.Log.LogDebug($"Giving {loc}?   nope, location does not exist"); break;
                case true: Plugin.Log.LogDebug($"Giving {loc}?   nope, location is already given"); break;
                case false: Plugin.Log.LogDebug($"Giving {loc}?   {(Plugin.RandoManager.GiveLocation(loc) ? "yes" : "nope, giving failed")}"); break;
            }
        }
    }
}
