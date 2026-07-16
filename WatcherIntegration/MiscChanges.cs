using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace RainWorldRandomizer.WatcherIntegration
{
    public static class MiscChanges
    {
        public static void ApplyHooks()
        {
            try
            {
                IL.Watcher.WarpPoint.ActivateWeaver += WarpPointOnActivateWeaver;
                IL.Player.WatcherUpdate += FixDialWarpAbilityHardcode;
                IL.Watcher.WatcherRoomSpecificScript.AddRoomSpecificScript += FixDialWarpAbilityHardcode;
                IL.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.Update += FixDialWarpAbilityHardcode;
                IL.Watcher.WarpPoint.NewWorldLoaded_Room += WarpPointOnNewWorldLoaded_Room;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }
        
        public static void RemoveHooks()
        {
            IL.Watcher.WarpPoint.ActivateWeaver += WarpPointOnActivateWeaver;
            IL.Player.WatcherUpdate -= FixDialWarpAbilityHardcode;
            IL.Watcher.WatcherRoomSpecificScript.AddRoomSpecificScript -= FixDialWarpAbilityHardcode;
            IL.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.Update -= FixDialWarpAbilityHardcode;
            IL.Watcher.WarpPoint.NewWorldLoaded_Room += WarpPointOnNewWorldLoaded_Room;
        }
        
        /// <summary>
        /// Stop karma flowers from being removed by warp filters
        /// </summary>
        private static void WarpPointOnNewWorldLoaded_Room(ILContext il)
        {
            ILCursor c = new(il);

            // Right before fetching the field from the PlacedObject
            c.GotoNext(MoveType.Before,
                x => x.MatchLdfld(typeof(PlacedObject).GetField(nameof(PlacedObject.deactivatedByWarpFilter))));
            // We intercept and modify the object
            c.EmitDelegate(DontWarpFilterFlowers);
            
            // Do it again for the other check
            c.GotoNext(MoveType.Before,
                x => x.MatchLdfld(typeof(PlacedObject).GetField(nameof(PlacedObject.deactivatedByWarpFilter))));
            c.EmitDelegate(DontWarpFilterFlowers);

            return;
            
            static PlacedObject DontWarpFilterFlowers(PlacedObject obj)
            {
                if (!Plugin.RandomizerActive) return obj;

                if (obj.deactivatedByWarpFilter && obj.type == PlacedObject.Type.KarmaFlower)
                    obj.deactivatedByWarpFilter = false;
                return obj;
            }
        }
        
        /// <summary>
        /// Prevent Weaver from spawning if the player needs to complete the Sentient Rot ending / has Rot spread checks
        /// </summary>
        private static void WarpPointOnActivateWeaver(ILContext il)
        {
            ILCursor c = new(il);

            // Fetch label for after call to InitiateWeaverPresence
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(World).GetMethod(nameof(World.InitiateWeaverPresence))),
                x => x.MatchBrfalse(out _));

            ILLabel jump = c.MarkLabel();
            
            // Move back before InitiateWeaverPresence is called
            c.GotoPrev(x => x.MatchLdarg(0));
            c.GotoPrev(MoveType.AfterLabel, x => x.MatchLdarg(0));
            
            // Skip past Weaver spawn
            c.EmitDelegate(DontSkipWeaver);
            c.Emit(OpCodes.Brfalse, jump);

            // Skip music trigger
            // RainCycle.MusicAllowed check near end of method
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(RainCycle).GetProperty(nameof(RainCycle.MusicAllowed))!.GetGetMethod()));
            
            c.Emit(OpCodes.Pop);
            c.EmitDelegate(DontSkipWeaver);
            return;

            static bool DontSkipWeaver()
            {
                return !(Plugin.ArchipelagoActive
                       && (ArchipelagoConnection.completionCondition == ArchipelagoConnection.CompletionCondition.SentientRot
                           || ArchipelagoConnection.spreadRotChecks));
            }
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
