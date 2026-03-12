using System.Linq;
using Watcher;

namespace RainWorldRandomizer.WatcherIntegration
{
    public class RoomSpecificScript
    {
        public static void AddRoomSpecificScript(Room room)
        {
            if (Plugin.RandoManager.currentSlugcat != WatcherEnums.SlugcatStatsName.Watcher) return;
            string roomName = room.abstractRoom.name;

            switch (roomName)
            {
                case "HI_W13":
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
                Player player = room.PlayersInRoom.FirstOrDefault();
                if (player is null) return;

                room.game.GetStorySession.saveState.deathPersistentSaveData.rippleLevel = 1f;
                player.StartPendingForcedWarp(Plugin.RandoManager.customStartDen, default, 400);
                player.pendingForcedWarpPos = null; // null position will make it try to select dynamic warp destination positon in room
                warpPending = true;
                Destroy();
            }
        }
    }
}
