using RainWorldRandomizer.Generation;
using System.Collections.Generic;

namespace RainWorldRandomizer
{
    public static class CustomRegionCompatability
    {
        private static bool hasInit = false;
        public static Dictionary<string, AccessRule> GlobalRuleOverrides = [];
        public static Dictionary<SlugcatStats.Name, Dictionary<string, AccessRule>> SlugcatRuleOverrides = [];

        public static void Init()
        {
            if (hasInit) return;

            Constants.OneWayGates.Add("GATE_PA_FR", false);

            // Old Hanging Gardens tries really hard to pretend it exists when it doesn't
            GlobalRuleOverrides.Add("Region-HG", new AccessRule(AccessRule.IMPOSSIBLE_ID));

            hasInit = true;
        }
    }
}
