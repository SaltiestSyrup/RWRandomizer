using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Linq;
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
        private static bool lastDeathWasLink = false;

        public static bool Active
        {
            get
            {
                return service is not null && ArchipelagoConnection.Session.ConnectionInfo.Tags.Contains("DeathLink");
            }
            set
            {
                if (value) service?.EnableDeathLink();
                else service?.DisableDeathLink();
            }
        }

        public static void ApplyHooks()
        {
            On.RainWorldGame.GoToDeathScreen += OnPlayerDie;
            On.RainWorldGame.Update += OnRainWorldGameUpdate;

            try
            {
                IL.DeathPersistentSaveData.SaveToString += DeathPersistentSaveDataToStringIL;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void RemoveHooks()
        {
            On.RainWorldGame.GoToDeathScreen -= OnPlayerDie;
            On.RainWorldGame.Update -= OnRainWorldGameUpdate;

            IL.DeathPersistentSaveData.SaveToString -= DeathPersistentSaveDataToStringIL;
        }

        public static void Init(ArchipelagoSession session)
        {
            service = session.CreateDeathLinkService();
            service.OnDeathLinkReceived += OnReceiveDeath;
        }

        private static void Kill(Creature player)
        {
            // This is the same effect played when Pebbles kills the player
            player.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, player.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
            player.mainBodyChunk.vel += RWCustom.Custom.RNV() * 12f;
            for (int k = 0; k < 20; k++)
            {
                player.room.AddObject(new Spark(player.mainBodyChunk.pos, RWCustom.Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
            }
            player.Die();
        }

        private static void OnReceiveDeath(DeathLink deathLink)
        {
            Plugin.Log.LogInfo($"Received DeathLink packet from {deathLink.Source}");

            if (!RandoOptions.archipelagoIgnoreMenuDL.Value // Ignore menu DeathLinks if setting
                || Plugin.Singleton.rainWorld.processManager.currentMainLoop is RainWorldGame)
            {
                string deathMessage = deathLink.Cause ?? $"{deathLink.Source} has died!";
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText(deathMessage));
                Plugin.ServerLog.Log(deathMessage);
                receiveDeathCooldown = 40; // 1 second
                deathPending = true;
            }
            else
            {
                Plugin.Log.LogInfo($"Ignoring DeathLink as main process is {Plugin.Singleton.rainWorld.processManager.currentMainLoop.GetType().Name}");
            }
        }

        private static void OnPlayerDie(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
        {
            if (!Active
                || lastDeathWasLink
                || receiveDeathCooldown > 0
                || self.manager.upcomingProcess != null)
            {
                orig(self);
                return;
            }
            orig(self);

            Plugin.Log.LogInfo("Sending DeathLink packet...");
            service.SendDeathLink(new DeathLink(ArchipelagoConnection.playerName));
        }

        private static void OnRainWorldGameUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.GamePaused || !self.processActive) return;

            if (deathPending
                && self.FirstAlivePlayer?.realizedCreature is Player firstPlayer // Player exists
                && firstPlayer.room != null // Player is in a room
                && self.manager.fadeToBlack == 0 // The screen has fully faded in
                && firstPlayer.controller == null) // There are no external forces controlling us
            {
                deathPending = false;

                // Secret chance to kill a slugpup instead
                foreach (var creature in firstPlayer.room.abstractRoom.creatures)
                {
                    if (creature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC
                        && creature.state.alive
                        && UnityEngine.Random.value < 0.2f)
                    {
                        Kill(creature.realizedCreature);
                        return;
                    }
                }

                lastDeathWasLink = true;
                foreach (AbstractCreature abstractPlayer in self.AlivePlayers)
                {
                    // Make sure player is realized
                    if (abstractPlayer.realizedCreature is Player player)
                    {
                        Plugin.Log.LogInfo("Deathlink Killing Player...");
                        Kill(player);
                    }
                }
            }

            // Cooldown Counter
            if (receiveDeathCooldown > 0) receiveDeathCooldown--;
            else if (deathPending) receiveDeathCooldown = 40;
        }

        // TODO: DeathLink deaths currently still display karma decreasing animation even when overwritten
        private static void DeathPersistentSaveDataToStringIL(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdstr("KARMA<dpB>{0}<dpA>"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.karma))),
                x => x.MatchLdcI4(1)
                );

            c.EmitDelegate<Func<int, int>>((orig) =>
            {
                bool preventDeath = RandoOptions.archipelagoPreventDLKarmaLoss.Value && lastDeathWasLink;
                lastDeathWasLink = false;
                return preventDeath ? 0 : 1;
            });
        }
    }
}
