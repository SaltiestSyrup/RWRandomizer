using System;
using MonoMod.Cil;

namespace RainWorldRandomizer.WatcherIntegration
{
    public static class MiscChanges
    {
        public static void ApplyHooks()
        {
            On.World.InitiateWeaverPresence += World_InitiateWeaverPresence;
            
            try
            {
                IL.Player.WatcherUpdate += FixDialWarpAbilityHardcode;
                IL.Watcher.WatcherRoomSpecificScript.AddRoomSpecificScript += FixDialWarpAbilityHardcode;
                IL.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.Update += FixDialWarpAbilityHardcode;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }
        
        public static void RemoveHooks()
        {
            On.World.InitiateWeaverPresence -= World_InitiateWeaverPresence;
            IL.Player.WatcherUpdate -= FixDialWarpAbilityHardcode;
            IL.Watcher.WatcherRoomSpecificScript.AddRoomSpecificScript -= FixDialWarpAbilityHardcode;
            IL.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.Update -= FixDialWarpAbilityHardcode;
        }

        /// <summary>
        /// Prevent Weaver from spawning if the player needs to complete the Sentient Rot ending
        /// </summary>
        private static bool World_InitiateWeaverPresence(On.World.orig_InitiateWeaverPresence orig, World self, AbstractRoom triggerRoom)
        {
            if (Plugin.RandoManager is ManagerArchipelago
                && ArchipelagoConnection.completionCondition == ArchipelagoConnection.CompletionCondition.SentientRot)
            {
                return false;
            }
            return orig(self, triggerRoom);
        }
        
        /// <summary>
        /// Replace the hardcoded "Does the player have dial warp" checks for egg visibility and elder spawning
        /// </summary>
        private static void FixDialWarpAbilityHardcode(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(MiscWorldSaveData)
                    .GetProperty(nameof(MiscWorldSaveData.hasRippleEggWarpAbility)).GetGetMethod()));
            c.EmitDelegate(HasRippleElderCheck);
            
            return;

            static bool HasRippleElderCheck(bool origVal)
            {
                // Meet Elder location is collected, or has dial warp if there is no location
                return Plugin.RandoManager?.IsLocationGiven("Meet_Ripple_Elder") ?? origVal;
            }
        }
    }
}
