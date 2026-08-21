namespace RainWorldRandomizer.SaveData;

public static class SaveDataHooks
{
    public static void ApplyHooks()
    {
        On.ProcessManager.PreSwitchMainProcess += ProcessManagerOnPreSwitchMainProcess;
    }

    public static void RemoveHooks()
    {
        On.ProcessManager.PreSwitchMainProcess -= ProcessManagerOnPreSwitchMainProcess;
    }

    private static void ProcessManagerOnPreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID id)
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
}