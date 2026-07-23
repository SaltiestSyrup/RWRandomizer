using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace RainWorldRandomizer.SaveData;

public class SaveTracker
{
    public const int SLOT_OFFSET = 100000;
    
    public static int OrigSaveSlot = 0;
    public static bool CustomSlotActive;

    private Dictionary<int, SaveSlotInfo> saveSlots;

    public Dictionary<int, SaveSlotInfo> SaveSlots
    {
        get
        {
            return saveSlots ?? LoadSlotsFromFile();
        }
    }

    public static string PersistentDataDir
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

    /// <summary>
    /// Register a new randomizer save slot.
    /// </summary>
    /// <param name="fromSlot">The game's current save slot that this should be assigned to.</param>
    /// <param name="slugcat">The slugcat to be registered to the new slot.</param>
    /// <param name="slotRegisteredTo">The slot number that the new slot was created at.</param>
    /// <returns>True if the slot was successfully created.</returns>
    public bool TryAddNewSaveSlot(int fromSlot, SlugcatStats.Name slugcat, out int slotRegisteredTo)
    {
        slotRegisteredTo = -1;
        if (saveSlots is null) LoadSlotsFromFile();

        int slotNumber = -1;
        int num = 0;
        do
        {
            if (!saveSlots!.ContainsKey(num + SLOT_OFFSET * fromSlot))
            {
                slotNumber = num + SLOT_OFFSET * fromSlot;
            }

            num++;
            if (num >= SLOT_OFFSET)
                return false;
        } while (slotNumber == -1);
        
        saveSlots![slotNumber] = new SaveSlotInfo(slotNumber, slugcat.value);
        SaveSlotsToFile(); // TODO: Move this somewhere more responsible
        slotRegisteredTo = slotNumber;
        return true;
    }

    private void SaveSlotsToFile()
    {
        if (saveSlots is null)
        {
            Plugin.Log.LogWarning("Failed to write randomizer saves to file as they are not yet loaded.");
            return;
        }

        string path = PersistentDataDir;
        Directory.CreateDirectory(path); // Make sure the folder exists
        StreamWriter file = File.CreateText(Path.Combine(path, "randomizer_saves.json"));
        
        file.Write(JsonConvert.SerializeObject(saveSlots.Select(kvp => new SaveSlotIdentifier(kvp.Key, kvp.Value.slugcatName))));
        file.Close();
    }

    private Dictionary<int, SaveSlotInfo> LoadSlotsFromFile()
    {
        string path = PersistentDataDir;
        if (!Directory.Exists(path))
        {
            saveSlots = [];
            return [];
        }

        saveSlots = JsonConvert.DeserializeObject<List<SaveSlotIdentifier>>(File.ReadAllText(Path.Combine(path, "randomizer_saves.json")))
            .ToDictionary(id => id.slotNumber, id => new SaveSlotInfo(id.slotNumber, id.slugcatName));
        
        // For each slot number found, create a new progression instance
        // mine each progression for the save data, store in struct
        
        return saveSlots;
    }

    private record struct SaveSlotIdentifier(int slotNumber, string slugcatName)
    {
        public readonly int slotNumber = slotNumber;
        public readonly string slugcatName = slugcatName;
    }

    public struct SaveSlotInfo(int slotNumber, string slugcatName)
    {
        public readonly int slotNumber = slotNumber;
        public readonly string slugcatName = slugcatName;
        // Check count
        // Completion
        // is Archipelago
        // options
        // 
    }
}