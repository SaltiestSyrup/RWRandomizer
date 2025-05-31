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
            }

            internal static void Unapply()
            {
                On.Watcher.SpinningTop.MarkSpinningTopEncountered -= DetectSpinningTop;
                On.Watcher.SpinningTop.CanRaiseRippleLevel -= Dont;
            }

            /// <summary>Prevent Ripple from being raised automatically.</summary>
            private static bool Dont(On.Watcher.SpinningTop.orig_CanRaiseRippleLevel orig, Watcher.SpinningTop self) => false;

            internal static void DetectSpinningTop(On.Watcher.SpinningTop.orig_MarkSpinningTopEncountered orig, Watcher.SpinningTop self)
            {
                orig(self);
                string loc = $"SpinningTop-{self.room.abstractRoom.name.ToUpperInvariant()}";
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
        }
    }
}
