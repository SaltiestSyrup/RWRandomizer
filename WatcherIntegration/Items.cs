using System.Collections.Generic;
using UnityEngine;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class Items
    {
        internal static int RippleIncrements;
        internal static IEnumerable<string> CollectedDynamicKeys { get; set; }  // this could get from where the data actually gets stored later
        internal static IEnumerable<string> CollectedStaticKeys { get; set; }

        internal static void IncreaseRipple()
        {
            RippleIncrements = Mathf.Clamp(RippleIncrements + 1, 0, 12);
            if (Plugin.Singleton.Game?.GetStorySession?.saveState.deathPersistentSaveData is DeathPersistentSaveData dpsd)
            {
                dpsd.maximumRippleLevel = Mathf.Min(5f, 1f + RippleIncrements / 2f);
                dpsd.minimumRippleLevel = Mathf.Max(1f, -1f + RippleIncrements / 2f);
            }
        }
    }
}
