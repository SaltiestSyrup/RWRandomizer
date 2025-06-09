using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class Items
    {
        /// <summary>The number of Ripple items collected.</summary>
        internal static int RippleIncrements;
        /// <summary>What the minimum and maximum Ripple should be, based on <see cref="RippleIncrements"/>.</summary>
        internal static Vector2 Ripple => new(Mathf.Max(1f, -1f + RippleIncrements / 2f), Mathf.Min(5f, 1f + RippleIncrements / 2f));
        internal static List<string> collectedDynamicKeys = new();
        internal static List<string> CollectedDynamicKeys => collectedDynamicKeys;  // this could get from where the data actually gets stored later
        internal static List<string> collectedStaticKeys = new();
        internal static List<string> CollectedStaticKeys => collectedDynamicKeys;

        internal struct StaticKey
        {
            internal string name;
            internal bool canHaveKey;

            /// <summary>Every SpinningTopSpot and its target, from static data.</summary>
            // Realistically, this could be done from BuildTokenCache automatically.
            // Note that WARA_P09 is pointed at WAUA generically since its exact target varies.
            internal static Dictionary<string, string> spinningTopTargets = new()
            {
                {"WARA_P09", "WAUA"}, {"WTDB_A26", "WRFB_D09"}, {"WAUA_TOYS", "NULL"}, {"WARF_B33", "WTDA_B12"}, {"WARD_R02", "WARB_F01"}, {"WARC_F01", "WARA_E08"}, {"WAUA_BATH", "SB_D07"}, {"LF_B01W", "WRFA_SK04"}, {"WARB_J01", "WARA_P05"}, {"WPTA_F03", "WARA_P08"}, {"WBLA_D03", "WSKD_B01"}, {"WRFB_A22", "WARE_I01X"}, {"WARE_H05", "WSKC_A03"}, {"WSKC_A23", "WPTA_B10"}, {"WTDA_Z14", "WBLA_C01"}, {"WSKD_B40", "WARD_R15"}, {"WVWA_F03", "WARC_E03"}, {"SH_A08", "WSKA_D02"}, {"CC_C12", "WSKB_C17"}
            };

            internal StaticKey(string region1, string region2)
            {
                name = string.Join("-", (new string[] { region1.Region(), region2.Region() }).OrderBy(x => x));
                canHaveKey = CanHaveStaticKey(name);
            }
            internal static bool CanHaveStaticKey(string name)
            {
                if (name.Contains("WAUA") && name != "WARA-WAUA") return false;  // leading out of Ancient Urban
                if (name == "WARA-WRSA") return false;  // leading out of Daemon
                if (name.Contains("WSUR") || name.Contains("WHIR") || name.Contains("WGWR") || name.Contains("WDSR")) return false;  // rot
                if (name == "NULL-WSSR") return false;

                return true;
            }
            internal readonly bool Missing => canHaveKey && !CollectedStaticKeys.Contains(name);

            internal static bool IsMissing(string region1, string region2) => new StaticKey(region1, region2).Missing;

            internal static StaticKey? FromSpinningTop(string room) => spinningTopTargets.TryGetValue(room.ToUpperInvariant(), out string dest) ? new(room, dest) : null;
        }

        /// <summary>Updates the Ripple levels of the currently loaded <see cref="DeathPersistentSaveData"/>.</summary>
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

        internal static void ResetItems()
        {
            CollectedDynamicKeys.Clear();
            CollectedStaticKeys.Clear();
            RippleIncrements = 0;
        }
    }
}
