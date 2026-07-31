using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using UnityEngine;
using MWSD = MiscWorldSaveData;

namespace RainWorldRandomizer.WatcherIntegration
{
    public static class Completion
    {
        public static class Hooks
        {
            private static Hook _rottedRegionTargetHook;

            public static void ApplyHooks()
            {
                On.ProcessManager.RequestMainProcessSwitch_ProcessID += DetectCompletion;

                _rottedRegionTargetHook = new Hook(
                    typeof(MWSD).GetProperty(nameof(MWSD.remainingRegionsForSentientRotEnding)).GetGetMethod(), 
                    typeof(Hooks).GetMethod(nameof(ApplyRottedRegionTarget), EntryPoint.bfAll));
            }

            public static void RemoveHooks()
            {
                On.ProcessManager.RequestMainProcessSwitch_ProcessID -= DetectCompletion;
                _rottedRegionTargetHook.Undo();
            }

            /// <summary>Reduce the number of regions needed for the Sentient Rot ending to match <see cref="Settings.rottedRegionTarget"/>.</summary>
            internal static int ApplyRottedRegionTarget(Func<MWSD, int> orig, MWSD self)
                => Plugin.ArchipelagoActive ? Mathf.Max(orig(self) - 21 + RandoOptions.RottedRegionTarget, 0) : orig(self);

            /// <summary>Detect completion conditions when switching to the ending slideshows.</summary>
            private static void DetectCompletion(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
            {
                if (ID == ProcessManager.ProcessID.SlideShow && Plugin.ArchipelagoActive)
                {
                    switch (self.nextSlideshow.value)
                    {
                        case "EndingSpinningTop":
                            Plugin.ArchipelagoManager.GiveCompletionCondition(RandoOptions.CompletionCondition.SpinningTop);
                            // Release all Spinning Top checks because they are now impossible
                            foreach (var loc in Plugin.RandoManager.GetLocations().Where(l => l.kind == LocationInfo.LocationKind.SpinningTop))
                                Plugin.ArchipelagoManager.GiveLocation(loc.internalName);
                            break;
                        case "EndingRot":
                            Plugin.ArchipelagoManager.GiveCompletionCondition(RandoOptions.CompletionCondition.SentientRot);
                            break;
                        case "EndingVoidWeaver":
                            Plugin.ArchipelagoManager.GiveCompletionCondition(RandoOptions.CompletionCondition.Weaver);
                            break;
                    }
                }
                orig(self, ID);
            }
        }
    }
}
