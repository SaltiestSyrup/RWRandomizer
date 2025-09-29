using RegionKit.Modules.EchoExtender;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace RainWorldRandomizer
{
    public static class RegionKitCompatibility
    {
        private static bool? _enabled;

        private static SlugcatStats.Name storedForSlugcat = null;
        /// <summary>
        /// Stores whether a region & slugcat pair has an echo for quick access
        /// </summary>
        private static Dictionary<string, bool> echoRegions = [];

        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("rwmodding.coreorg.rk");
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool RegionHasEcho(string regionInitials, SlugcatStats.Name slugcat)
        {
            // Lock statement to avoid threads breaking dictionary
            lock (echoRegions)
            {
                if (storedForSlugcat != slugcat)
                {
                    storedForSlugcat = slugcat;
                    echoRegions.Clear();
                }
                // Return cached value if we've already checked this
                if (echoRegions.TryGetValue(regionInitials, out bool hasEcho)) return hasEcho;

                string settingsPath = AssetManager.ResolveFilePath("world/" + regionInitials + "/echoSettings.txt");

                if (File.Exists(settingsPath)
                    && EchoSettings.FromFile(settingsPath, slugcat).SpawnOnDifficulty)
                {
                    echoRegions.Add(regionInitials, true);
                    return true;
                }

                echoRegions.Add(regionInitials, false);
                return false;
            }
        }
    }
}
