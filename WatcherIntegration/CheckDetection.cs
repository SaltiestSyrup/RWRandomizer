using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Watcher;
using WPOT = Watcher.WatcherEnums.PlacedObjectType;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class CheckDetection
    {
        const float WARP_DETECTION_RADIUS = 200f;

        internal static class Hooks
        {
            internal static void ApplyHooks()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered += DetectSpinningTop;
                On.Watcher.WarpPoint.Update += WarpPoint_Update;
                On.Watcher.SpinningTop.CanRaiseRippleLevel += Dont;
                //IL.Room.Loaded += SpinningTopKeyCheck;
                On.WinState.TrackerAllowedOnSlugcat += LetThemWander;
                On.SlugcatStats.SlugcatStoryRegions += WatcherStoryRegions;
                On.Watcher.WarpSpawningRipple.Success += DetectThroneWarpCreation;
                On.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.PromptSpecialWarp += OnElderSpawn_PromptSpecialWarp;

                IL.Watcher.WatcherRoomSpecificScript.WORA_KarmaSigils.Update += DetectPrince;
                IL.Watcher.SpinningTop.SpawnWarpPoint += SpinningTop_SpawnWarpPoint;
                //IL.World.SpawnGhost += NullifyPresence;
            }

            internal static void RemoveHooks()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered -= DetectSpinningTop;
                On.Watcher.WarpPoint.Update -= WarpPoint_Update;
                On.Watcher.SpinningTop.CanRaiseRippleLevel -= Dont;
                //IL.Room.Loaded -= SpinningTopKeyCheck;
                On.WinState.TrackerAllowedOnSlugcat -= LetThemWander;
                On.SlugcatStats.SlugcatStoryRegions -= WatcherStoryRegions;
                On.Watcher.WarpSpawningRipple.Success -= DetectThroneWarpCreation;
                On.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.PromptSpecialWarp -= OnElderSpawn_PromptSpecialWarp;

                IL.Watcher.WatcherRoomSpecificScript.WORA_KarmaSigils.Update -= DetectPrince;
                IL.Watcher.SpinningTop.SpawnWarpPoint -= SpinningTop_SpawnWarpPoint;
                //IL.World.SpawnGhost -= NullifyPresence;
            }

            /// <summary>Detect when a new Throne room opens up after a Prince encounter.</summary>
            private static void DetectPrince(ILContext il)
            {
                ILCursor c = new(il);

                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(HUD.HUD).GetMethod(nameof(HUD.HUD.ResetMap))));  // 042c
                c.MoveBeforeLabels();  // keep cursor inside the conditional
                c.Emit(OpCodes.Ldarg_0);  // WORA_KarmaSigils this
                static void Delegate(WatcherRoomSpecificScript.WORA_KarmaSigils self)
                {
                    if (self.room.game.GetStorySession?.saveState.miscWorldSaveData.numberOfPrinceEncounters is int nope)
                        EntryPoint.TryGiveLocation($"Prince-{nope + 1}");
                }
                c.EmitDelegate(Delegate);
            }

            /// <summary>Detect when a Throne dynamic warp is successfully created.</summary>
            private static void DetectThroneWarpCreation(On.Watcher.WarpSpawningRipple.orig_Success orig, WarpSpawningRipple self, float duration, bool bad, bool weird, bool strong)
            {
                orig(self, duration, bad, weird, strong);
                if (DynamicWarpTargetting.GetWarpSourceKind(self.room.abstractRoom.name) == DynamicWarpTargetting.WarpSourceKind.Throne)
                {
                    EntryPoint.TryGiveLocation($"ThroneWarp-{self.room.abstractRoom.name.Substring(11)}");
                }
            }

            internal static List<string> watcherStoryRegions =
            [
                "WARA", "WARB", "WARC", "WARD", "WARE", "WARF", "WARG", "WAUA", "WBLA",
                "WDSR", "WGWR", "WHIR", "WORA", "WPTA", "WRFA", "WRFB", "WRRA", "WRSA",
                "WSKA", "WSKB", "WSKC", "WSKD", "WSSR", "WSUR", "WTDA", "WTDB", "WVWA"
            ];

            /// <summary>Return a relevant list of regions for Watcher.</summary>
            private static List<string> WatcherStoryRegions(On.SlugcatStats.orig_SlugcatStoryRegions orig, SlugcatStats.Name i)
                => i.value == "Watcher" ? watcherStoryRegions : orig(i);

            /// <summary>Don't blacklist The Wanderer for Watcher.</summary>
            private static bool LetThemWander(On.WinState.orig_TrackerAllowedOnSlugcat orig, WinState.EndgameID trackerId, SlugcatStats.Name slugcat)
                => (ModManager.Watcher && slugcat.value == "Watcher" && trackerId == WinState.EndgameID.Traveller) || orig(trackerId, slugcat);

            /// <summary>Prevent Ripple from being raised automatically.
            /// This also prevents the Ripple ladder from appearing when <see cref="SpinningTop.SpawnWarpPoint"/> is called.</summary>
            private static bool Dont(On.Watcher.SpinningTop.orig_CanRaiseRippleLevel orig, SpinningTop self) => false;

            /// <summary>
            /// Prevent the warp that Spinning Top makes from instantly triggering
            /// </summary>
            private static void SpinningTop_SpawnWarpPoint(ILContext il)
            {
                ILCursor c = new(il);

                c.GotoNext(MoveType.Before, x => x.MatchStfld(typeof(WarpPoint).GetField(nameof(WarpPoint.guaranteeTrigger))));

                c.EmitDelegate(PreventInstantPull);

                static bool PreventInstantPull(bool value) => false;
            }

            /// <summary>Detect the moment that a Spinning Top is marked as encountered.</summary>
            internal static void DetectSpinningTop(On.Watcher.SpinningTop.orig_MarkSpinningTopEncountered orig, SpinningTop self)
            {
                orig(self);
                EntryPoint.TryGiveLocation($"SpinningTop-{self.room.abstractRoom.name.Region()}");
            }

            /// <summary>Detect, at cycle end, what regions have been infected.</summary>
            internal static void DetectFixedWarpPointAndRotSpread(SaveState saveState)
            {
                for (int i = 1; i <= saveState.miscWorldSaveData.regionsInfectedBySentientRotSpread.Count; i++)
                    EntryPoint.TryGiveLocation($"SpreadRot-{i}");
            }

            /// <summary>
            /// Award warp discovery check when player is near warp
            /// </summary>
            private static void WarpPoint_Update(On.Watcher.WarpPoint.orig_Update orig, WarpPoint self, bool eu)
            {
                orig(self, eu);

                foreach (var crit in self.room?.game?.Players)
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
                if (Plugin.RandoManager is null)
                {
                    orig(self, player);
                    return;
                }

                EntryPoint.TryGiveLocation("Meet_Ripple_Elder");
                self.Destroy();
            }
        }
    }
}
