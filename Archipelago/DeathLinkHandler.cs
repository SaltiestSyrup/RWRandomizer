using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class DeathLinkHandler
    {
        private static DeathLinkService service = null;
        
        /// <summary>Cooldown to ensure we don't send a packet for a received death</summary>
        private static int receiveDeathCooldown = 0;
        /// <summary>When True, the mod is waiting for a proper state to kill the player</summary>
        private static bool deathPending = false;
        private static bool lastDeathWasMe = false;

        public static bool Active
        {
            get
            {
                return service != null && ArchipelagoConnection.Session.ConnectionInfo.Tags.Contains("DeathLink");
            }
            set
            {
                if (value) service?.EnableDeathLink();
                else service?.DisableDeathLink();
            }
        }

        public static void ApplyHooks()
        {
            On.Player.Die += OnPlayerDie;
            On.RainWorldGame.Update += OnRainWorldGameUpdate;

            try
            {
                IL.RainWorldGame.GoToDeathScreen += GoToDeathScreenIL;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.Player.Die -= OnPlayerDie;
            On.RainWorldGame.Update -= OnRainWorldGameUpdate;

            IL.RainWorldGame.GoToDeathScreen -= GoToDeathScreenIL;
        }

        public static void Init(ArchipelagoSession session)
        {
            service = session.CreateDeathLinkService();
            service.OnDeathLinkReceived += OnReceiveDeath;
        }

        private static void OnReceiveDeath(DeathLink deathLink)
        {
            Plugin.Log.LogInfo($"Received DeathLink packet from {deathLink.Source}");

            if (!Plugin.archipelagoIgnoreMenuDL.Value // Ignore menu DeathLinks if setting
                || Plugin.Singleton.rainWorld.processManager.currentMainLoop is RainWorldGame)
            {
                receiveDeathCooldown = 40; // 1 second
                deathPending = true;
            }
            else
            {
                Plugin.Log.LogInfo($"Ignoring DeathLink as main process is {Plugin.Singleton.rainWorld.processManager.currentMainLoop.GetType().Name}");
            }
        }

        private static void OnPlayerDie(On.Player.orig_Die orig, Player self)
        {
            orig(self);
            if (!Active || receiveDeathCooldown > 0 || self.AI != null) return;

            Plugin.Log.LogInfo("Sending DeathLink packet...");
            service.SendDeathLink(new DeathLink(ArchipelagoConnection.playerName));
        }

        private static void OnRainWorldGameUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.GamePaused || !self.processActive) return;

            if (deathPending 
                && self.FirstAlivePlayer?.realizedCreature?.room != null // Player exists
                && self.manager.fadeToBlack == 0 // The screen has fully faded in
                && (self.FirstAlivePlayer.realizedCreature as Player).controller == null) // There are no external forces controlling us
            {
                deathPending = false;
                lastDeathWasMe = true;
                foreach (AbstractCreature abstractPlayer in self.AlivePlayers)
                {
                    // Make sure player is realized
                    if (abstractPlayer.realizedCreature is Player player)
                    {
                        Plugin.Log.LogInfo("Deathlink Killing Player...");
                        // This is the same effect played when Pebbles kills the player
                        player.mainBodyChunk.vel += RWCustom.Custom.RNV() * 12f;
                        for (int k = 0; k < 20; k++)
                        {
                            player.room.AddObject(new Spark(player.mainBodyChunk.pos, RWCustom.Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        }
                        player.Die();
                    }
                }
            }

            // Cooldown Counter
            if (receiveDeathCooldown > 0) receiveDeathCooldown--;
            else if (deathPending) receiveDeathCooldown = 40;
        }

        // TODO: DeathLink deaths currently still display karma decreasing animation even when overwritten
        private static void GoToDeathScreenIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            try
            {
                c.GotoNext(
                    MoveType.Before,
                    x => x.MatchLdcI4(0),
                    x => x.MatchCallOrCallvirt(typeof(SaveState).GetMethod(nameof(SaveState.SessionEnded))),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(MainLoopProcess).GetField(nameof(MainLoopProcess.manager))),
                    x => x.MatchLdsfld(typeof(ProcessManager.ProcessID).GetField(nameof(ProcessManager.ProcessID.DeathScreen)))
                );

                c.Emit(OpCodes.Pop);
                // If setting and the last death was a DeathLink, tell the save state we survived actually
                c.EmitDelegate<Func<int>>(() =>
                {
                    bool preventDeath = Plugin.archipelagoPreventDLKarmaLoss.Value && lastDeathWasMe;
                    lastDeathWasMe = false;
                    return preventDeath ? 1 : 0;
                });
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed Hooking for GoToDeathScreen");
                Plugin.Log.LogError(e);
            }
        }
    }
}
