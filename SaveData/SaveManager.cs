using Archipelago.MultiClient.Net.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MoreSlugcats;
using RainWorldRandomizer.Menu;
using RainWorldRandomizer.SaveData;
using RWCustom;
using UnityEngine;

namespace RainWorldRandomizer
{
    public static class SaveManager
    {
        public static bool SeenWatcherSealLockedWarpTutorial(this DeathPersistentSaveData dpsd)
        {
            return dpsd.tutorialMessages.Contains(RandomizerEnums.Tutorial.WatcherSealLockedWarp);
        }

        public static void SetWatcherSealLockedWarpTutorial(this DeathPersistentSaveData dpsd, bool value)
        {
            dpsd.SetTutorialValue(RandomizerEnums.Tutorial.WatcherSealLockedWarp, value);
        }
        
        public static bool IsThereASavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            return File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));
        }

        // Meant for vanilla saves only
        // public static void WriteSavedGameToFile(Dictionary<string, Unlock> game, SlugcatStats.Name slugcat, int saveSlot)
        // {
        //     StreamWriter file = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));
        //
        //     file.WriteLine($"StartingDen->{Plugin.RandoManager.customStartDen}");
        //     file.WriteLine(Plugin.RandoManager.currentSeed);
        //     foreach (var item in game)
        //     {
        //         // TODO: Rewrite saves to use new ExtEnum
        //         string serializedUnlock = $"{{{item.Value.Type.value},{item.Value.ID},{item.Value.IsGiven}}}";
        //
        //         file.Write($"{item.Key}->{serializedUnlock}");
        //         file.WriteLine();
        //     }
        //
        //     file.Close();
        // }

        // Meant for vanilla saves only
        // public static Dictionary<string, Unlock> LoadSavedGame(SlugcatStats.Name slugcat, int saveSlot)
        // {
        //     Dictionary<string, Unlock> game = [];
        //
        //     string[] file = File.ReadAllLines(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));
        //
        //     Plugin.RandoManager.customStartDen = Regex.Split(file[0], "->")[1]; // StartingDen->SU_S01
        //     Plugin.RandoManager.currentSeed = file[1];
        //     file = [.. file.Skip(2)];
        //
        //     foreach (string line in file)
        //     {
        //         string[] keyValue = Regex.Split(line, "->");
        //
        //         string[] unlockString = Regex.Split(keyValue[1]
        //             .TrimStart('{')
        //             .TrimEnd('}'), ",");
        //
        //         Unlock.UnlockType type = Unlock.UnlockType.Item;
        //         if (ExtEnumBase.TryParse(typeof(Unlock.UnlockType), unlockString[0], true, out ExtEnumBase t))
        //         {
        //             type = (Unlock.UnlockType)t;
        //         }
        //
        //         Unlock unlock = new(
        //             type,
        //             unlockString[1],
        //             bool.Parse(unlockString[2]));
        //
        //         game.Add(keyValue[0], unlock);
        //     }
        //
        //     return game;
        // }

        // public static void WriteItemQueueToFile(IEnumerable<Unlock.Item> items, IEnumerable<TrapsHandler.Trap> traps, SlugcatStats.Name slugcat, int saveSlot)
        // {
        //     string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt");
        //
        //     // If there is nothing to store, delete any stored data
        //     if (items.Count() + traps.Count() == 0)
        //     {
        //         if (File.Exists(path))
        //         {
        //             File.Delete(path);
        //         }
        //         return;
        //     }
        //
        //     StreamWriter file = File.CreateText(path);
        //
        //     foreach (TrapsHandler.Trap trap in traps)
        //     {
        //         file.WriteLine($"Trap,{trap.id}");
        //     }
        //     foreach (Unlock.Item item in items)
        //     {
        //         file.WriteLine($"{item.type.enumType.Name},{item.id}");
        //     }
        //
        //     file.Close();
        // }

        // public static (Queue<Unlock.Item>, Queue<TrapsHandler.Trap>) LoadItemQueue(SlugcatStats.Name slugcat, int saveSlot)
        // {
        //     Queue<Unlock.Item> itemQueue = [];
        //     Queue<TrapsHandler.Trap> trapQueue = [];
        //
        //     if (!File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt")))
        //         return (itemQueue, trapQueue);
        //
        //     string[] text = File.ReadAllLines(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt"));
        //
        //     foreach (string line in text)
        //     {
        //         string[] itemString = Regex.Split(line, ",");
        //         Unlock.Item item;
        //
        //         if (itemString[0] == "Trap")
        //         {
        //             trapQueue.Enqueue(new TrapsHandler.Trap(itemString[1]));
        //             continue;
        //         }
        //
        //         if (itemString[0] == nameof(DataPearl.AbstractDataPearl.DataPearlType))
        //         {
        //             item = Unlock.IDToItem(itemString[1], true);
        //         }
        //         else if (itemString[0] == nameof(AbstractPhysicalObject.AbstractObjectType))
        //         {
        //             item = Unlock.IDToItem(itemString[1]);
        //         }
        //         else
        //         {
        //             Plugin.Log.LogError($"Encountered error in LoadItemQueue:\n\t'{itemString[0]}' is not a valid type");
        //             continue;
        //         }
        //
        //         itemQueue.Enqueue(item);
        //     }
        //
        //     return (itemQueue, trapQueue);
        // }

        // [Obsolete("Unsafe when RandoManager is null, which is the only case where it is useful")]
        // public static int CountRedsCycles(int saveSlot)
        // {
        //     if (!IsThereASavedGame(SlugcatStats.Name.Red, saveSlot))
        //     {
        //         return -1;
        //     }
        //
        //     Dictionary<string, Unlock> game = LoadSavedGame(SlugcatStats.Name.Red, saveSlot);
        //     return game.Values.Where(u => u.Type == Unlock.UnlockType.HunterCycles && u.IsGiven).Count();
        // }


        private const string SCOUTED_LOCS_KEY = "RANDOMIZER_SCOUTED_LOCS";
        private static Dictionary<string, ItemFlags> _scoutedLocations;
        public static Dictionary<string, ItemFlags> ScoutedLocations
        {
            get
            {
                if (_scoutedLocations is not null) return _scoutedLocations;

                // If not loaded yet, try to load from save data
                DeathPersistentSaveData dpsd = Plugin.Singleton.Game?.GetStorySession?.saveState?.deathPersistentSaveData;
                if (dpsd is null) return null;

                string savedData = dpsd.unrecognizedSaveStrings.FirstOrDefault(s => s.StartsWith(SCOUTED_LOCS_KEY));
                if (savedData is null) return [];

                var scouted = JsonConvert.DeserializeObject<Dictionary<string, ItemFlags>>(savedData.Substring(SCOUTED_LOCS_KEY.Length));
                _scoutedLocations = scouted;
                return _scoutedLocations;
            }
        }

        public static void AddScoutedLocations(Dictionary<string, ItemFlags> scoutedLocs)
        {
            DeathPersistentSaveData dpsd = Plugin.Singleton.Game?.GetStorySession?.saveState?.deathPersistentSaveData;
            if (dpsd is null)
            {
                Plugin.Log.LogError("Tried to add scouted locations, but there is no current DeathPersistentSaveData");
                return;
            }

            // Save new data in memory and create new save string
            _scoutedLocations ??= [];
            foreach (var loc in scoutedLocs) _scoutedLocations[loc.Key] = loc.Value;
            string newData = $"{SCOUTED_LOCS_KEY}{JsonConvert.SerializeObject(_scoutedLocations)}";

            // Try to find existing key for this data
            string savedData = dpsd.unrecognizedSaveStrings.FirstOrDefault(s => s.StartsWith(SCOUTED_LOCS_KEY));
            int index = dpsd.unrecognizedSaveStrings.IndexOf(savedData);

            // Write to DeathPersistentSaveData
            if (savedData is null) dpsd.unrecognizedSaveStrings.Add(newData);
            else dpsd.unrecognizedSaveStrings[index] = newData;
        }

        public static void ClearScoutedLocationCache()
        {
            _scoutedLocations = null;
        }

        public struct APSave(long lastIndex, Dictionary<string, bool> locationsStatus)
        {
            public long lastIndex = lastIndex;
            public Dictionary<string, bool> locationsStatus = locationsStatus;
        }

        // AP saves store the found locations under a save ID, which is a string of pattern "[Generation Seed]_[Player Name]"
        // public static bool IsThereAnAPSave(string saveId)
        // {
        //     return File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json"));
        // }

        // public static void WriteAPSaveToFile(string saveId, long lastIndex, List<LocationInfo> locations)
        // {
        //     if (locations == null || locations.Count == 0) return;
        //
        //     APSave save = new(lastIndex, locations.ToDictionary(l => l.internalName, l => l.Collected));
        //
        //     StreamWriter saveFile = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json"));
        //
        //     string jsonSave = JsonConvert.SerializeObject(save, Formatting.Indented);
        //     saveFile.Write(jsonSave);
        //
        //     saveFile.Close();
        // }

        // public static APSave LoadAPSave(string saveId)
        // {
        //     string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json");
        //
        //     if (!File.Exists(path))
        //     {
        //         Plugin.Log.LogError($"Failed to load save from file: ap_save_{saveId}.json");
        //         return new APSave();
        //     }
        //
        //     return JsonConvert.DeserializeObject<APSave>(File.ReadAllText(path));
        // }

        /// <summary>
        /// Deletes every AP save file in the "newest" folder
        /// </summary>
        public static void DeleteAllAPSaves()
        {
            string folder = ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath;

            foreach (string file in Directory.EnumerateFiles(folder))
            {
                if (file.Contains("ap_save"))
                {
                    File.Delete(file);
                }
            }
        }

        public static bool HasSaveFileForSlot(int saveSlot)
        {
            return File.Exists(Path.Combine(SaveTracker.PersistentDataDir, $"rand{saveSlot}"));
        }

        /// <summary>
        /// Find if there is a legacy Archipelago save file for the given generation seed and slot name.
        /// </summary>
        public static bool HasLegacySave(string seed, string slotName)
        {
            return File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{seed}_{slotName}.json"));
        }

        // TODO: Detection for existing standalone saves
        public static bool HasLegacySave()
        {
            throw new NotImplementedException();
        }

        public static long GetLastIndexFromLegacy(string seed, string slotName)
        {
            string saveId = $"{seed}_{slotName}";
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json");
            
            if (!File.Exists(path))
            {
                Plugin.Log.LogError($"Failed to load save from file: ap_save_{saveId}.json");
                return 0L;
            }
            
            return JsonConvert.DeserializeObject<APSave>(File.ReadAllText(path)).lastIndex;
        }
        
        // Requires the correct Progression to be active
        /// <summary>
        /// Creates and writes save data to file, using the game and randomizer's current state.
        /// </summary>
        /// <param name="rainWorld"></param>
        /// <param name="randoManager"></param>
        /// <param name="saveCurrentState">Whether certain non death persistent values should be saved.</param>
        public static void WriteToFile(RainWorld rainWorld, ManagerBase randoManager, bool saveCurrentState = true)
        {
            if (rainWorld.progression.currentSaveState is null)
            {
                Plugin.Log.LogError("Failed to write randomizer save, no active save state.");
                return;
            }

            if (!SaveTracker.CustomSlotActive || randoManager is null)
            {
                Plugin.Log.LogError("Failed to write randomizer save, not currently in a randomizer state.");
                return;
            }
            
            string path = SaveTracker.PersistentDataDir;
            Directory.CreateDirectory(path);

            StreamWriter file = File.CreateText(Path.Combine(path, $"rand{rainWorld.options.saveSlot}.json"));
            
            file.Write(JsonConvert.SerializeObject(SaveFile.Create(rainWorld.progression.currentSaveState, randoManager, saveCurrentState)));
            file.Close();
        }

        /// <summary>
        /// Writes an existing <see cref="SaveFile"/> instance to file, at the specified save slot.
        /// </summary>
        public static void WriteToFile(SaveFile saveFile, int slot)
        {
            string path = SaveTracker.PersistentDataDir;
            Directory.CreateDirectory(path);

            StreamWriter file = File.CreateText(Path.Combine(path, $"rand{slot}.json"));
            
            file.Write(JsonConvert.SerializeObject(saveFile));
            file.Close();
        }

        public static bool TryReadFromFile(int saveSlot, out SaveFile saveFile)
        {
            saveFile = new SaveFile();
            string filePath = Path.Combine(SaveTracker.PersistentDataDir, $"rand{saveSlot}.json");

            if (!File.Exists(filePath))
            {
                Plugin.Log.LogError($"Failed to find save file for slot {saveSlot}");
                return false;
            }

            try
            {
                saveFile = JsonConvert.DeserializeObject<SaveFile>(File.ReadAllText(filePath));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to deserialize save file: {e}");
                return false;
            }
            
            return true;
        }

        public static void DeleteFile(RainWorld rainWorld, int saveSlot)
        {
            if (SaveTracker.CustomSlotActive 
                && (rainWorld.progression?.progressionLoaded ?? false))
            {
                Plugin.Log.LogError("Cannot delete save file, as there is one currently loaded");
            }
            
            string filePath1 = Path.Combine(SaveTracker.PersistentDataDir, $"rand{saveSlot}.json");
            string filePath2 = Path.Combine(Application.persistentDataPath, $"sav{saveSlot + 1}");
            
            if (File.Exists(filePath1)) File.Delete(filePath1);
            if (File.Exists(filePath2)) File.Delete(filePath2);
        }
    }

    public struct SaveFile()
    {
        // Normal stats
        public bool isDownpourDLC = false;
        public bool isWatcherDLC = false;
        public string slugcat = null;
        public int karma = 0;
        public int maxKarma = 0;
        public float ripple = 1;
        public bool karmaReinforced = false;
        public int food = 0;
        public IntVector2 maxFood = new(7, 4);
        public int cycle = 0;
        public double playtime = 0;
        public DateTime lastPlayed;
        
        // Randomizer stuff
        public string seed;
        public string startingDen;
        public bool completedGoal;
        public Dictionary<string, UnlockInfo> locationMap = null;
        public List<FillerItem> pendingFiller = null;
        public List<string> pendingTraps = null;
        public OptionStruct options = new();
        
        // Archipelago stuff
        public bool isArchipelago = false;
        public long lastItemIndex = 0;
        public ConnectionInfo connectionInfo = default;

        public static SaveFile Create(SaveState saveState, ManagerBase randoManager, bool saveCurrentState = true)
        {
            return new SaveFile
            {
                // TODO: Doesn't currently consider whether current state should be saved for normal save values
                isDownpourDLC = ModManager.MSC,
                isWatcherDLC = ModManager.Watcher,
                slugcat = saveState.saveStateNumber.value,
                karma = saveState.deathPersistentSaveData.karma,
                maxKarma = saveState.deathPersistentSaveData.karmaCap,
                ripple = saveState.deathPersistentSaveData.rippleLevel,
                karmaReinforced = saveState.deathPersistentSaveData.reinforcedKarma,
                food = saveState.food,
                maxFood = SlugcatStats.SlugcatFoodMeter(saveState.saveStateNumber),
                cycle = saveState.cycleNumber,
                playtime = SpeedRunTimer.GetCampaignTimeTracker(saveState.saveStateNumber).TotalFreeTime,
                lastPlayed = DateTime.Now,
                
                seed = randoManager.currentSeed,
                startingDen = randoManager.customStartDen,
                completedGoal = randoManager is ManagerArchipelago { gameCompleted: true },
                locationMap = randoManager.GetLocations()
                    .ToDictionary(l => l.internalName, l =>
                    {
                        UnlockInfo info = new UnlockInfo { collected = l.Collected };
                        if (randoManager.GetUnlockAtLocation(l.internalName) is Unlock unl)
                        {
                            info.type = unl.Type.value;
                            info.id = unl.ID;
                        }

                        return info;
                    }),
                pendingFiller = [.. (saveCurrentState ? randoManager.itemDeliveryQueue : randoManager.lastItemDeliveryQueue)
                    .Select(i => new FillerItem { type = i.type.value, id = i.id })],
                pendingTraps = [.. randoManager.pendingTrapQueue.Select(t => t.id)],
                options = RandoOptions.LoadedOptions,
                
                isArchipelago = randoManager is ManagerArchipelago,
                lastItemIndex = randoManager is ManagerArchipelago ? ArchipelagoConnection.lastItemIndex : 0,
                connectionInfo = randoManager is ManagerArchipelago 
                    ? new ConnectionInfo
                    {
                        hostName = ArchipelagoConnection.ConnectedHostName,
                        port = ArchipelagoConnection.ConnectedPort,
                        slotName = ArchipelagoConnection.ConnectedSlotName,
                        password = ArchipelagoConnection.ConnectedPassword,
                    } : default
            };
        }

        public struct UnlockInfo()
        {
            public bool collected = false;
            public string type = null;
            public string id = null;
        }
        
        public struct FillerItem()
        {
            public string type;
            public string id;
        }
        
        public struct ConnectionInfo
        {
            public string hostName;
            public int port;
            public string slotName;
            public string password;
        }
    }
}