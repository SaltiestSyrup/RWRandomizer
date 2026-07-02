using System.Linq;
using Watcher;

namespace RainWorldRandomizer.WatcherIntegration
{
    public static class RoomSpecificScript
    {
        public static void AddRoomSpecificScript(Room room)
        {
            if (Plugin.RandoManager.currentSlugcat != WatcherEnums.SlugcatStatsName.Watcher) return;
            string roomName = room.abstractRoom.name;

            switch (roomName)
            {
                case "HI_W14":
                    if (room.game.GetStorySession.saveState.cycleNumber == 0)
                        room.AddObject(new WatcherRandomizedSpawn(room));
                    break;
            }
        }

        /// <summary>
        /// Initiates a forced warp to the desired starting room
        /// </summary>
        public class WatcherRandomizedSpawn : UpdatableAndDeletable
        {
            public static bool warpPending = false;

            public WatcherRandomizedSpawn(Room room)
            {
                this.room = room;
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                // Wait until player takes a couple steps after the intro
                if (room.PlayersInRoom.FirstOrDefault() is not Player player
                    || player.firstChunk.pos.x < 1200f) return;
                
                room.game.GetStorySession.saveState.deathPersistentSaveData.rippleLevel = 1f;
                player.StartPendingForcedWarp(Plugin.RandoManager.customStartDen, default, 400);
                player.pendingForcedWarpPos = null; // null position will make it try to select dynamic warp destination positon in room
                warpPending = true;
                Destroy();
            }
        }
    }
}
