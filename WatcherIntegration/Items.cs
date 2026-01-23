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
        internal static Vector2 Ripple => new(Mathf.Clamp(-1f + RippleIncrements / 2f, 1f, 5f), Mathf.Clamp(1f + RippleIncrements / 2f, 1f, 5f));
        internal static List<string> collectedDynamicKeys = [];
        internal static List<string> CollectedDynamicKeys => collectedDynamicKeys;  // this could get from where the data actually gets stored later
        internal static List<string> collectedStaticKeys = [];
        internal static List<string> CollectedStaticKeys => collectedStaticKeys;

        internal static List<string> unkeyableWarps =
        [
            "WORA-WSSR", "WORA-WSUR", "WGWR-WORA", "WHIR-WORA", "WDSR-WORA", "WARA-WRSA"
        ];

        internal static string[] spinningTopWarps =
        [
            "WARF-WTDA", "WBLA-WVWB", "WRFB-WTDB", "WARA-WARC", "WARD-WVWB", "WARE-WSKC", "WARA-WARB",
            "WARA-WPTA", "WPTA-WSKC", "WBLA-WTDA", "WARE-WRFB", "WARC-WVWA", "WARA-WAUA"
        ];

        internal struct StaticKey
        {
            internal string name;

            internal StaticKey(string region1, string region2)
            {
                name = string.Join("-", (new string[] { region1.Region(), region2.Region() }).OrderBy(x => x));
            }

            internal readonly bool CanHaveStaticKey
            {
                get
                {
                    if (!Settings.spinningTopKeys && spinningTopWarps.Contains(name)) return false;

                    if (name.Contains("WAUA") && name != "WARA-WAUA") return false;  // leading out of Ancient Urban
                    if (name == "WARA-WRSA") return false;  // leading out of Daemon
                    if (name.Contains("WSUR") || name.Contains("WHIR") || name.Contains("WGWR") || name.Contains("WDSR")) return false;  // rot
                    if (name == "NULL-WSSR") return false;
                    if (name == "WORA-WORA") return false;

                    return true;
                }
            }

            internal readonly bool Missing => CanHaveStaticKey && !CollectedStaticKeys.Contains(name);

            internal static bool IsMissing(string region1, string region2) => new StaticKey(region1, region2).Missing;
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
            string[] split = item.Split(['-'], 2);

            switch (split[0])  // switch on first part of item name
            {
                case "Dynamic": CollectedDynamicKeys.Add(split[1]); break;
                case "Warp": CollectedStaticKeys.Add(split[1]); break;
                case "Ripple": RippleIncrements++; UpdateRipple(); break;
                default:
                    if (item == "Dial_Warp")
                    {
                        Plugin.RandoManager.GivenRippleEggWarp = true;
                        if (Plugin.Singleton.Game?.GetStorySession?.saveState is not null)
                            Plugin.Singleton.Game.GetStorySession.saveState.miscWorldSaveData.hasRippleEggWarpAbility = true;
                    }
                    break;
            }
        }

        internal static void ResetItems()
        {
            CollectedDynamicKeys.Clear();
            CollectedStaticKeys.Clear();
            RippleIncrements = 0;
        }

        internal static List<string> GetAllOpenWarps() => [.. CollectedStaticKeys, .. unkeyableWarps];

        internal static List<string> GetAllAccessibleRegions()
        {
            List<string> ret = [Plugin.RandoManager.customStartDen.Split('_')[0]];
            Dictionary<string, bool[]> keyDict = GetAllOpenWarps().ToDictionary(x => x, GateMapDisplay.CanUseGate);
            bool updated = true;
            while (updated)
            {
                updated = false;
                foreach (var pair in keyDict)
                {
                    List<string> names = [.. pair.Key.Split('-')];
                    bool[] usable = pair.Value;

                    for (int i = 0; i < 2; i++)
                    {
                        string here = names[i];
                        string there = names[1 - i];
                        if (usable[i] && ret.Contains(here) && !ret.Contains(there))
                        {
                            ret.Add(there);
                            ret.Add($"{there}*");
                            updated = true;
                        }
                    }
                }
            }
            return ret;
        }
    }
}
