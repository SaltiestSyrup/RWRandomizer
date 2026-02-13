namespace RainWorldRandomizer.WatcherIntegration
{
    public static class MiscChanges
    {
        public static void ApplyHooks()
        {
            On.World.InitiateWeaverPresence += World_InitiateWeaverPresence;
        }

        public static void RemoveHooks()
        {
            On.World.InitiateWeaverPresence -= World_InitiateWeaverPresence;
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
    }
}
