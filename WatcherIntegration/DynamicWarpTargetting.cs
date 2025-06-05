using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using static RainWorldRandomizer.WatcherIntegration.Settings;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class DynamicWarpTargetting
    {
        internal enum WarpSourceKind { Other, Normal, Throne, Permarotted, Unrottable }
        internal static WarpSourceKind GetWarpSourceKind(string room)
        {
            if (room.ToUpperInvariant().StartsWith("WORA_THRONE")) return WarpSourceKind.Throne;
            string region = room.Split('_')[0];
            if (Region.IsSentientRotRegion(region)) return WarpSourceKind.Permarotted;
            if (Region.HasSentientRotResistance(region)) return WarpSourceKind.Unrottable;
            return WarpSourceKind.Normal;
        }

        internal static class Hooks
        {
            internal static void Apply()
            {
                IL.Watcher.WarpPoint.GetAvailableDynamicWarpTargets += Yes;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets += PredetermineOrFilter;
                IL.Player.SpawnDynamicWarpPoint += Player_SpawnDynamicWarpPoint;
            }

            internal static void Unapply()
            {
                IL.Watcher.WarpPoint.GetAvailableDynamicWarpTargets -= Yes;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets -= PredetermineOrFilter;
                IL.Player.SpawnDynamicWarpPoint -= Player_SpawnDynamicWarpPoint;
            }

            /// <summary>Sets a custom failure message for if Archipelago dynamic warping rules specifically cause a failure.</summary>
            private static void Player_SpawnDynamicWarpPoint(ILContext il)
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, x => x.MatchLdstr("The paths overflow beyond measure"));  // 0c1d
                c.EmitDelegate<Func<string, string>>(CustomFailureMessage);
            }

            internal static string CustomFailureMessage(string orig)
            {
                switch (failureReason)
                {
                    case FailureReason.MissingSourceKey: return "Something is missing... the dynamic key?";
                    case FailureReason.NoOtherVisitedRegions: return "No threads lead to new horizons";
                    case FailureReason.MissingPredetermination: return "Something went terribly wrong here";
                    case FailureReason.NoUsableDynamicKeys: return "Something is missing... any dynamic key?";
                    case FailureReason.NoValidStaticPoolTargets: return "There are few paths and none are within reach";
                    default: return orig;
                }
            }

            private static List<string> PredetermineOrFilter(On.Watcher.WarpPoint.orig_GetAvailableDynamicWarpTargets orig, AbstractRoom room, bool spreadingRot)
            {
                failureReason = null;
                string roomName = room.name.ToUpperInvariant();
                string region = roomName.Region();
                WarpSourceKind sourceKind = GetWarpSourceKind(roomName);
                DynWarpMode relevantMode = sourceKind == WarpSourceKind.Throne ? modeThrone : modeNormal;

                // For a predetermined mode, we return a single room early instead of running the original method.
                if (relevantMode.Predetermined())
                {
                    // If we need the key but do not have it, set a failure reason and return empty.
                    if (relevantMode == DynWarpMode.PredeterminedUnlockableSource && !Items.CollectedDynamicKeys.Contains(region))
                    {
                        failureReason = FailureReason.MissingSourceKey;
                        return new List<string> { };
                    }
                    // Specifically in the Throne, we need to index the mapping with the room; otherwise, index with the region.
                    if (predetermination.TryGetValue(sourceKind == WarpSourceKind.Throne ? roomName : region, out string destRoom))
                    {
                        return new List<string> { destRoom };
                    }

                    // Ideally this failure state never happens - it would imply either custom regions,
                    // some sort of generation failure, or incomplete slot data.
                    failureReason = FailureReason.MissingPredetermination;
                    return new List<string> { };
                }

                // For a non-predetermined mode, get the ordinarily valid candidate targets.
                List<string> candidates = orig(room, spreadingRot);

                // If, for some reason, there are no valid candidate to begin with,
                // something else has gone wrong and we don't need to bother with a custom failure reason.
                if (candidates.Count == 0) return candidates;
                Plugin.Log.LogDebug($"Original candidates for dynamic warp: [{string.Join(",", candidates)}]");

                // Otherwise, we may need to filter this list, depending on the warp mode.
                IEnumerable<string> regionFilter = null;
                FailureReason? latentFR = null;
                switch (relevantMode)
                {
                    case DynWarpMode.Visited: regionFilter = visitedRegions; latentFR = FailureReason.NoOtherVisitedRegions; break;
                    case DynWarpMode.StaticTargetPool: regionFilter = targetPool; latentFR = FailureReason.NoValidStaticPoolTargets; break;
                    case DynWarpMode.UnlockableTargetPool: regionFilter = Items.CollectedDynamicKeys; latentFR = FailureReason.NoUsableDynamicKeys; break;
                }

                if (regionFilter != null)
                {
                    candidates = candidates.Where(x => regionFilter.Contains(x.Region())).ToList();
                    if (candidates.Count == 0) failureReason = latentFR;
                }
                Plugin.Log.LogDebug($"Filtered candidates for dynamic warp: [{string.Join(",", candidates)}]");
                return candidates;
            }

            internal enum FailureReason { MissingSourceKey, MissingPredetermination, NoOtherVisitedRegions, NoUsableDynamicKeys, NoValidStaticPoolTargets }
            internal static FailureReason? failureReason = null;

            internal static void Yes(ILContext il)
            {
                ILCursor c = new ILCursor(il);

                // Store a reference to the visited region list so we don't waste time recomputing it later.
                c.GotoNext(MoveType.After, x => x.MatchLdloc(4));  // List<string> list3
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Stsfld, typeof(Hooks).GetField(nameof(visitedRegions), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
                //c.EmitDelegate<Action<List<string>>>(StoreVisitedRegions);

                // Comparative branch interception.  Waive Ripple requirement if that setting is enabled.
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(KeyValuePair<float, List<string>>).GetProperty(nameof(KeyValuePair<float, List<string>>.Key)).GetGetMethod()));  // 0174
                c.EmitDelegate<Func<float, float>>(WaiveRippleRequirement);
            }

            internal static List<string> visitedRegions;
            internal static void StoreVisitedRegions(List<string> visited) => visitedRegions = visited;
            internal static float WaiveRippleRequirement(float orig) => rippleReq == RippleReqMode.None ? float.MinValue : orig;
        }
    }
}
