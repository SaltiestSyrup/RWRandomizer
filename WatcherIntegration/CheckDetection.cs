using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using WPOT = Watcher.WatcherEnums.PlacedObjectType;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class CheckDetection
    {
        internal static class Hooks
        {
            internal static void Apply()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered += DetectSpinningTop;
                On.Watcher.SpinningTop.CanRaiseRippleLevel += Dont;
                IL.Room.Loaded += SpinningTopKeyCheck;
                On.WinState.TrackerAllowedOnSlugcat += LetThemWander;
                On.SlugcatStats.SlugcatStoryRegions += SlugcatStats_SlugcatStoryRegions;
                On.ProcessManager.RequestMainProcessSwitch_ProcessID += DetectCompletion;
            }

            internal static void Unapply()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered -= DetectSpinningTop;
                On.Watcher.SpinningTop.CanRaiseRippleLevel -= Dont;
                IL.Room.Loaded -= SpinningTopKeyCheck;
                On.WinState.TrackerAllowedOnSlugcat -= LetThemWander;
                On.SlugcatStats.SlugcatStoryRegions -= SlugcatStats_SlugcatStoryRegions;
                On.ProcessManager.RequestMainProcessSwitch_ProcessID -= DetectCompletion;
            }

            /// <summary>Detect completion conditions when switching to the ending slideshows.</summary>
            private static void DetectCompletion(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
            {
                if (ID == ProcessManager.ProcessID.SlideShow && Plugin.RandoManager is ManagerArchipelago managerAP && !managerAP.gameCompleted)
                {
                    if (self.nextSlideshow == Watcher.WatcherEnums.SlideShowID.EndingRot)
                        managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SentientRot);
                    else if (self.nextSlideshow == Watcher.WatcherEnums.SlideShowID.EndingSpinningTop)
                        managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SpinningTop);
                }
                orig(self, ID);
            }

            internal static List<string> watcherStoryRegions = new List<string> { 
                "WARA", "WARB", "WARC", "WARD", "WARE", "WARF", "WARG", "WAUA", "WBLA", 
                "WDSR", "WGWR", "WHIR", "WORA", "WPTA", "WRFA", "WRFB", "WRRA", "WRSA", 
                "WSKA", "WSKB", "WSKC", "WSKD", "WSSR", "WSUR", "WTDA", "WTDB", "WVWA" 
            };

            private static List<string> SlugcatStats_SlugcatStoryRegions(On.SlugcatStats.orig_SlugcatStoryRegions orig, SlugcatStats.Name i) 
                => i.value == "Watcher" ? watcherStoryRegions : orig(i);
            
            /// <summary>Don't blacklist The Wanderer for Watcher.</summary>
            private static bool LetThemWander(On.WinState.orig_TrackerAllowedOnSlugcat orig, WinState.EndgameID trackerId, SlugcatStats.Name slugcat) 
                => (ModManager.Watcher && slugcat.value == "Watcher" && trackerId == WinState.EndgameID.Traveller) || orig(trackerId, slugcat);

            /// <summary>Prevent Ripple from being raised automatically.
            /// This also prevents the Ripple ladder from appearing (`Watcher.SpinningTop.SpawnWarpPoint`).</summary>
            private static bool Dont(On.Watcher.SpinningTop.orig_CanRaiseRippleLevel orig, Watcher.SpinningTop self) => false;

            internal static void DetectSpinningTop(On.Watcher.SpinningTop.orig_MarkSpinningTopEncountered orig, Watcher.SpinningTop self)
            {
                orig(self);
                string loc = $"SpinningTop-{self.room.abstractRoom.name.Region()}";
                if (Plugin.RandoManager.IsLocationGiven(loc) == false) Plugin.RandoManager.GiveLocation(loc);
            }

            internal static void DetectStaticWarpPoint(SaveState saveState)
            {
                foreach (var point in saveState.deathPersistentSaveData.newlyDiscoveredWarpPoints)
                {
                    string loc = $"Warp-{point.Key.Split(':')[0].ToUpperInvariant()}";
                    if (Plugin.RandoManager.IsLocationGiven(loc) == false) Plugin.RandoManager.GiveLocation(loc);
                }
            }
            /// <summary>Prevent Spinning Top from spawning if the key is not collected (unless that setting is disabled).</summary>
            private static void SpinningTopKeyCheck(ILContext il)
            {
                ILCursor c = new ILCursor(il);
                // Branch interception at 1d71 (roomSettings.placedObjects[num10].type == WatcherEnums.PlacedObjectType.SpinningTopSpot).
                c.GotoNext(x => x.MatchLdsfld(typeof(WPOT).GetField(nameof(WPOT.SpinningTopSpot))));  // 1d67
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ExtEnum<PlacedObject.Type>).GetMethod("op_Equality")));  // 1d6c

                c.Emit(OpCodes.Ldarg_0);  // Room this
                c.Emit(OpCodes.Ldloc, 27);  // int num10 (iteration variable)
                bool Delegate(bool orig, Room self, int index)
                {
                    if (!orig || !Settings.spinningTopKeys) return orig;
                    string dest = (self.roomSettings.placedObjects[index].data as SpinningTopData).destRegion;
                    return !Items.StaticKey.IsMissing(self.world.name, dest);
                }
                c.EmitDelegate<Func<bool, Room, int, bool>>(Delegate);
            }
        }
    }
}
