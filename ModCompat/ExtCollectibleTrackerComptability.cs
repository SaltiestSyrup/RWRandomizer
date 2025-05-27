using MonoMod.RuntimeDetour;
using System;

namespace RainWorldRandomizer
{
    public static class ExtCollectibleTrackerComptability
    {
        private static bool? _enabled;

        public static bool Enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("franklygd.extendedcollectiblestracker");
                }
                return (bool)_enabled;
            }
        }

        public static void ApplyHooks()
        {
            _ = new Hook(typeof(ExtendedCollectiblesTracker.Plugin).Assembly.GetType("ExtendedCollectiblesTracker.Mod", true).GetMethod("IsPearlRead"),
                typeof(ExtCollectibleTrackerComptability).GetMethod(nameof(OnIsPearlRead)));
        }

        public static bool OnIsPearlRead(Func<RainWorld, DataPearl.AbstractDataPearl.DataPearlType, bool> orig, RainWorld rainWorld, DataPearl.AbstractDataPearl.DataPearlType pearlType)
        {
            bool origResult = orig(rainWorld, pearlType);
            string locName = $"Pearl-{pearlType}";

            if (Plugin.RandoManager is ManagerArchipelago)
            {
                // More costly lookup to find where this pearl comes from
                foreach (var region in Plugin.Singleton.rainWorld.regionDataPearls)
                {
                    if (region.Value.Contains(pearlType))
                    {
                        locName += $"-{region.Key.ToUpperInvariant()}";
                        break;
                    }
                }
            }

            if (Plugin.RandoManager.LocationExists(locName))
            {
                return Plugin.RandoManager.IsLocationGiven(locName) ?? false;
            }

            return origResult;
        }
    }
}
