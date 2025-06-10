using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using WPOT = Watcher.WatcherEnums.PlacedObjectType;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class CheckDetection
    {
        internal static class Hooks
        {
            internal static void ApplyHooks()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered += DetectSpinningTop;
                On.Watcher.SpinningTop.CanRaiseRippleLevel += Dont;
                IL.Room.Loaded += SpinningTopKeyCheck;
                On.WinState.TrackerAllowedOnSlugcat += LetThemWander;
                On.SlugcatStats.SlugcatStoryRegions += WatcherStoryRegions;
                On.Watcher.WarpSpawningRipple.Success += DetectThroneWarpCreation;
                IL.Watcher.WatcherRoomSpecificScript.WORA_KarmaSigils.Update += DetectPrince;
                IL.World.SpawnGhost += NullifyPresence;
            }

            internal static void RemoveHooks()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered -= DetectSpinningTop;
                On.Watcher.SpinningTop.CanRaiseRippleLevel -= Dont;
                IL.Room.Loaded -= SpinningTopKeyCheck;
                On.WinState.TrackerAllowedOnSlugcat -= LetThemWander;
                On.SlugcatStats.SlugcatStoryRegions -= WatcherStoryRegions;
                On.Watcher.WarpSpawningRipple.Success -= DetectThroneWarpCreation;
                IL.Watcher.WatcherRoomSpecificScript.WORA_KarmaSigils.Update -= DetectPrince;
                IL.World.SpawnGhost -= NullifyPresence;
            }

            /// <summary>Prevent a <see cref="GhostWorldPresence"/> and <see cref="GhostCreatureSedater"/> (later, in <see cref="Room.Loaded"/>) from being created for a Spinning Top that won't be spawned (due to <see cref="SpinningTopKeyCheck(ILContext)"/>).</summary>
            private static void NullifyPresence(ILContext il)
            {
                ILCursor c = new(il);
                // Branch interception at 006a.
                c.GotoNext(x => x.MatchStloc(3));  // 0044
                c.GotoNext(MoveType.Before, x => x.MatchStloc(3));  // 0094

                static List<string> Delegate(List<string> orig)
                    => [.. orig.Where(x => Items.StaticKey.FromSpinningTop(x.Split(':')[0])?.Missing != true)];

                c.EmitDelegate(Delegate);
            }

            /// <summary>Detect when a new Throne room opens up after a Prince encounter.</summary>
            private static void DetectPrince(ILContext il)
            {
                ILCursor c = new(il);

                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(HUD.HUD).GetMethod(nameof(HUD.HUD.ResetMap))));  // 042c
                c.MoveBeforeLabels();  // keep cursor inside the conditional
                c.Emit(OpCodes.Ldarg_0);  // WORA_KarmaSigils this
                static void Delegate(Watcher.WatcherRoomSpecificScript.WORA_KarmaSigils self)
                {
                    if (self.room.game.GetStorySession?.saveState.miscWorldSaveData.numberOfPrinceEncounters is int nope)
                        EntryPoint.TryGiveLocation($"Prince-{nope + 1}");
                }
                c.EmitDelegate(Delegate);
            }

            /// <summary>Detect when a Throne dynamic warp is successfully created.</summary>
            private static void DetectThroneWarpCreation(On.Watcher.WarpSpawningRipple.orig_Success orig, Watcher.WarpSpawningRipple self, float duration, bool bad, bool weird, bool strong)
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
            /// This also prevents the Ripple ladder from appearing when <see cref="Watcher.SpinningTop.SpawnWarpPoint"/> is called.</summary>
            private static bool Dont(On.Watcher.SpinningTop.orig_CanRaiseRippleLevel orig, Watcher.SpinningTop self) => false;

            /// <summary>Detect the moment that a Spinning Top is marked as encountered.</summary>
            internal static void DetectSpinningTop(On.Watcher.SpinningTop.orig_MarkSpinningTopEncountered orig, Watcher.SpinningTop self)
            {
                orig(self);
                string loc = $"SpinningTop-{self.room.abstractRoom.name.Region()}";
                if (Plugin.RandoManager.IsLocationGiven(loc) == false) Plugin.RandoManager.GiveLocation(loc);
            }

            /// <summary>Detect, at cycle end, what new fixed warp points have been discovered and what regions have been infected.</summary>
            internal static void DetectFixedWarpPointAndRotSpread(SaveState saveState)
            {
                foreach (var point in saveState.deathPersistentSaveData.newlyDiscoveredWarpPoints)
                    EntryPoint.TryGiveLocation($"Warp-{point.Key.Split(':')[0].ToUpperInvariant()}");

                foreach (string region in saveState.miscWorldSaveData.regionsInfectedBySentientRot)
                    if (!Region.HasSentientRotResistance(region))
                        EntryPoint.TryGiveLocation($"SpreadRot-{region.ToUpperInvariant()}");
            }
            /// <summary>Prevent Spinning Top from spawning if the key is not collected and <see cref="Settings.spinningTopKeys"/> is enabled.</summary>
            private static void SpinningTopKeyCheck(ILContext il)
            {
                ILCursor c = new(il);
                // Branch interception at 1d71 (roomSettings.placedObjects[num10].type == WatcherEnums.PlacedObjectType.SpinningTopSpot).
                c.GotoNext(x => x.MatchLdsfld(typeof(WPOT).GetField(nameof(WPOT.SpinningTopSpot))));  // 1d67
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ExtEnum<PlacedObject.Type>).GetMethod("op_Equality")));  // 1d6c

                c.Emit(OpCodes.Ldarg_0);  // Room this
                c.Emit(OpCodes.Ldloc, 27);  // int num10 (iteration variable)
                static bool Delegate(bool orig, Room self, int index)
                {
                    if (!orig || !Settings.spinningTopKeys) return orig;
                    string dest = (self.roomSettings.placedObjects[index].data as SpinningTopData).destRegion;
                    return !Items.StaticKey.IsMissing(self.world.name, dest);
                }
                c.EmitDelegate(Delegate);
            }
        }
    }
}
