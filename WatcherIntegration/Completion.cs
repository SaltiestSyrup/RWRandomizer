using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using UnityEngine;
using MWSD = MiscWorldSaveData;

namespace RainWorldRandomizer.WatcherIntegration
{
    internal static class Completion
    {
        internal static class Hooks
        {
            internal static Hook rottedRegionTargetHook;

            internal static void ApplyHooks()
            {
                On.ProcessManager.RequestMainProcessSwitch_ProcessID += DetectCompletion;

                rottedRegionTargetHook = new Hook(
                    typeof(MWSD).GetProperty(nameof(MWSD.remainingRegionsForSentientRotEnding)).GetGetMethod(), 
                    typeof(Hooks).GetMethod(nameof(ApplyRottedRegionTarget), EntryPoint.bfAll));
            }

            internal static void RemoveHooks()
            {
                On.ProcessManager.RequestMainProcessSwitch_ProcessID -= DetectCompletion;
                rottedRegionTargetHook.Undo();
            }

            /// <summary>Reduce the number of regions needed for the Sentient Rot ending to match <see cref="Settings.rottedRegionTarget"/>.</summary>
            internal static int ApplyRottedRegionTarget(Func<MWSD, int> orig, MWSD self)
                => Mathf.Max(orig(self) - 21 + (int)Settings.rottedRegionTarget, 0);

            /// <summary>Detect completion conditions when switching to the ending slideshows.</summary>
            private static void DetectCompletion(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
            {
                if (ID == ProcessManager.ProcessID.SlideShow && Plugin.RandoManager is ManagerArchipelago managerAP && !managerAP.gameCompleted)
                {
                    switch (self.nextSlideshow.value)
                    {
                        case "EndingSpinningTop":
                            managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SpinningTop);
                            // Release all Spinning Top checks because they are now impossible
                            foreach (var loc in managerAP.GetLocations().Where(l => l.kind == LocationInfo.LocationKind.SpinningTop))
                                managerAP.GiveLocation(loc.internalName);
                            break;
                        case "EndingRot":
                            managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.SentientRot);
                            break;
                        case "EndingVoidWeaver":
                            managerAP.GiveCompletionCondition(ArchipelagoConnection.CompletionCondition.Weaver);
                            break;
                    }
                }
                orig(self, ID);
            }
        }
    }
}
