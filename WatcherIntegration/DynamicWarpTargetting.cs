using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using Watcher;
using static RainWorldRandomizer.WatcherIntegration.Settings;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class DynamicWarpTargetting
    {
        internal enum WarpSourceKind { Other, Normal, Throne, Permarotted, Unrottable }

        /// <summary>Get the kind of dynamic warp that would be performed from this room.</summary>
        internal static WarpSourceKind GetWarpSourceKind(string room)
        {
            if (room.ToUpperInvariant() is "WORA_THRONE10" or "WORA_THRONE09" or "WORA_THRONE07" or "WORA_THRONE05") return WarpSourceKind.Throne;
            string region = room.Split('_')[0];
            if (Region.IsSentientRotRegion(region)) return WarpSourceKind.Permarotted;
            if (Region.HasSentientRotResistance(region)) return WarpSourceKind.Unrottable;
            return WarpSourceKind.Normal;
        }

        internal static class Hooks
        {
            internal static void ApplyHooks()
            {
                IL.Player.SpawnDynamicWarpPoint += SpawnDynamicWarpPointIL;
                On.Watcher.WarpPoint.NewWorldLoaded_Room += OnNewWorldLoaded;

                IL.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool += WaiveRippleReq;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool += PredetermineOrFilter;
                IL.Player.FailToSpawnWarpPoint += CustomFailureMessage;
            }

            internal static void RemoveHooks()
            {
                IL.Player.SpawnDynamicWarpPoint -= SpawnDynamicWarpPointIL;
                On.Watcher.WarpPoint.NewWorldLoaded_Room -= OnNewWorldLoaded;

                IL.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool -= WaiveRippleReq;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool -= PredetermineOrFilter;
                IL.Player.FailToSpawnWarpPoint -= CustomFailureMessage;
            }

            // (This can be modified to make other single use warps if ever needed)
            /// <summary>
            /// Make the initial warp into the starting region a one-way
            /// </summary>
            private static void SpawnDynamicWarpPointIL(ILContext il)
            {
                ILCursor c = new(il);

                // Check if has weaver power at 0268
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchCallOrCallvirt(typeof(MiscWorldSaveData).GetProperty(nameof(MiscWorldSaveData.hasVoidWeaverAbility)).GetGetMethod())
                    );

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(OneWayIfFirstWarp);

                static bool OneWayIfFirstWarp(bool origVal, Player player)
                {
                    return RoomSpecificScript.WatcherRandomizedSpawn.warpPending || origVal;
                }
            }

            /// <summary>
            /// Remove ripple death screen effect after warp to start region
            /// </summary>
            private static void OnNewWorldLoaded(On.Watcher.WarpPoint.orig_NewWorldLoaded_Room orig, WarpPoint self, Room newRoom)
            {
                orig(self, newRoom);

                if (!RoomSpecificScript.WatcherRandomizedSpawn.warpPending) return;
                foreach (AbstractCreature crit in newRoom.game.Players)
                {
                    Player player = crit.realizedCreature as Player;
                    if (player is not null)
                    {
                        player.pendingForcedWarpRoom = null;
                        player.rippleDeathIntensity = 0f;
                        player.rippleDeathTime = 0;
                    }
                }
                RoomSpecificScript.WatcherRandomizedSpawn.warpPending = false;
            }

            /// <summary>Sets a custom failure message for if Archipelago dynamic warping rules specifically cause a failure.</summary>
            private static void CustomFailureMessage(ILContext il)
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, x => x.MatchLdstr("The paths overflow beyond measure"));  // 0c1d

                static string Delegate(string orig)
                {
                    return failureReason switch
                    {
                        FailureReason.MissingSourceKey => "Something is missing... the dynamic key?",
                        FailureReason.NoOtherVisitedRegions => "No threads lead to new horizons",
                        // Once the AP logic is cleaned up, this should not happen at all.
                        FailureReason.MissingPredetermination => "Something went terribly wrong here",
                        FailureReason.NoUsableDynamicKeys => "Something is missing... any dynamic key?",
                        // Static pool only errors if the pool is very small - if every pool region is either gated by Ripple or is the current region.
                        FailureReason.NoValidStaticPoolTargets => "There are few paths and none are within reach",
                        _ => orig,
                    };
                }

                c.EmitDelegate(Delegate);
            }

            /// <summary>Intercept the list of normal dynamic warp targets.</summary>
            private static List<string> PredetermineOrFilter(On.Watcher.WarpPoint.orig_GetAvailableDynamicWarpTargets_World_string_string_bool orig, World world, string oldRoom, string targetRegion, bool spreadingRot)
            {
                failureReason = null;
                string roomName = oldRoom.ToUpperInvariant();
                string region = roomName.Region();
                WarpSourceKind sourceKind = GetWarpSourceKind(roomName);
                DynWarpMode relevantMode = sourceKind == WarpSourceKind.Throne ? modeThrone : modeNormal;

                // For a predetermined mode, we return a single room early instead of running the original method.
                if (relevantMode.Predetermined())
                {
                    // If we need the key but do not have it, set a failure reason and return empty.
                    if (relevantMode == DynWarpMode.UnlockablePredetermined
                        && sourceKind is not WarpSourceKind.Permarotted or WarpSourceKind.Unrottable
                        && !Items.CollectedDynamicKeys.Contains(region))
                    {
                        failureReason = FailureReason.MissingSourceKey;
                        return [];
                    }
                    // Specifically in the Throne, we need to index the mapping with the room; otherwise, index with the region.
                    if (predetermination.TryGetValue(sourceKind == WarpSourceKind.Throne ? roomName : region, out string destRoom))
                    {
                        return [destRoom];
                    }

                    // Ideally this failure state never happens - it would imply either custom regions,
                    // some sort of generation failure, or incomplete slot data.
                    failureReason = FailureReason.MissingPredetermination;
                    return [];
                }

                // For a non-predetermined mode, get the ordinarily valid candidate targets.
                List<string> candidates = orig(world, oldRoom, targetRegion, spreadingRot);

                // If, for some reason, there are no valid candidate to begin with,
                // something else has gone wrong and we don't need to bother with a custom failure reason.
                if (candidates.Count == 0) return candidates;
                //Plugin.Log.LogDebug($"Original candidates for dynamic warp: [{string.Join(",", candidates)}]");

                // Otherwise, we may need to filter this list, depending on the warp mode.
                IEnumerable<string> regionFilter = null;
                FailureReason? latentFR = null;
                switch (relevantMode)
                {
                    case DynWarpMode.Visited: regionFilter = visitedRegions; latentFR = FailureReason.NoOtherVisitedRegions; break;
                    case DynWarpMode.StaticPool: regionFilter = targetPool; latentFR = FailureReason.NoValidStaticPoolTargets; break;
                    case DynWarpMode.UnlockablePool: regionFilter = Items.CollectedDynamicKeys; latentFR = FailureReason.NoUsableDynamicKeys; break;
                }

                if (regionFilter != null)
                {
                    regionFilter = regionFilter.Select(x => x.ToUpperInvariant());
                    candidates = [.. candidates.Where(x => regionFilter.Contains(x.Region()))];
                    if (candidates.Count == 0) failureReason = latentFR;
                }
                //Plugin.Log.LogDebug($"Filtered candidates for dynamic warp: [{string.Join(",", candidates)}]");
                return candidates;
            }

            internal enum FailureReason { MissingSourceKey, MissingPredetermination, NoOtherVisitedRegions, NoUsableDynamicKeys, NoValidStaticPoolTargets }

            /// <summary>The AP-specific reason, if any, that the next dynamic warp might fail.</summary>
            internal static FailureReason? failureReason = null;

            /// <summary>Waive Ripple requirements if set, and store the list of visited regions for efficiency.</summary>
            internal static void WaiveRippleReq(ILContext il)
            {
                ILCursor c = new(il);

                // Store a reference to the visited region list so we don't waste time recomputing it later.
                c.GotoNext(MoveType.After, x => x.MatchLdloc(4));  // List<string> list3
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Stsfld, typeof(Hooks).GetField(nameof(visitedRegions), EntryPoint.bfAll));

                // Comparative branch interception.  Waive Ripple requirement if that setting is enabled.
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(KeyValuePair<float, List<string>>).GetProperty(nameof(KeyValuePair<float, List<string>>.Key)).GetGetMethod()));  // 0174
                static float WaiveRippleRequirement(float orig) => rippleReq == RippleReqMode.None ? float.MinValue : orig;
                c.EmitDelegate(WaiveRippleRequirement);
            }

            internal static List<string> visitedRegions;
        }
    }
}
