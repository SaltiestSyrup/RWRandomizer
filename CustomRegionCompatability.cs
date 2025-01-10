using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public static class CustomRegionCompatability
    {
        private static bool hasInit = false;
        //public static readonly string[] supportedRegions =
        //{
        //    "SD", "PA", "FR", "MF", "CW"
        //};

        public static void Init()
        {
            if (hasInit) return;

            Plugin.Log.LogDebug("Init region compat");
            Plugin.OneWayGates.Add("GATE_PA_FR", false);

            //RandomizerMain.LogicBlacklist.AddRange(new List<string>
            //{
            //    "Pearl-DSH_Unlore_1", "Pearl-DSH_Unlore_2", "Pearl-DSH_Unlore_3", // Diverse Shelters
            //    "BigGoldenPearl" // Shrouded Assembly
            //});

            hasInit = true;
        }
    }
}
