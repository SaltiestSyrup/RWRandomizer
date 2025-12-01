using UnityEngine;
using Random = UnityEngine.Random;
using WarpPoint = Watcher.WarpPoint;

namespace RainWorldRandomizer.WatcherIntegration
{
    public static class StaticWarps
    {
        /// <summary>Determine if this <see cref="WarpPoint"/> needs a key which is not yet collected.</summary>
        private static bool MissingKey(this WarpPoint self) =>
            self.Data.nonDynamicWarpPoint && Items.StaticKey.IsMissing(self.room.world.region.name, self.Data.RegionString is null ? "WRSA" : self.Data.RegionString);

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
            }

            internal static void RemoveHooks()
            {
                On.Watcher.WarpPoint.Update -= WarpPoint_Update;
            }

            /// <summary>Temporarily mark static warps as sealed if the key is not collected</summary>
            private static void WarpPoint_Update(On.Watcher.WarpPoint.orig_Update orig, WarpPoint self, bool eu)
            {
                orig(self, eu);

                if (self.currentState != WarpPoint.State.Sealed && self.MissingKey())
                {
                    self.Rando_LockWarp(false);
                }
            }
        }
    }
}
