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

        public static void Init()
        {
            if (hasInit) return;

            Plugin.OneWayGates.Add("GATE_PA_FR", false);

            hasInit = true;
        }
    }
}
