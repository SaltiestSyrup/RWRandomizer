using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public static class TrapsHandler
    {
        // TODO: Make these options driven
        /// <summary> Lowest possible trap cooldown in seconds </summary>
        public const int TRAP_COOLDOWN_LOW = 30;
        /// <summary> Highest possible trap cooldown in seconds </summary>
        public const int TRAP_COOLDOWN_HIGH = 90;

        private class Trap
        {
            public string id;
            public float duration;
            private readonly Action<RainWorldGame> onActivate = (game) => { };
            private readonly Action<RainWorldGame> onUpdate = (game) => { };
            private readonly Action<RainWorldGame> onDeactivate = (game) => { };

            public Trap(string id)
            {
                this.id = id;

                if (trapActions.ContainsKey(id))
                {
                    onActivate += trapActions[id];
                }
                else
                {
                    Plugin.Log.LogError($"Tried to create trap with invalid ID: {id}");
                }
            }

            public void Activate(RainWorldGame game)
            {
                Plugin.Log.LogDebug($"Trap Triggered! ({id})");
                Plugin.Singleton.notifQueue.Enqueue($"Trap Triggered! ({id})");
                onActivate(game);

                if (duration > 0f)
                {
                    // No duration traps yet
                }
            }
        }

        private static readonly Dictionary<string, Action<RainWorldGame>> trapActions = new Dictionary<string, Action<RainWorldGame>>()
        {
            { "Stun", TrapStun },
            { "Timer", game => { TrapCycleTimer(game); } },
            { "Zoomies", TrapZoomiesPlayer },
            { "Flood", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            { "Rain", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            { "Gravity", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            { "Fog", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            { "KillSquad", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            { "Alarm", TrapAlarm },

            { "RedLizard", game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.RedLizard); } },
            { "RedCentipede", game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.RedCentipede); } },
            { "SpitterSpider", game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.SpitterSpider); } },
            { "BrotherLongLegs", game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.BrotherLongLegs); } },
            { "DaddyLongLegs", game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.DaddyLongLegs); } },
        };

        private static int currentCooldown = 0;
        // TODO: Save pending traps like we do items
        private static Queue<Trap> pendingTrapQueue = new Queue<Trap>();

        public static void ApplyHooks()
        {
            On.RainWorldGame.Update += OnRainWorldGameUpdate;
        }

        public static void RemoveHooks()
        {
            On.RainWorldGame.Update -= OnRainWorldGameUpdate;
        }

        public static void EnqueueTrap(string itemId)
        {
            pendingTrapQueue.Enqueue(new Trap(itemId.Substring(5)));
            Plugin.Log.LogDebug($"Added trap to queue. Current traps count: {pendingTrapQueue.Count}");
        }

        private static void ResetCooldown()
        {
            // Set the new countdown randomly in desired range * the default frame rate
            currentCooldown = UnityEngine.Random.Range(TRAP_COOLDOWN_LOW, TRAP_COOLDOWN_HIGH) * 40;
        }

        public static void OnRainWorldGameUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.GamePaused) return;

            // Decrement countdown every frame
            if (currentCooldown > 0)
            {
                currentCooldown--;
            }

            if (pendingTrapQueue.Count == 0) return;
            
            if (currentCooldown == 0)
            {
                // Defer trap trigger until the player is in a proper state to suffer
                if (self.FirstAlivePlayer?.realizedCreature?.room == null
                    || self.FirstAlivePlayer.realizedCreature.room.abstractRoom.shelter)
                {
                    Plugin.Log.LogInfo("Deferring trap, player is not ready");
                    ResetCooldown();
                    return;
                }

                // Process the next trap in queue
                pendingTrapQueue.Dequeue().Activate(self);
                
                ResetCooldown();
            }
        }

        #region Trap Functions
        /// <summary>Stuns the player</summary>
        private static void TrapStun(this RainWorldGame game)
        {
            game.FirstAlivePlayer?.realizedCreature?.Stun(85);
        }

        /// <summary>Removes time from the current cycle</summary>
        /// <param name="seconds">How much time the player should lose</param>
        private static void TrapCycleTimer(this RainWorldGame game, int seconds = 120)
        {
            game.world.rainCycle.timer += seconds * 40;
        }

        /// <summary>Doubles the time scale of the player</summary>
        private static void TrapZoomiesPlayer(this RainWorldGame game)
        {
            Player player = game.FirstAlivePlayer?.realizedCreature as Player;
            player.room.updateList.Add(player);
        }

        /// <summary>Spawns the desired creature in an adjacent room</summary>
        /// <param name="template">The type of creature to spawn</param>
        private static void TrapSpawnCreatureNearby(this RainWorldGame game, CreatureTemplate.Type template)
        {
            Player player = game.FirstAlivePlayer?.realizedCreature as Player;
            
            int[] connectedRooms = player.room.abstractRoom.connections;
            AbstractRoom chosenRoom = game.world.GetAbstractRoom(connectedRooms[UnityEngine.Random.Range(0, connectedRooms.Length)]);

            AbstractCreature crit = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(template), null, chosenRoom.RandomNodeInRoom(), game.GetNewID());
            
            chosenRoom.AddEntity(crit);
            
            if (chosenRoom.realizedRoom != null)
            {
                crit.RealizeInRoom();
                crit.abstractAI.RealAI.tracker.SeeCreature(player.abstractCreature);
            }
        }

        /// <summary>Alerts every creature of the player's position</summary>
        private static void TrapAlarm(this RainWorldGame game)
        {
            Player player = game.FirstAlivePlayer?.realizedCreature as Player;
            
            // For each realized room
            foreach (AbstractRoom room in game.world.abstractRooms.Where(e => e.realizedRoom != null))
            {
                // For each realized creature in room
                foreach (AbstractCreature creature in room.creatures.Where(e => e.realizedCreature != null))
                {
                    creature.abstractAI.RealAI.tracker.SeeCreature(player.abstractCreature);
                }
            }
        }
        #endregion
    }
}
