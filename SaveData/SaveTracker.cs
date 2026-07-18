using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace RainWorldRandomizer.SaveData;

public static class SaveTracker
{
    public const int SLOT_OFFSET = 10000;
    
    public static int OrigSaveSlot = 0;
    public static bool CustomSlotActive;

    private static List<SaveSlotInfo> _saveSlots;

    public static List<SaveSlotInfo> SaveSlots
    {
        get
        {
            return _saveSlots ?? LoadSlotsFromFile();
        }
    }

    private static string PersistentDataDir
    {
        get
        {
            return string.Join("", [
                Application.persistentDataPath,
                Path.DirectorySeparatorChar,
                "Randomizer",
                Path.DirectorySeparatorChar,
            ]);
        }
    }

    public static void AddNewSaveSlot(int slotNumber, SlugcatStats.Name slugcat)
    {
        SaveSlotInfo info = new SaveSlotInfo(slotNumber, slugcat.value);
        
        if (_saveSlots is null) LoadSlotsFromFile();
        if (_saveSlots!.Contains(info))
        {
            Plugin.Log.LogWarning("Tried to create already existing save slot");
            return;
        }
        
        _saveSlots!.Add(info);
        SaveSlotsToFile(); // TODO: Move this somewhere more responsible
    }

    private static void SaveSlotsToFile()
    {
        if (_saveSlots is null)
        {
            Plugin.Log.LogWarning("Failed to write randomizer saves to file as they are not yet loaded.");
        }

        string path = PersistentDataDir;
        Directory.CreateDirectory(path); // Make sure the folder exists
        StreamWriter file = File.CreateText(Path.Combine(path, "randomizer_saves.json"));
        
        file.Write(JsonConvert.SerializeObject(_saveSlots));
        file.Close();
    }

    private static List<SaveSlotInfo> LoadSlotsFromFile()
    {
        string path = PersistentDataDir;
        if (!Directory.Exists(path))
        {
            _saveSlots = [];
            return [];
        }

        _saveSlots = JsonConvert.DeserializeObject<List<SaveSlotInfo>>(File.ReadAllText(Path.Combine(path, "randomizer_saves.json")));
        return _saveSlots;
    }

    public record struct SaveSlotInfo(int slotNumber, string slugcatName)
    {
        public readonly int slotNumber = slotNumber;
        public readonly string slugcatName = slugcatName;
    }
}