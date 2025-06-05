using System.Collections.Generic;
using UnityEngine;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class Items
    {
        /// <summary>The number of Ripple items collected.</summary>
        internal static int RippleIncrements;
        internal static Vector2 Ripple => new Vector2(Mathf.Max(1f, -1f + RippleIncrements / 2f), Mathf.Min(5f, 1f + RippleIncrements / 2f));
        internal static List<string> collectedDynamicKeys = new List<string>();
        internal static List<string> CollectedDynamicKeys => collectedDynamicKeys;  // this could get from where the data actually gets stored later
        internal static List<string> collectedStaticKeys = new List<string>();
        internal static List<string> CollectedStaticKeys => collectedDynamicKeys;

        internal static void UpdateRipple()
        {
            if (Plugin.Singleton.Game?.GetStorySession?.saveState.deathPersistentSaveData is DeathPersistentSaveData dpsd)
            {
                dpsd.minimumRippleLevel = Ripple.x;
                dpsd.maximumRippleLevel = Ripple.y;
            }
        }

        internal static void ReceiveItem(string item)
        {
            string[] split = item.Split(new char[] { '-' }, 2);

            switch (split[0])  // switch on first part of item name
            {
                case "Dynamic": CollectedDynamicKeys.Add(split[1]); break;
                case "Warp": CollectedStaticKeys.Add(split[1]); break;
                case "Ripple": RippleIncrements++; UpdateRipple(); break;
            }
        }
    }
}
