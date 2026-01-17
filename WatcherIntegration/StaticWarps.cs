using RWCustom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Watcher;
using Random = UnityEngine.Random;
using WarpPoint = Watcher.WarpPoint;

namespace RainWorldRandomizer.WatcherIntegration
{
    public static class StaticWarps
    {
        /// <summary>
        /// Determine if this <see cref="WarpPoint"/> needs a key which is not yet collected.
        /// </summary>
        private static bool MissingKey(this WarpPoint self) =>
            self.Data.nonDynamicWarpPoint
            && Items.StaticKey.IsMissing(self.room.world.region.name, self.Data.RegionString ?? (self.room.world.region.name == "WARA" ? "WAUA" : "WRSA"));

        /// <summary>
        /// Keeps track of warp tear graphics that should be displaying as locked
        /// </summary>
        private static ConditionalWeakTable<WarpTear, WarpPoint> ActiveLockedWarps = new();

        /// <summary>
        /// Alt version of <see cref="WarpPoint.LockWarp(bool)"/> that doesn't change any of its data
        /// </summary>
        private static void Rando_LockWarp(this WarpPoint self, bool withAnimation)
        {
            if (self.warpLocked) return;

            self.warpLocked = true;
            self.refreshGraphics = true;

            if (!withAnimation && self.warpTear is not null)
            {
                self.warpTear.weaverBlockAnimation = 1f;
                self.warpTear.openAnimation = 1f;
            }

            self.weaverThreadAnimTime = 300;
            self.weaverAnimationsStarts.Clear();
            self.weaverAnimationsEnds.Clear();
            self.weaverThreadAbsPos.Clear();
            int branches = Random.Range(12, 16);
            float minRadius = self.badWarpRadiusMax + 60f;
            Random.State state = Random.state;
            Random.InitState(self.room.abstractRoom.index);
            for (int i = 0; i < branches; i++)
            {
                float angle = 180f / branches * i + Random.Range(-20, 20);
                Vector2 vector = RWCustom.Custom.DegToVec(angle) * (minRadius + Random.Range(-20, 20));
                Vector2 vector2 = RWCustom.Custom.DegToVec(angle + 180f) * (minRadius + Random.Range(-20, 20));
                bool even = i % 2 == 0;
                self.weaverAnimationsStarts.Add(even ? vector : vector2);
                self.weaverAnimationsEnds.Add(even ? vector2 : vector);
                self.weaverThreadAbsPos.Add(self.placedObject.pos + RWCustom.Custom.RNV() * 40f);
            }

            self.warpTear?.GetThreadAnimation(self.weaverAnimationsStarts, self.weaverAnimationsEnds, self.weaverThreadAbsPos);
            self.weaverAnimationTime = withAnimation ? self.weaverAnimationsStarts.Count + self.weaverThreadAnimTime : -1;
            self.totalWeaverAnimationTime = self.weaverAnimationTime;
            self.weaverAnimationStarted = withAnimation;
            Random.state = state;
            self.room.game.warpDeferPlayerSpawnRoomName = "";
        }

        public static class Hooks
        {
            internal static void ApplyHooks()
            {
                On.Watcher.WarpPoint.Update += WarpPoint_Update;
                On.Watcher.WarpTear.InitiateSprites += WarpTear_InitiateSprites;
                On.Watcher.WarpPoint.ExpireWarpByWeaver += WarpPoint_ExpireWarpByWeaver;
            }

            internal static void RemoveHooks()
            {
                On.Watcher.WarpPoint.Update -= WarpPoint_Update;
                On.Watcher.WarpTear.InitiateSprites -= WarpTear_InitiateSprites;
                On.Watcher.WarpPoint.ExpireWarpByWeaver -= WarpPoint_ExpireWarpByWeaver;
            }

            /// <summary>
            /// Temporarily mark static warps as sealed if the key is not collected
            /// </summary>
            private static void WarpPoint_Update(On.Watcher.WarpPoint.orig_Update orig, WarpPoint self, bool eu)
            {
                orig(self, eu);

                if (self.MissingKey())
                {
                    if (self.currentState != WarpPoint.State.Sealed)
                        self.Rando_LockWarp(false);
                    if (self.warpTear is not null && !ActiveLockedWarps.TryGetValue(self.warpTear, out _))
                        ActiveLockedWarps.Add(self.warpTear, self);
                }
            }

            /// <summary>
            /// Apply custom shader to locked warps to make them red
            /// </summary>
            private static void WarpTear_InitiateSprites(On.Watcher.WarpTear.orig_InitiateSprites orig, Watcher.WarpTear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                orig(self, sLeaser, rCam);

                if (ActiveLockedWarps.TryGetValue(self, out _))
                    sLeaser.sprites[0].shader = Custom.rainWorld.Shaders["Rando.WarpTear"];
            }

            /// <summary>
            /// When sealing a warp, make the Weaver seal all other warps in the region the player left
            /// </summary>
            private static void WarpPoint_ExpireWarpByWeaver(On.Watcher.WarpPoint.orig_ExpireWarpByWeaver orig, WarpPoint self)
            {
                orig(self);
                SaveState saveState = self.room.game.GetStorySession.saveState;
                string regionToSeal = self.Data.destRegion;
                // Don't seal the region if any of the following are true
                if (regionToSeal is null // The region we left is unknown
                    || Region.IsWatcherVanillaRegion(regionToSeal) // Leaving a tutorial region
                    || Region.IsSentientRotRegion(regionToSeal) // Leaving a rotted region
                    || self.Data.rippleWarp // The warp is a ripple warp
                    || (self.Data.limitedUse && self.Data.uses <= 1) // The warp is a "limited use" warp
                    || self.MyIdentifyingString() != self.room.game.GetStorySession.pendingWarpPointTransferId) // We didn't just pass through this warp
                    return;

                Plugin.Log.LogDebug($"Attempting to seal all region warps");
                Plugin.Log.LogDebug($"This warp point: origin = {self.room.abstractRoom.name}, destination = {self.Data.destRoom}, sealing region = {regionToSeal}");

                List<string> roomsWithWarpsRemaining = self.room.game.GetStorySession.saveState.RoomsWithWarpsRemainingToBeSealed(true, regionToSeal);
                foreach (string warpRoom in roomsWithWarpsRemaining)
                {

                    string destRoom = FindDestRoom(regionToSeal.ToLowerInvariant(), warpRoom).ToLowerInvariant();
                    if (destRoom is null)
                    {
                        Plugin.Log.LogDebug($"Could not find destination room to seal warp in room: {warpRoom}");
                        continue;
                    }

                    if (!saveState.miscWorldSaveData.roomsSealedByVoidWeaver.Contains(warpRoom))
                        saveState.miscWorldSaveData.roomsSealedByVoidWeaver.Add(warpRoom);
                    if (!saveState.miscWorldSaveData.roomsSealedByVoidWeaver.Contains(destRoom))
                        saveState.miscWorldSaveData.roomsSealedByVoidWeaver.Add(destRoom);

                    Plugin.Log.LogDebug($"Added {warpRoom} & {destRoom} to roomsSealedByVoidWeaver");
                }

                // Attempts to find the specified warp and return the room the warp leads to
                string FindDestRoom(string regionLower, string warpRoom)
                {
                    foreach (var kvp in saveState.deathPersistentSaveData.spawnedWarpPoints)
                    {
                        if (WarpPoint.RoomFromIdentifyingString(kvp.Key).ToLowerInvariant() == warpRoom)
                        {
                            PlacedObject placedObject = new PlacedObject(PlacedObject.Type.WarpPoint, null);
                            (placedObject.data as WarpPoint.WarpPointData).owner.FromString(Regex.Split(kvp.Value.Trim(), "><"));
                            return (placedObject.data as WarpPoint.WarpPointData).destRoom;
                        }
                    }
                    foreach (var kvp in saveState.miscWorldSaveData.discoveredWarpPoints)
                    {
                        if (WarpPoint.RoomFromIdentifyingString(kvp.Key).ToLowerInvariant() == warpRoom)
                        {
                            PlacedObject placedObject = new PlacedObject(PlacedObject.Type.WarpPoint, null);
                            (placedObject.data as WarpPoint.WarpPointData).owner.FromString(Regex.Split(kvp.Value.Trim(), "><"));
                            return (placedObject.data as WarpPoint.WarpPointData).destRoom;
                        }
                    }
                    if (Custom.rainWorld.regionWarpRooms.TryGetValue(regionLower, out List<string> regionWarps))
                    {
                        foreach (string data in regionWarps)
                        {
                            string[] split = data.Split([':']);
                            if (split[0].ToLowerInvariant() == warpRoom && split.Length > 3) return split[3];
                        }
                    }
                    if (Custom.rainWorld.regionSpinningTopRooms.TryGetValue(regionLower, out List<string> regionWarps2))
                    {
                        foreach (string data in regionWarps2)
                        {
                            string[] split = data.Split([':']);
                            if (split[0].ToLowerInvariant() == warpRoom && split.Length > 2) return split[2];
                        }
                    }
                    return null;
                }
            }
        }
    }
}
