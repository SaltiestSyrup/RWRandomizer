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

    private Dictionary<int, SaveFile> saveSlots;

    public Dictionary<int, SaveFile> SaveSlots
    {
        get
        {
            saveSlots ??= LoadSlotsFromFile();
            return saveSlots;
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
    /// <param name="newSlot">The slot number that the new slot was created at.</param>
    /// <returns>True if the slot was successfully created.</returns>
    public bool TryGetNextSaveSlot(int fromSlot, out int newSlot)
    {
        newSlot = -1;
        int slotNumber = -1;
        int num = 0;
        do
        {
            if (!SaveSlots.ContainsKey(num + SLOT_OFFSET * (fromSlot + 1)))
            {
                slotNumber = num + SLOT_OFFSET * (fromSlot + 1);
            }

            num++;
            if (num >= SLOT_OFFSET)
                return false;
        } while (slotNumber == -1);

        newSlot = slotNumber;
        return true;
    }

    private static Dictionary<int, SaveFile> LoadSlotsFromFile()
    {
        string path = PersistentDataDir;
        Dictionary<int, SaveFile> slots = [];
        if (!Directory.Exists(path))
        {
            return slots;
        }

        foreach (string fileName in Directory.EnumerateFiles(path))
        {
            if (int.TryParse(Path.GetFileNameWithoutExtension(fileName).Substring(4), out int slot)
                && SaveManager.TryReadFromFile(slot, out SaveFile save))
            {
                slots[slot] = save;
            }
        }
        
        return slots;
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