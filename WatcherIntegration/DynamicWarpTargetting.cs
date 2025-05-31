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
                string region = room.name.Region();
                bool isWORA = region == "wora";
                DynWarpMode relevantMode = isWORA ? modeThrone : modeNormal;
                if (relevantMode == DynWarpMode.Ignored) return orig(room, spreadingRot); // shortcut
                string key = null;

                // Warping from WORA in Predetermined mode uses the room's name as the key.
                if (isWORA) key = relevantMode == DynWarpMode.Predetermined ? room.name.ToLowerInvariant() : null;
                // Warping from elsewhere in PUS mode requires the dynamic key.  Without it, return an empty list to fail the warp.
                else if (relevantMode == DynWarpMode.PredeterminedUnlockableSource && !Items.CollectedDynamicKeys.Contains(region))
                {
                    failureReason = FailureReason.MissingSourceKey;
                    return new List<string>();
                }
                // Warping from elsewhere in PUS mode with the dynamic key or in P mode uses the region as the key.
                else if (relevantMode.Predetermined()) key = region;
                // If the key was set from a previous step, use it to get the predetermined target and make that our only possible target.
                if (predetermination.TryGetValue(key, out string dest)) return new List<string> { dest };

                // Otherwise, get the list of candidate targets as usual, then filter depending on mode.
                List<string> candidates = orig(room, spreadingRot);

                // If the filtering step removes all remaining targets,
                // we want a unique message to indicate that Archipelago is the reason for warp failure.
                // But if, for some reason, there aren't any valid candidates to begin with,
                // that's some other problem that is causing the paths to overflow beyond measure,
                // and we don't want to override that failure message.
                if (candidates.Count > 0)
                {
                    switch (relevantMode)
                    {
                        case DynWarpMode.Visited: 
                            candidates = candidates.Where(x => visitedRegions.Contains(x.Region())).ToList();
                            if (candidates.Count == 0) failureReason = FailureReason.NoOtherVisitedRegions;
                            break;
                        case DynWarpMode.UnlockableTargetPool: 
                            candidates = candidates.Where(x => Items.CollectedDynamicKeys.Contains(x.Region())).ToList();
                            if (candidates.Count == 0) failureReason = FailureReason.NoUsableDynamicKeys;
                            break;
                        case DynWarpMode.StaticTargetPool:
                            candidates = candidates.Where(x => targetPool.Contains(x.Region())).ToList();
                            if (candidates.Count == 0) failureReason = FailureReason.NoValidStaticPoolTargets;
                            break;
                    }
                }
                return candidates;
            }

            internal enum FailureReason { MissingSourceKey, MissingPredetermination, NoOtherVisitedRegions, NoUsableDynamicKeys, NoValidStaticPoolTargets }
            internal static FailureReason? failureReason = null;

            internal static void Yes(ILContext il)
            {
                ILCursor c = new ILCursor(il);

                // Store a reference to the visited region list so we don't waste time recomputing it later.
                c.GotoNext(MoveType.Before, x => x.MatchLdloc(4));  // List<string> list3
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Stsfld, typeof(Hooks).GetField(nameof(visitedRegions)));
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
