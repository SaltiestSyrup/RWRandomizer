using System.Collections.Generic;
using System.IO;
using RainWorldRandomizer.Generation;
using UnityEngine;

namespace RainWorldRandomizer.SaveData;

public class SaveTracker
{
    public const int SLOT_OFFSET = 100000;
    
    public static int OrigSaveSlot = 0;
    public static bool CustomSlotActive;
    /// <summary>
    /// Refers to the save slot the randomizer file is stored under, not the campaign slot the game is loading.
    /// Negative if no legacy slot active
    /// </summary>
    public static int ActiveLegacySlot = -1;
    /// <summary>
    /// Last generator used for a new standalone game.
    /// Stored here so the manager can find and load the data.
    /// </summary>
    public static VanillaGenerator ActiveGenerator;

    public Dictionary<int, SaveFile> SaveSlots { get; }

    public static int CurrentRandomizerSlot
    {
        get
        {
            if (!CustomSlotActive) return -1;
            if (ActiveLegacySlot >= 0) return ActiveLegacySlot;
            return Plugin.Singleton.rainWorld.options.saveSlot;
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

    public SaveTracker()
    {
        SaveSlots = [];
        LoadSlotsFromFile();
        LoadLegacySlotsFromFile();
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

    private void LoadSlotsFromFile()
    {
        string path = PersistentDataDir;
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (string fileName in Directory.EnumerateFiles(path))
        {
            if (int.TryParse(Path.GetFileNameWithoutExtension(fileName).Substring(4), out int slot)
                && SaveManager.TryReadFromFile(slot, out SaveFile save))
            {
                SaveSlots[slot] = save;
            }
        }
    }

    private void LoadLegacySlotsFromFile()
    {
        List<SaveFile> legacyFiles = SaveManager.LoadAllLegacyStandaloneSaves();
        foreach (SaveFile saveFile in legacyFiles)
        {
            if (TryGetNextSaveSlot(saveFile.legacySaveSlot, out int slot))
            {
                SaveSlots[slot] = saveFile;
            }
            else
            {
                Plugin.Log.LogError($"Failed to find new slot number for importing standalone legacy file.\n" +
                                    $"{new System.Diagnostics.StackTrace()}");
            }
        }
    }
}