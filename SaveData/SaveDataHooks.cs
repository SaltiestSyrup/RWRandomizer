namespace RainWorldRandomizer.SaveData;

public static class SaveDataHooks
{
    public static void ApplyHooks()
    {
        On.ProcessManager.PreSwitchMainProcess += ProcessManagerOnPreSwitchMainProcess;
        On.Options.GetSaveFileName_SavOrExp += OptionsOnGetSaveFileName_SavOrExp;
    }

    public static void RemoveHooks()
    {
        On.ProcessManager.PreSwitchMainProcess -= ProcessManagerOnPreSwitchMainProcess;
        On.Options.GetSaveFileName_SavOrExp -= OptionsOnGetSaveFileName_SavOrExp;
    }

    private static void ProcessManagerOnPreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig,
        ProcessManager self, ProcessManager.ProcessID id)
    {
        if (SaveTracker.CustomSlotActive && id == ProcessManager.ProcessID.MainMenu)
        {
            int curSlot = self.rainWorld.options.saveSlot;
            self.rainWorld.options.saveSlot = SaveTracker.OrigSaveSlot;
            self.rainWorld.progression.Destroy(curSlot);
            self.rainWorld.progression = new PlayerProgression(self.rainWorld, true, true);
            SaveTracker.CustomSlotActive = false;
            SaveTracker.ActiveLegacySlot = -1;
        }

        orig(self, id);
    }

    private static string OptionsOnGetSaveFileName_SavOrExp(On.Options.orig_GetSaveFileName_SavOrExp orig, Options self)
    {
        string origVal = orig(self);
        // Redundant double check here to make sure the slot is ours.
        // Vanilla slot numbers won't collide, but another mod might.
        return self.saveSlot >= SaveTracker.SLOT_OFFSET && SaveTracker.CustomSlotActive
            ? $"ran{self.saveSlot+1}"
            : origVal;
    }
}
