using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer
{
    public static class TrapsHandler
    {
        public class Trap
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
                Plugin.Log.LogInfo($"Trap Triggered! ({id})");
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText($"Trap Triggered! ({id})",
                    ArchipelagoConnection.palette[Archipelago.MultiClient.Net.Colors.PaletteColor.Red]));
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

        public readonly struct TrapDefinition(Action<RainWorldGame> TriggerAction, Action<RainWorldGame> DisableAction = null,
            Action<RainWorldGame> UpdateAction = null, Action<RainWorldGame> NewRoomAction = null, int duration = 0)
        {
            /// <summary>
            /// How many game ticks (40/s) the effect should last. 0 for single activation traps
            /// </summary>
            public readonly int duration = duration;
            /// <summary>
            /// Action called when the trap first triggers
            /// </summary>
            public readonly Action<RainWorldGame> onTrigger = TriggerAction;
            /// <summary>
            /// Action called every update while active
            /// </summary>
            public readonly Action<RainWorldGame> onUpdate = UpdateAction ?? ((game) => { });
            /// <summary>
            /// Action called when the duration expires. Trap is deleted right after
            /// </summary>
            public readonly Action<RainWorldGame> onDeactivate = DisableAction ?? ((game) => { });
            /// <summary>
            /// Action called when a player enters a new room. Useful for room-only effects that need re-triggering
            /// </summary>
            public readonly Action<RainWorldGame> onNewRoom = NewRoomAction ?? ((game) => { });
        }

        private static readonly Dictionary<string, TrapDefinition> trapDefinitions = new()
        {
            { "Stun", new TrapDefinition(TrapStun) },
            { "Timer", new TrapDefinition(game => { TrapCycleTimer(game); }) },
            { "Zoomies", new TrapDefinition(TrapZoomiesPlayer, TrapDisableZoomies, null, TrapZoomiesPlayer, 1200) },
            //{ "Flood", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            { "Rain", new TrapDefinition(TrapRainActivate, TrapRainDeactivate, null, null, 600) },
            { "Gravity", new TrapDefinition(TrapGravityActivate, TrapGravityDeactivate, null, TrapGravityActivate, 1200) },
            //{ "Fog", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            //{ "KillSquad", game => { throw new NotImplementedException("Trap not implemented yet"); } },
            { "Alarm", new TrapDefinition(TrapAlarm) },

            { "RedLizard", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.RedLizard); }) },
            { "RedCentipede", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.RedCentipede); }) },
            { "SpitterSpider", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.SpitterSpider, 4); }) },
            { "BrotherLongLegs", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.BrotherLongLegs, 2); }) },
            { "DaddyLongLegs", new TrapDefinition(game => { TrapSpawnCreatureNearby(game, CreatureTemplate.Type.DaddyLongLegs); }) },
        };

        private static int currentCooldown = 0;
        /// <summary>
        /// Traps with a duration that are currently active
        /// </summary>
        private static HashSet<Trap> activeTraps = [];
        private static Action<RainWorldGame> TrapUpdate = (game) => { };

        public static void ApplyHooks()
        {
            On.RainWorldGame.Update += OnRainWorldGameUpdate;
            On.Player.NewRoom += OnPlayerNewRoom;

            try
            {
                IL.GlobalRain.Update += ILGlobalRainUpdate;

                _ = new ILHook(typeof(RainCycle).GetProperty(nameof(RainCycle.preCycleRain_Intensity)).GetGetMethod(), ILGetPreCycleRainIntensity);

                _ = new ILHook(typeof(RainCycle).GetProperty(nameof(RainCycle.ScreenShake)).GetGetMethod(), ILPreTimer);
                _ = new ILHook(typeof(RainCycle).GetProperty(nameof(RainCycle.MicroScreenShake)).GetGetMethod(), ILPreTimer);

                _ = new ILHook(typeof(ElectricDeath).GetProperty(nameof(ElectricDeath.Intensity)).GetGetMethod(), ILElectricDeathIntensity);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.RainWorldGame.Update -= OnRainWorldGameUpdate;
            On.Player.NewRoom -= OnPlayerNewRoom;

            IL.GlobalRain.Update -= ILGlobalRainUpdate;
        }

        public static void EnqueueTrap(string itemId)
        {
            Plugin.RandoManager?.pendingTrapQueue.Enqueue(new Trap(itemId[5..]));
        }

        private static void ResetCooldown()
        {
            // Set the new countdown randomly in desired range * the default frame rate
            currentCooldown = UnityEngine.Random.Range(RandoOptions.trapMinimumCooldown.Value, RandoOptions.trapMaximumCooldown.Value) * 40;
        }

        public static void OnRainWorldGameUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.GamePaused) return;

            // Decrement countdown every tick
            if (currentCooldown > 0)
            {
                currentCooldown--;
            }

            TrapUpdate(self);

            if ((Plugin.RandoManager?.pendingTrapQueue.Count ?? 0) == 0) return;

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
                Plugin.RandoManager.pendingTrapQueue.Dequeue().Activate(self);

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
        /// <param name="count">How many of the creature to spawn</param>
        private static void TrapSpawnCreatureNearby(this RainWorldGame game, CreatureTemplate.Type template, int count = 1)
        {
            Player player = game.FirstAlivePlayer?.realizedCreature as Player;

            int[] connectedRooms = player.room.abstractRoom.connections;

            for (int i = 0; i < count; i++)
            {
                AbstractRoom chosenRoom = game.world.GetAbstractRoom(connectedRooms[UnityEngine.Random.Range(0, connectedRooms.Length)]);

                if (chosenRoom == null)
                {
                    Plugin.Log.LogError("Trap failed to find a valid room to spawn creature in");
                    continue;
                }

                AbstractCreature crit = new(game.world, StaticWorld.GetCreatureTemplate(template), null, chosenRoom.RandomNodeInRoom(), game.GetNewID());

                chosenRoom.AddEntity(crit);

                if (chosenRoom.realizedRoom != null)
                {
                    crit.RealizeInRoom();
                    crit.abstractAI.RealAI.tracker.SeeCreature(player.abstractCreature);
                }
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

        /// <summary>
        /// All rooms currently affected by gravity trap. Rooms may be null if player has unloaded them
        /// </summary>
        private static List<WeakReference<Room>> gravityTrappedRooms = [];

        /// <summary>Sets the gravity to 0 (Does not apply to rooms with <see cref="AntiGravity"/> or other gravity effects)</summary>
        private static void TrapGravityActivate(this RainWorldGame game)
        {
            Room currentRoom = (game.FirstAlivePlayer?.realizedCreature as Player).room;

            // Track each room this effect is applied to for cleaning up later
            gravityTrappedRooms.Add(new WeakReference<Room>(currentRoom));

            // Most gravity effects in the game apply through update loops,
            // so this will be overidden instantly in those cases
            currentRoom.gravity = 0f;
        }

        /// <summary>Turns gravity back on for affected rooms</summary>
        private static void TrapGravityDeactivate(this RainWorldGame game)
        {
            foreach (WeakReference<Room> roomRef in gravityTrappedRooms)
            {
                // Abstracted rooms will already have gravity reset when realized again
                if (roomRef.TryGetTarget(out Room room)) room.gravity = 1f;
            }
            gravityTrappedRooms.Clear();
        }

        private static bool rainTrapActive = false;

        /// <summary>Triggers precycle rain effect</summary>
        private static void TrapRainActivate(this RainWorldGame game)
        {
            rainTrapActive = true;
            // TODO: Couldn't figure out how flooding works to force it to always happen. May return to this later
            //game.globalRain.drainWorldFlood = 10f;
            //Plugin.Log.LogDebug(game.globalRain.flood);
            //Plugin.Log.LogDebug(game.globalRain.drainWorldFlood);
        }
        /// <summary>Returns rain trap to normal</summary>
        private static void TrapRainDeactivate(this RainWorldGame game)
        {
            rainTrapActive = false;
            //Plugin.Log.LogDebug(game.globalRain.flood);
            //Plugin.Log.LogDebug(game.globalRain.drainWorldFlood);
            //Room room = (game.FirstAlivePlayer?.realizedCreature as Player).room;
            //Plugin.Log.LogDebug(room.waterObject.originalWaterLevel);
            //Plugin.Log.LogDebug(room.roomRain.FloodLevel);
        }

        private static void ILGlobalRainUpdate(ILContext il)
        {
            ILCursor c = new(il);

            // --- Redirect if statement

            // After beq at 053F
            c.GotoNext(x => x.MatchLdfld(typeof(GlobalRain).GetField(nameof(GlobalRain.preCycleRainPulse_Scale))));
            c.GotoNext(MoveType.After, x => x.MatchBeq(out _));
            // Mark entry to if block
            ILLabel jump = c.MarkLabel();

            // Before checking precycle module at 0534
            c.GotoPrev(x => x.MatchCallOrCallvirt(typeof(ModManager).GetProperty(nameof(ModManager.PrecycleModule)).GetGetMethod()));
            c.MoveAfterLabels();

            // If rain trap is active, act as if it's a precycle
            c.EmitDelegate(ActivateRainTrap);
            c.Emit(OpCodes.Brtrue, jump);

            static bool ActivateRainTrap() => rainTrapActive;

            // --- Modify values

            // Load pulse intensity at 055F
            c.GotoNext(MoveType.After, x => x.MatchLdfld(typeof(GlobalRain).GetField(nameof(GlobalRain.preCycleRainPulse_Intensity))));
            // Load pulse scale at 0575
            c.GotoNext(MoveType.After, x => x.MatchLdfld(typeof(GlobalRain).GetField(nameof(GlobalRain.preCycleRainPulse_Scale))));
            // Supply new value for scale without overriding precyles
            c.EmitDelegate(AddRainTrapPulseScale);

            static float AddRainTrapPulseScale(float oldScale) => oldScale > 0 ? oldScale : 1f;
        }

        /// <summary>
        /// Set precycle rain intensity when trap is active
        /// </summary>
        private static void ILGetPreCycleRainIntensity(ILContext il)
        {
            ILCursor c = new(il);

            // First return at 001C
            c.GotoNext(x => x.MatchRet());

            // If precycle is not active but trap is, set our intensity
            c.EmitDelegate<Func<float, float>>(intensity =>
            {
                return rainTrapActive ? 1f : 0f;
            });
        }

        /// <summary>
        /// Pretend that preTimer is not 0 for certain checks
        /// </summary>
        /// <param name="il"></param>
        private static void ILPreTimer(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After, x => x.MatchLdfld(typeof(RainCycle).GetField(nameof(RainCycle.preTimer))));
            c.EmitDelegate<Func<int, int>>(preTimer =>
            {
                if (preTimer > 0f) return preTimer;
                return rainTrapActive ? 1 : 0;
            });
        }

        /// <summary>
        /// Make ElectricDeath consider rain trap
        /// </summary>
        private static void ILElectricDeathIntensity(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After, x => x.MatchLdfld(typeof(RainCycle).GetField(nameof(RainCycle.preTimer))));
            c.EmitDelegate<Func<int, int>>(preTimer =>
            {
                if (preTimer > 0f) return preTimer;
                return rainTrapActive ? 1 : 0;
            });

            c.GotoNext(MoveType.After, x => x.MatchLdfld(typeof(RainCycle).GetField(nameof(RainCycle.maxPreTimer))));
            c.EmitDelegate<Func<int, int>>(maxPreTimer =>
            {
                if (maxPreTimer > 0f) return maxPreTimer;
                return rainTrapActive ? 1 : 0;
            });
        }

        #endregion
    }
}
