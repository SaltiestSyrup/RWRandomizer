using System;
using System.Collections.Generic;
using System.Linq;

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
            public TrapDefinition definition;
            public int timer;

            public Trap(string id)
            {
                this.id = id;
                if (trapDefinitions.ContainsKey(id))
                {
                    definition = trapDefinitions[id];
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
                definition.onTrigger(game);
                timer = definition.duration;

                if (definition.duration > 0)
                {
                    TrapUpdate += Update;
                    activeTraps.Add(this);
                }
            }

            public void Update(RainWorldGame game)
            {
                definition.onUpdate(game);

                if (definition.duration <= 0f) return;

                if (timer > 0f) timer--;
                else Deactivate(game);
            }

            public void Deactivate(RainWorldGame game)
            {
                definition.onDeactivate(game);
                TrapUpdate -= Update;
                activeTraps.Remove(this);
            }
            public void NewRoom(RainWorldGame game) => definition.onNewRoom(game);
        }

        public readonly struct TrapDefinition
        {
            /// <summary>
            /// How many game ticks (40/s) the effect should last. 0 for single activation traps
            /// </summary>
            public readonly int duration;
            /// <summary>
            /// Action called when the trap first triggers
            /// </summary>
            public readonly Action<RainWorldGame> onTrigger;
            /// <summary>
            /// Action called every update while active
            /// </summary>
            public readonly Action<RainWorldGame> onUpdate;
            /// <summary>
            /// Action called when the duration expires. Trap is deleted right after
            /// </summary>
            public readonly Action<RainWorldGame> onDeactivate;
            /// <summary>
            /// Action called when a player enters a new room. Useful for room-only effects that need re-triggering
            /// </summary>
            public readonly Action<RainWorldGame> onNewRoom;

            public TrapDefinition(Action<RainWorldGame> TriggerAction, Action<RainWorldGame> DisableAction = null,
                Action<RainWorldGame> UpdateAction = null, Action<RainWorldGame> NewRoomAction = null, int duration = 0)
            {
                onTrigger = TriggerAction;
                onDeactivate = DisableAction ?? ((game) => { });
                onUpdate = UpdateAction ?? ((game) => { });
                onNewRoom = NewRoomAction ?? ((game) => { });
                this.duration = duration;
            }
        }

        private static readonly Dictionary<string, TrapDefinition> trapDefinitions = new Dictionary<string, TrapDefinition>()
        {
            { "Stun", new TrapDefinition(TrapStun) },
            { "Timer", new TrapDefinition(game => { TrapCycleTimer(game); }) },
            { "Zoomies", new TrapDefinition(TrapZoomiesPlayer, TrapDisableZoomies, null, TrapZoomiesPlayer, 1200) },
            //{ "Flood", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            //{ "Rain", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            //{ "Gravity", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            //{ "Fog", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            //{ "KillSquad", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            { "Alarm", new TrapDefinition(TrapAlarm) },

            { "RedLizard", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.RedLizard); }) },
            { "RedCentipede", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.RedCentipede); }) },
            { "SpitterSpider", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.SpitterSpider); }) },
            { "BrotherLongLegs", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.BrotherLongLegs); }) },
            { "DaddyLongLegs", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.DaddyLongLegs); }) },
        };

        private static int currentCooldown = 0;
        // TODO: Save pending traps like we do items
        /// <summary>
        /// Traps waiting to be activated
        /// </summary>
        private static Queue<Trap> pendingTrapQueue = new Queue<Trap>();
        /// <summary>
        /// Traps with a duration that are currently active
        /// </summary>
        private static HashSet<Trap> activeTraps = new HashSet<Trap>();
        private static Action<RainWorldGame> TrapUpdate = (game) => { };

        public static void ApplyHooks()
        {
            On.RainWorldGame.Update += OnRainWorldGameUpdate;
            On.Player.NewRoom += OnPlayerNewRoom;
        }

        public static void RemoveHooks()
        {
            On.RainWorldGame.Update -= OnRainWorldGameUpdate;
            On.Player.NewRoom -= OnPlayerNewRoom;
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

            TrapUpdate(self);
            //foreach (Trap trap in activeTraps)
            //{
            //    trap.Update(self);
            //}

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

        public static void OnPlayerNewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);
            if (self == newRoom.game.FirstAlivePlayer?.realizedCreature)
            {
                foreach (Trap trap in activeTraps)
                {
                    trap.NewRoom(newRoom.game);
                }
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

        private static void TrapDisableZoomies(this RainWorldGame game)
        {
            Player player = game.FirstAlivePlayer?.realizedCreature as Player;
            int sumUpdates = player.room.updateList.Count((c) => c == player);

            // Safety check to ensure we never completely remove player from updateList
            if (sumUpdates > 1)
            {
                player.room.updateList.Remove(player);
            }
        }

        /// <summary>Spawns the desired creature in an adjacent room</summary>
        /// <param name="template">The type of creature to spawn</param>
        private static void TrapSpawnCreatureNearby(this RainWorldGame game, CreatureTemplate.Type template)
        {
            Player player = game.FirstAlivePlayer?.realizedCreature as Player;

            int[] connectedRooms = player.room.abstractRoom.connections;
            AbstractRoom chosenRoom = game.world.GetAbstractRoom(connectedRooms[UnityEngine.Random.Range(0, connectedRooms.Length)]);

            if (chosenRoom == null)
            {
                Plugin.Log.LogError("Trap failed to find a valid room to spawn creature in");
                return;
            }

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
                    creature.abstractAI?.RealAI?.tracker?.SeeCreature(player.abstractCreature);
                }
            }
        }
        #endregion
    }
}
