using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Watcher;
using static RainWorldRandomizer.WatcherIntegration.Settings;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class DynamicWarpTargetting
    {
        /// <summary>The maximum multiplier on dynamic warp weighting that check completion can have</summary>
        private const int INFLUENCE_COMPLETION = 10;
        /// <summary>The multiplier on dynamic warp weighting that spreading rot provides</summary>
        private const int INFLUENCE_ROT_SPREAD = 8;
        /// <summary>The additional multiplier on dynamic warp weighting that spreading rot provides when the flag 
        /// for rot ending "endgame" is triggered (Spoken to prince after 12 regions)</summary>
        private const int INFLUENCE_ROT_ENDGAME = 3;
        /// <summary>The multiplier on dynamic warp weighting applied when the region has warps to seal</summary>
        private const int INFLUENCE_WARPS_TO_SEAL = 8;

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

                //IL.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool += WaiveRippleReq;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool += GetAvailableDynamicWarpTargets;
                IL.Player.FailToSpawnWarpPoint += CustomFailureMessage;
            }

            internal static void RemoveHooks()
            {
                IL.Player.SpawnDynamicWarpPoint -= SpawnDynamicWarpPointIL;
                On.Watcher.WarpPoint.NewWorldLoaded_Room -= OnNewWorldLoaded;

                //IL.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool -= WaiveRippleReq;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool -= GetAvailableDynamicWarpTargets;
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

            /// <summary>
            /// Reimplemtation of this method, because so much has to change. Decides where a regular dynamic warp should go.
            /// </summary>
            private static List<string> GetAvailableDynamicWarpTargets(On.Watcher.WarpPoint.orig_GetAvailableDynamicWarpTargets_World_string_string_bool orig,
                World world, string oldRoom, string targetRegion, bool spreadingRot)
            {
                if (Plugin.RandoManager?.isRandomizerActive is not true) return orig(world, oldRoom, targetRegion, spreadingRot);
                SaveState saveState = world.game.GetStorySession.saveState;

                if (saveState.miscWorldSaveData.hasVoidWeaverAbility) spreadingRot = false;

                // All regions that have been visited before (have a RegionState saved in the SaveState)
                // minus the region we're currently in
                List<string> regionCandidates = [.. saveState.regionStates
                    .Where(s => s is not null && s.regionName != world.name)
                    .Select(s => s.regionName.ToLowerInvariant())];

                // Always respect target region parameter
                if (targetRegion is not null) regionCandidates = [targetRegion];

                // Get all regions that have warps that need to be sealed
                List<string> regionsWithSealableWarps = [];
                if (saveState.miscWorldSaveData.hasVoidWeaverAbility)
                {
                    foreach (string room in saveState.RoomsWithWarpsRemainingToBeSealed(true))
                    {
                        if (!room.Contains('_')) continue;
                        string reg = room.Split('_')[0].ToLowerInvariant();
                        if (!regionsWithSealableWarps.Contains(reg) 
                            && reg != world.name.ToLowerInvariant()
                            && world.game.rainWorld.regionDynamicWarpTargets.TryGetValue(reg, out List<string> targets)
                            && targets.Any())
                            regionsWithSealableWarps.Add(reg);
                    }
                }

                Dictionary<string, List<string>> candidatesByRegion = [];

                List<string> normalWeightedCandidates = [];

                world.game.rainWorld.levelDynamicWarpTargets.TryGetValue(999f, out List<string> noRippleTargets);
                string fallbackWarp = null;
                foreach (var regionWarpsPair in world.game.rainWorld.regionDynamicWarpTargets) // REGION LOOP
                {
                    if (!regionCandidates.Contains(regionWarpsPair.Key)) continue;

                    int weight = 1;

                    // Weight regions by check completion
                    float regionCompletion = Plugin.RandoManager.PercentOfRegionComplete(regionWarpsPair.Key);
                    if (regionCompletion != -1f)
                    {
                        // Full influence if region has 0% checks complete, lerp towards no influence if 100% complete
                        Plugin.Log.LogDebug($"Influence of {regionWarpsPair.Key} with {regionCompletion * 100f}% completion: " +
                            $"{(int)Mathf.Lerp(INFLUENCE_COMPLETION, 1, regionCompletion)}");
                        weight *= (int)Mathf.Lerp(INFLUENCE_COMPLETION, 1, regionCompletion);
                    }

                    // Weight regions if spreading rot to them
                    if (spreadingRot
                        && !saveState.miscWorldSaveData.regionsInfectedBySentientRot.Contains(regionWarpsPair.Key)
                        && !Region.HasSentientRotResistance(regionWarpsPair.Key))
                    {
                        weight *= INFLUENCE_ROT_SPREAD;
                        if (saveState.miscWorldSaveData.highestPrinceConversationSeen >= PrinceBehavior.PrinceConversation.PrinceConversationToId(WatcherEnums.ConversationID.Prince_7))
                            weight *= INFLUENCE_ROT_ENDGAME;
                    }

                    // Weight regions if we need to seal warps there
                    if (regionsWithSealableWarps.Contains(regionWarpsPair.Key)) weight *= INFLUENCE_WARPS_TO_SEAL;

                    foreach (string warpTarget in regionWarpsPair.Value) // WARP DESTINATION LOOP
                    {
                        // Skip rooms in the current region unless the target is a special case
                        if (regionWarpsPair.Key == world.name && (warpTarget == oldRoom || !noRippleTargets.Contains(warpTarget)))
                            continue;

                        fallbackWarp = warpTarget;

                        // Don't warp to rooms with a warp in them already
                        if (saveState.miscWorldSaveData.discoveredWarpPoints.Any(w => WarpPoint.RoomFromIdentifyingString(w.Key) == warpTarget)) continue;
                        if (saveState.deathPersistentSaveData.spawnedWarpPoints.Any(w => WarpPoint.RoomFromIdentifyingString(w.Key) == warpTarget)) continue;

                        // If region was targetted, all weights are equal
                        if (targetRegion is not null)
                        {
                            normalWeightedCandidates.Add(warpTarget);
                            continue;
                        }

                        for (int i = 0; i < weight; i++) normalWeightedCandidates.Add(warpTarget);
                    }
                }

                if (normalWeightedCandidates.Count == 0 && fallbackWarp is not null) normalWeightedCandidates.Add(fallbackWarp);
                return normalWeightedCandidates;
            }

            /// <summary>Intercept the list of normal dynamic warp targets.</summary>
            //private static List<string> PredetermineOrFilter(On.Watcher.WarpPoint.orig_GetAvailableDynamicWarpTargets_World_string_string_bool orig, World world, string oldRoom, string targetRegion, bool spreadingRot)
            //{
            //    failureReason = null;
            //    string roomName = oldRoom.ToUpperInvariant();
            //    string region = roomName.Region();
            //    WarpSourceKind sourceKind = GetWarpSourceKind(roomName);
            //    DynWarpMode relevantMode = sourceKind == WarpSourceKind.Throne ? modeThrone : modeNormal;

            //    // For a predetermined mode, we return a single room early instead of running the original method.
            //    if (relevantMode.Predetermined())
            //    {
            //        // If we need the key but do not have it, set a failure reason and return empty.
            //        if (relevantMode == DynWarpMode.UnlockablePredetermined
            //            && sourceKind is not WarpSourceKind.Permarotted or WarpSourceKind.Unrottable
            //            && !Items.CollectedDynamicKeys.Contains(region))
            //        {
            //            failureReason = FailureReason.MissingSourceKey;
            //            return [];
            //        }
            //        // Specifically in the Throne, we need to index the mapping with the room; otherwise, index with the region.
            //        if (predetermination.TryGetValue(sourceKind == WarpSourceKind.Throne ? roomName : region, out string destRoom))
            //        {
            //            return [destRoom];
            //        }

            //        // Ideally this failure state never happens - it would imply either custom regions,
            //        // some sort of generation failure, or incomplete slot data.
            //        failureReason = FailureReason.MissingPredetermination;
            //        return [];
            //    }

            //    // For a non-predetermined mode, get the ordinarily valid candidate targets.
            //    List<string> candidates = orig(world, oldRoom, targetRegion, spreadingRot);

            //    // If, for some reason, there are no valid candidate to begin with,
            //    // something else has gone wrong and we don't need to bother with a custom failure reason.
            //    if (candidates.Count == 0) return candidates;
            //    //Plugin.Log.LogDebug($"Original candidates for dynamic warp: [{string.Join(",", candidates)}]");

            //    // Otherwise, we may need to filter this list, depending on the warp mode.
            //    IEnumerable<string> regionFilter = null;
            //    FailureReason? latentFR = null;
            //    switch (relevantMode)
            //    {
            //        case DynWarpMode.Visited: regionFilter = visitedRegions; latentFR = FailureReason.NoOtherVisitedRegions; break;
            //        case DynWarpMode.StaticPool: regionFilter = targetPool; latentFR = FailureReason.NoValidStaticPoolTargets; break;
            //        case DynWarpMode.UnlockablePool: regionFilter = Items.CollectedDynamicKeys; latentFR = FailureReason.NoUsableDynamicKeys; break;
            //    }

            //    if (regionFilter != null)
            //    {
            //        regionFilter = regionFilter.Select(x => x.ToUpperInvariant());
            //        candidates = [.. candidates.Where(x => regionFilter.Contains(x.Region()))];
            //        if (candidates.Count == 0) failureReason = latentFR;
            //    }
            //    //Plugin.Log.LogDebug($"Filtered candidates for dynamic warp: [{string.Join(",", candidates)}]");
            //    return candidates;
            //}

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
