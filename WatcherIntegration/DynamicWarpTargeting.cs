using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Watcher;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class DynamicWarpTargeting
    {
        /// <summary>The maximum multiplier on dynamic warp weighting that check completion can have</summary>
        private const int INFLUENCE_COMPLETION = 10;
        /// <summary>The multiplier on dynamic warp weighting that spreading rot provides</summary>
        private const int INFLUENCE_ROT_SPREAD = 8;
        /// <summary>The additional multiplier on dynamic warp weighting that spreading rot provides when the flag 
        /// for rot ending "endgame" is triggered (Spoken to prince after 12 regions)</summary>
        private const int INFLUENCE_ROT_ENDGAME = 3;
        /// <summary>The multiplier on dynamic warp weighting applied when the region has warps to seal</summary>
        private const int INFLUENCE_WARPS_TO_SEAL = 16;

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
        
        private static List<string> GetAllAccessibleRegions()
        {
            List<string> ret = [Plugin.RandoManager.customStartDen.Split('_')[0]];
            Dictionary<string, bool[]> keyDict = Plugin.RandoManager.GetAllOpenWarps().ToDictionary(x => x, Menu.GateMapDisplay.CanUseGate);
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

        internal static class Hooks
        {
            internal static void ApplyHooks()
            {
                On.Watcher.WarpPoint.NewWorldLoaded_Room += OnNewWorldLoaded;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool += GetAvailableDynamicWarpTargets;
                On.Watcher.WarpPoint.GetAvailableBadWarpTargets_World_string += GetAvailableBadWarpTargets;
                
                try
                {
                    IL.Player.SpawnDynamicWarpPoint += SpawnDynamicWarpPointIL;
                    IL.OverWorld.InitiateSpecialWarp_WarpPoint += OverWorldOnInitiateSpecialWarp_WarpPointIL;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError(e);
                }
            }

            internal static void RemoveHooks()
            {
                On.Watcher.WarpPoint.NewWorldLoaded_Room -= OnNewWorldLoaded;
                On.Watcher.WarpPoint.GetAvailableDynamicWarpTargets_World_string_string_bool -= GetAvailableDynamicWarpTargets;
                On.Watcher.WarpPoint.GetAvailableBadWarpTargets_World_string -= GetAvailableBadWarpTargets;

                IL.Player.SpawnDynamicWarpPoint -= SpawnDynamicWarpPointIL;
                IL.OverWorld.InitiateSpecialWarp_WarpPoint -= OverWorldOnInitiateSpecialWarp_WarpPointIL;
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

            /// <summary>
            /// Reimplemtation of this method, because so much has to change. Decides where a regular dynamic warp should go.
            /// </summary>
            private static List<string> GetAvailableDynamicWarpTargets(On.Watcher.WarpPoint.orig_GetAvailableDynamicWarpTargets_World_string_string_bool orig,
                World world, string oldRoom, string targetRegion, bool spreadingRot)
            {
                if (Plugin.RandoManager?.isRandomizerActive is not true) return orig(world, oldRoom, targetRegion, spreadingRot);
                SaveState saveState = world.game.GetStorySession.saveState;
                bool hasWeaverAbility = saveState.miscWorldSaveData.hasVoidWeaverAbility;

                List<string> regionCandidates = [..GetAllAccessibleRegions().Select(r => r.ToLowerInvariant())];

                //TODO: Make only targeting visited regions an option (Either client or yaml, depending on if other methods get added)
                //for (int i = 0; i < saveState.regionStates.Length; i++)
                //{
                //    if (saveState.regionStates[i] is not null) 
                //        regionCandidates.Add(saveState.regionStates[i].regionName.ToLowerInvariant());
                //    else if (saveState.regionLoadStrings[i] is not null)
                //        regionCandidates.Add(Regex.Split(Regex.Split(saveState.regionLoadStrings[i], "<rgA>")[0], "<rgB>")[1].ToLowerInvariant());
                //}

                List<string> rottedRegions = ["wsur", "whir", "wgwr", "wdsr", "wora", "wssr"];

                // If bad warping is difficult, make rotted regions possible dynamic targets
                // TODO: Adjust this when karmaless warping item is added
                if (hasWeaverAbility)
                {
                    spreadingRot = false;
                    regionCandidates.AddRange(rottedRegions.Where(r => Plugin.RandoManager.PercentOfRegionComplete(r) >= 0 && Plugin.RandoManager.PercentOfRegionComplete(r) < 1));
                }
                else
                {
                    regionCandidates.RemoveAll(rottedRegions.Contains);
                }

                // Always respect target region parameter
                if (targetRegion is not null) regionCandidates = [targetRegion];

                // Get all regions that have warps that need to be sealed
                List<string> regionsWithSealableWarps = [];
                if (hasWeaverAbility)
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

                List<string> normalWeightedCandidates = [];
                List<string> fallbackCandidates = [];

                world.game.rainWorld.levelDynamicWarpTargets.TryGetValue(999f, out List<string> noRippleTargets);
                string finalFallbackWarp = null;
                foreach (KeyValuePair<string, List<string>> regionWarpsPair in world.game.rainWorld.regionDynamicWarpTargets) // REGION LOOP
                {
                    if (!regionCandidates.Contains(regionWarpsPair.Key)) continue;

                    int weight = 1;

                    // Weight regions by check completion
                    float regionCompletion = Plugin.RandoManager.PercentOfRegionComplete(regionWarpsPair.Key.ToUpperInvariant());
                    if (regionCompletion != -1f)
                    {
                        // Full influence if region has 0% checks complete, lerp towards no influence if 100% complete
                        //Plugin.Log.LogDebug($"Influence of {regionWarpsPair.Key} with {regionCompletion * 100f}% completion: " +
                        //    $"{(int)Mathf.Lerp(INFLUENCE_COMPLETION, 1, regionCompletion)}");
                        weight *= (int)Mathf.Lerp(INFLUENCE_COMPLETION, 1, regionCompletion);
                    }

                    // Weight regions if spreading rot to them
                    bool inRotEndgame = saveState.miscWorldSaveData.highestPrinceConversationSeen >= PrinceBehavior.PrinceConversation.PrinceConversationToId(WatcherEnums.ConversationID.Prince_7);
                    if (spreadingRot
                        && !saveState.miscWorldSaveData.regionsInfectedBySentientRot.Contains(regionWarpsPair.Key)
                        && !Region.HasSentientRotResistance(regionWarpsPair.Key))
                    {
                        weight *= INFLUENCE_ROT_SPREAD;
                        if (inRotEndgame) weight *= INFLUENCE_ROT_ENDGAME;
                    }

                    // Weight regions if we need to seal warps there
                    if (regionsWithSealableWarps.Contains(regionWarpsPair.Key)) weight *= INFLUENCE_WARPS_TO_SEAL;

                    // If we're close to rot ending or have the weaver ability, remove the possibility of warping to regions without checks remaining
                    if (weight == 1 && (inRotEndgame || hasWeaverAbility)) weight--;

                    foreach (string warpTarget in regionWarpsPair.Value) // WARP DESTINATION LOOP
                    {
                        string targetRoom = warpTarget.Split(':')[0];

                        if (targetRoom == oldRoom) continue;
                        //if (world.game.rainWorld.levelBadWarpTargets.Contains(targetRoom)) continue;
                        if (targetRegion is null && noRippleTargets.Contains(targetRoom)) continue;

                        // The fallback warp can be in the current region or a room with a warp in it if need be
                        finalFallbackWarp = targetRoom;

                        // Don't warp to the region we're currently in
                        if (targetRegion is null && regionWarpsPair.Key.Equals(world.name, StringComparison.InvariantCultureIgnoreCase)) continue;

                        // Don't warp to rooms with a warp in them already
                        if (saveState.miscWorldSaveData.discoveredWarpPoints.Any(w => WarpPoint.RoomFromIdentifyingString(w.Key) == targetRoom)) continue;
                        if (saveState.deathPersistentSaveData.spawnedWarpPoints.Any(w => WarpPoint.RoomFromIdentifyingString(w.Key) == targetRoom)) continue;

                        // If region was targeted, all weights are equal
                        if (targetRegion is not null)
                        {
                            normalWeightedCandidates.Add(targetRoom);
                            continue;
                        }

                        Plugin.Log.LogDebug($"Adding target room \"{targetRoom}\" to dynamic warp list with weight of {Mathf.CeilToInt((float)weight / regionWarpsPair.Value.Count)}");
                        for (int i = 0; i < Mathf.CeilToInt((float)weight / regionWarpsPair.Value.Count); i++)
                            normalWeightedCandidates.Add(targetRoom);
                        fallbackCandidates.Add(targetRoom);
                    }
                }

                // If normal candidate list is empty because of 0 weights, use fallback list with 1 weight for each valid room.
                // If normal candidate list is empty for some other reason, use final fallback target.
                if (normalWeightedCandidates.Count == 0)
                {
                    Plugin.Log.LogInfo("No weighted targets for dynamic warp, using fallback");
                    return fallbackCandidates.Count > 0 ? fallbackCandidates : [finalFallbackWarp];
                }
                return normalWeightedCandidates;
            }

            private static List<string> GetAvailableBadWarpTargets(On.Watcher.WarpPoint.orig_GetAvailableBadWarpTargets_World_string orig, World world, string oldRoom)
            {
                List<string> origRet = orig(world, oldRoom);
                if (Plugin.RandoManager?.isRandomizerActive is not true) return origRet;

                List<string> badWeightedCandidates = [];
                foreach (string warpTarget in origRet)
                {
                    if (!warpTarget.Contains('_')) continue;
                    string region = warpTarget.Split('_')[0];
                    float regionCompletion = Plugin.RandoManager.PercentOfRegionComplete(region.ToUpperInvariant());
                    if (regionCompletion == -1f) continue;

                    // Full influence if region has 0% checks complete, lerp towards not added at all if 100% complete
                    //Plugin.Log.LogDebug($"Influence of {region} with {regionCompletion * 100f}% completion: " +
                    //    $"{(int)Mathf.Lerp(INFLUENCE_COMPLETION, 0, regionCompletion)}");
                    int weight = (int)Mathf.Lerp(INFLUENCE_COMPLETION, 0, regionCompletion);

                    Plugin.Log.LogDebug($"Adding warp \"{warpTarget}\" to list with weight of {weight}");
                    for (int i = 0; i < weight; i++) badWeightedCandidates.Add(warpTarget);
                }

                if (badWeightedCandidates.Any()) return badWeightedCandidates;
                else return origRet;
            }
            
            /// <summary>
            /// Makes it possible to reach WORA city when there are no waiting Prince encounters
            /// </summary>
            private static void OverWorldOnInitiateSpecialWarp_WarpPointIL(ILContext il)
            {
                ILCursor c = new(il);

                c.GotoNext(x =>
                    x.MatchLdfld(
                        typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.maximumRippleLevel))));
                c.GotoPrev(MoveType.Before, x => x.MatchLdarg(0));
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate(ToOuterRimIfNeeded);
                
                return;

                static void ToOuterRimIfNeeded(WarpPoint.WarpPointData warpData)
                {
                    if (Plugin.RandoManager is null) return;

                    float percentWORA = Mathf.Abs(Plugin.RandoManager.PercentOfRegionComplete("WORA"));
                    // Average completion of all rotted regions (If region not found, it is considered complete)
                    float percentRotRegions = new[] {"WSUR", "WHIR", "WGWR", "WDSR"}
                        .Sum(r => Mathf.Abs(Plugin.RandoManager.PercentOfRegionComplete(r))) / 4;

                    // If more has been completed from rotted regions than WORA, guarantee sending player to WORA
                    if (percentRotRegions > percentWORA) warpData.RegionString = "WORA";
                }
            }
        }
    }
}
