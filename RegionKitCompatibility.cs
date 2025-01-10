using RegionKit.Modules.EchoExtender;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public static class RegionKitCompatibility
    {
        private static bool? _enabled;

        public static bool Enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("rwmodding.coreorg.rk");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool RegionHasEcho(string regionInitials, SlugcatStats.Name slugcat)
        {
            string settingsPath = AssetManager.ResolveFilePath("world/" + regionInitials + "/echoSettings.txt");

            if (File.Exists(settingsPath)
                && EchoSettings.FromFile(settingsPath, slugcat).SpawnOnDifficulty)
            {
                return true;
            }

            return false;
        }
    }
}
