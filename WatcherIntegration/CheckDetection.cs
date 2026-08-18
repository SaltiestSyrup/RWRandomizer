using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Watcher;

namespace RainWorldRandomizer.WatcherIntegration
{
    public static class CheckDetection
    {
        const float WARP_DETECTION_RADIUS = 200f;

        public static class Hooks
        {
            public static void ApplyHooks()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered += DetectSpinningTop;
                On.Watcher.WarpPoint.Update += WarpPoint_Update;
                On.Watcher.SpinningTop.CanRaiseRippleLevel += Dont;
                //IL.Room.Loaded += SpinningTopKeyCheck;
                On.WinState.TrackerAllowedOnSlugcat += LetThemWander;
                On.SlugcatStats.SlugcatStoryRegions += WatcherStoryRegions;
                On.Watcher.WarpSpawningRipple.Success += DetectThroneWarpCreation;
                On.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.PromptSpecialWarp += OnElderSpawn_PromptSpecialWarp;
                On.Watcher.VoidWeaver.DeactivateWeaver += VoidWeaverOnDeactivateWeaver;

                IL.Watcher.WatcherRoomSpecificScript.WORA_KarmaSigils.Update += DetectPrince;
                IL.Watcher.SpinningTop.SpawnWarpPoint += SpinningTop_SpawnWarpPoint;
                //IL.World.SpawnGhost += NullifyPresence;
            }
            
            public static void RemoveHooks()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered -= DetectSpinningTop;
                On.Watcher.WarpPoint.Update -= WarpPoint_Update;
                On.Watcher.SpinningTop.CanRaiseRippleLevel -= Dont;
                //IL.Room.Loaded -= SpinningTopKeyCheck;
                On.WinState.TrackerAllowedOnSlugcat -= LetThemWander;
                On.SlugcatStats.SlugcatStoryRegions -= WatcherStoryRegions;
                On.Watcher.WarpSpawningRipple.Success -= DetectThroneWarpCreation;
                On.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.PromptSpecialWarp -= OnElderSpawn_PromptSpecialWarp;
                On.Watcher.VoidWeaver.DeactivateWeaver -= VoidWeaverOnDeactivateWeaver;

                IL.Watcher.WatcherRoomSpecificScript.WORA_KarmaSigils.Update -= DetectPrince;
                IL.Watcher.SpinningTop.SpawnWarpPoint -= SpinningTop_SpawnWarpPoint;
                //IL.World.SpawnGhost -= NullifyPresence;
            }
            
            private static void VoidWeaverOnDeactivateWeaver(On.Watcher.VoidWeaver.orig_DeactivateWeaver orig, VoidWeaver self)
            {
                if (!Plugin.RandomizerActive)
                {
                    orig(self);
                    return;
                }
                
                if (!self.room.game.GetStorySession?.voidWeaverEncountersThisCycle
                        .Contains(self.room.abstractRoom.name) ?? false)
                {
                    // Stops the orig call from incrementing the encounters
                    self.room.game.GetStorySession.voidWeaverEncountersThisCycle.Add(self.room.abstractRoom.name);
                    
                    // Directly check the integers flag because we've hijacked the helper property
                    int encounters = ++self.room.game.GetStorySession.saveState.miscWorldSaveData.integersWatcher[4];
                    
                    for (int i = 0; i < encounters; i++)
                        Plugin.RandoManager.GiveLocation($"Weaver-{i + 1}");
                }
                
                orig(self);
            }

            /// <summary>Detect when a new Throne room opens up after a Prince encounter.</summary>
            private static void DetectPrince(ILContext il)
            {
                ILCursor c = new(il);

                // After setting unlockedHallConnection to true
                c.GotoNext(MoveType.After,
                    x => x.MatchLdcI4(1),
                    x => x.MatchStfld(typeof(WatcherRoomSpecificScript.WORA_KarmaSigils)
                        .GetField(nameof(WatcherRoomSpecificScript.WORA_KarmaSigils.unlockedHallConnection), 
                            BindingFlags.Instance | BindingFlags.NonPublic))
                    );
                c.Emit(OpCodes.Ldarg_0);  // WORA_KarmaSigils this
                c.EmitDelegate(Delegate);
                return;
                
                static void Delegate(WatcherRoomSpecificScript.WORA_KarmaSigils self)
                {
                    if (self.room.game.GetStorySession?.saveState.miscWorldSaveData.numberOfPrinceEncounters is not int encounters)
                        return;
                    
                    // Try to give all previous encounters for safety
                    for (int i = 0; i <= encounters; i++)
                        Plugin.RandoManager?.GiveLocation($"Prince-{i + 1}");
                }
            }

            /// <summary>Detect when a Throne dynamic warp is successfully created.</summary>
            private static void DetectThroneWarpCreation(On.Watcher.WarpSpawningRipple.orig_Success orig, WarpSpawningRipple self, float duration, bool bad, bool weird, bool strong)
            {
                orig(self, duration, bad, weird, strong);
                if (Plugin.RandomizerActive
                    && DynamicWarpTargeting.GetWarpSourceKind(self.room.abstractRoom.name) == DynamicWarpTargeting.WarpSourceKind.Throne)
                {
                    Plugin.RandoManager.GiveLocation($"ThroneWarp-{self.room.abstractRoom.name.Substring(11)}");
                }
            }

            private static List<string> _watcherStoryRegions =
            [
                "WARA", "WARB", "WARC", "WARD", "WARE", "WARF", "WARG", "WAUA", "WBLA",
                "WDSR", "WGWR", "WHIR", "WORA", "WPTA", "WRFA", "WRFB", "WRRA", "WRSA",
                "WSKA", "WSKB", "WSKC", "WSKD", "WSSR", "WSUR", "WTDA", "WTDB", "WVWA"
            ];

            /// <summary>Return a relevant list of regions for Watcher.</summary>
            private static List<string> WatcherStoryRegions(On.SlugcatStats.orig_SlugcatStoryRegions orig, SlugcatStats.Name i)
                => Plugin.RandomizerActive && i.value == "Watcher" ? _watcherStoryRegions : orig(i);

            /// <summary>Don't blacklist The Wanderer for Watcher.</summary>
            private static bool LetThemWander(On.WinState.orig_TrackerAllowedOnSlugcat orig, WinState.EndgameID trackerId, SlugcatStats.Name slugcat)
                => (Plugin.RandomizerActive && ModManager.Watcher && slugcat.value == "Watcher" && trackerId == WinState.EndgameID.Traveller) 
                   || orig(trackerId, slugcat);

            /// <summary>Prevent Ripple from being raised automatically.
            /// This also prevents the Ripple ladder from appearing when <see cref="SpinningTop.SpawnWarpPoint"/> is called.</summary>
            private static bool Dont(On.Watcher.SpinningTop.orig_CanRaiseRippleLevel orig, SpinningTop self) => !Plugin.RandomizerActive;

            /// <summary>
            /// Prevent the warp that Spinning Top makes from instantly triggering
            /// </summary>
            private static void SpinningTop_SpawnWarpPoint(ILContext il)
            {
                ILCursor c = new(il);

                c.GotoNext(MoveType.Before, x => x.MatchStfld(typeof(WarpPoint).GetField(nameof(WarpPoint.guaranteeTrigger))));

                c.EmitDelegate(PreventInstantPull);
                return;

                static bool PreventInstantPull(bool value) => !Plugin.RandomizerActive;
            }

            /// <summary>Detect the moment that a Spinning Top is marked as encountered.</summary>
            private static void DetectSpinningTop(On.Watcher.SpinningTop.orig_MarkSpinningTopEncountered orig, SpinningTop self)
            {
                orig(self);
                Plugin.RandoManager?.GiveLocation($"SpinningTop-{self.room.abstractRoom.name.Region()}");
            }

            /// <summary>Detect, at cycle end, what regions have been infected.</summary>
            internal static void DetectFixedWarpPointAndRotSpread(SaveState saveState)
            {
                for (int i = 1; i <= saveState.miscWorldSaveData.regionsInfectedBySentientRotSpread.Count; i++)
                    Plugin.RandoManager?.GiveLocation($"SpreadRot-{i}");
            }

            /// <summary>
            /// Award warp discovery check when player is near warp
            /// </summary>
            private static void WarpPoint_Update(On.Watcher.WarpPoint.orig_Update orig, WarpPoint self, bool eu)
            {
                orig(self, eu);
                if (!Plugin.RandomizerActive || self?.room?.game?.Players is null) return;

                foreach (var crit in self.room.game.Players)
                {
                    if (crit.Room.name == self.room.abstractRoom.name
                        && crit.realizedCreature is Creature player
                        && Vector2.Distance(self.pos, player.mainBodyChunk.pos) < WARP_DETECTION_RADIUS)
                    {
                        Plugin.RandoManager.GiveLocation($"Warp-{self.room.abstractRoom.name.ToUpperInvariant()}");
                    }
                }
            }

            /// <summary>
            /// Detect meeting Ripple Elder and cancel warp tutorial
            /// </summary>
            private static void OnElderSpawn_PromptSpecialWarp(On.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.orig_PromptSpecialWarp orig,
                WatcherRoomSpecificScript.WORA_ElderSpawn self, Player player)
            {
                if (!Plugin.RandomizerActive)
                {
                    orig(self, player);
                    return;
                }

                Plugin.RandoManager.GiveLocation("Meet_Ripple_Elder");
                self.Destroy();
            }
        }
    }
}
