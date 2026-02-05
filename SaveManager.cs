using Archipelago.MultiClient.Net.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RainWorldRandomizer
{
    public static class SaveManager
    {
        public static bool IsThereASavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            return File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));
        }

        // Meant for vanilla saves only
        public static void WriteSavedGameToFile(Dictionary<string, ItemInfo> game, SlugcatStats.Name slugcat, int saveSlot)
        {
            StreamWriter file = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));

            file.WriteLine($"StartingDen->{Plugin.RandoManager.customStartDen}");
            file.WriteLine(Plugin.RandoManager.currentSeed);
            foreach (var item in game)
            {
                // TODO: Rewrite saves to use new ExtEnum
                string serializedUnlock = $"{{{item.Value.Type.value},{item.Value.ID},{item.Value.IsGiven}}}";

                file.Write($"{item.Key}->{serializedUnlock}");
                file.WriteLine();
            }

            file.Close();
        }

        // Meant for vanilla saves only
        public static Dictionary<string, ItemInfo> LoadSavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            Dictionary<string, ItemInfo> game = [];

            string[] file = File.ReadAllLines(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));

            if (int.TryParse(file[0], out int seed))
            {
                Plugin.RandoManager.currentSeed = seed;
                file = [.. file.Skip(1)];
            }
            else
            {
                // New save file includes additional line to store starting den
                Plugin.RandoManager.customStartDen = Regex.Split(file[0], "->")[1]; // StartingDen->SU_S01
                Plugin.RandoManager.currentSeed = int.Parse(file[1]);
                file = [.. file.Skip(2)];
            }

            foreach (string line in file)
            {
                string[] keyValue = Regex.Split(line, "->");

                string[] unlockString = Regex.Split(keyValue[1]
                    .TrimStart('{')
                    .TrimEnd('}'), ",");

                ItemInfo.ItemInfoType type = ItemInfo.ItemInfoType.Item;
                if (ExtEnumBase.TryParse(typeof(ItemInfo.ItemInfoType), unlockString[0], true, out ExtEnumBase t))
                {
                    type = (ItemInfo.ItemInfoType)t;
                }

                ItemInfo unlock = new(
                    type,
                    unlockString[1],
                    bool.Parse(unlockString[2]));

                game.Add(keyValue[0], unlock);
            }

            return game;
        }

        public static void WriteItemQueueToFile(IEnumerable<ItemInfo.PhysicalObjectItem> items, IEnumerable<TrapsHandler.Trap> traps, SlugcatStats.Name slugcat, int saveSlot)
        {
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt");

            // If there is nothing to store, delete any stored data
            if (items.Count() + traps.Count() == 0)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return;
            }

            StreamWriter file = File.CreateText(path);

            foreach (TrapsHandler.Trap trap in traps)
            {
                file.WriteLine($"Trap,{trap.id}");
            }
            foreach (ItemInfo.PhysicalObjectItem item in items)
            {
                file.WriteLine($"{item.type.enumType.Name},{item.id}");
            }

            file.Close();
        }

        public static (Queue<ItemInfo.PhysicalObjectItem>, Queue<TrapsHandler.Trap>) LoadItemQueue(SlugcatStats.Name slugcat, int saveSlot)
        {
            Queue<ItemInfo.PhysicalObjectItem> itemQueue = [];
            Queue<TrapsHandler.Trap> trapQueue = [];

            if (!File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt")))
                return (itemQueue, trapQueue);

            string[] text = File.ReadAllLines(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt"));

            foreach (string line in text)
            {
                string[] itemString = Regex.Split(line, ",");
                ItemInfo.PhysicalObjectItem item;

                if (itemString[0] == "Trap")
                {
                    trapQueue.Enqueue(new TrapsHandler.Trap(itemString[1]));
                    continue;
                }

                if (itemString[0] == nameof(DataPearl.AbstractDataPearl.DataPearlType))
                {
                    item = ItemInfo.IDToItem(itemString[1], true);
                }
                else if (itemString[0] == nameof(AbstractPhysicalObject.AbstractObjectType))
                {
                    item = ItemInfo.IDToItem(itemString[1]);
                }
                else
                {
                    Plugin.Log.LogError($"Encountered error in LoadItemQueue:\n\t'{itemString[0]}' is not a valid type");
                    continue;
                }

                itemQueue.Enqueue(item);
            }

            return (itemQueue, trapQueue);
        }

        [Obsolete("Unsafe when RandoManager is null, which is the only case where it is useful")]
        public static int CountRedsCycles(int saveSlot)
        {
            if (!IsThereASavedGame(SlugcatStats.Name.Red, saveSlot))
            {
                return -1;
            }

            Dictionary<string, ItemInfo> game = LoadSavedGame(SlugcatStats.Name.Red, saveSlot);
            return game.Values.Where(u => u.Type == ItemInfo.ItemInfoType.HunterCycles && u.IsGiven).Count();
        }

        #region Archipelago save data

        const string SCOUTED_LOCS_KEY = "RANDOMIZER_SCOUTED_LOCS";
        private static Dictionary<string, ItemFlags> _scoutedLocations = null;
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

        public struct APSave(long lastIndex, Dictionary<string, bool> locationsStatus)
        {
            public long lastIndex = lastIndex;
            public Dictionary<string, bool> locationsStatus = locationsStatus;
        }

        // AP saves store the found locations under a save ID, which is a string of pattern "[Generation Seed]_[Player Name]"
        public static bool IsThereAnAPSave(string saveId)
        {
            return File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json"));
        }

        public static void WriteAPSaveToFile(string saveId, long lastIndex, List<LocationInfo> locations)
        {
            if (locations == null || locations.Count == 0) return;

            APSave save = new(lastIndex, locations.ToDictionary(l => l.internalName, l => l.Collected));

            StreamWriter saveFile = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json"));

            string jsonSave = JsonConvert.SerializeObject(save, Formatting.Indented);
            saveFile.Write(jsonSave);

            saveFile.Close();
        }

        public static APSave LoadAPSave(string saveId)
        {
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json");

            if (!File.Exists(path))
            {
                Plugin.Log.LogError($"Failed to load save from file: ap_save_{saveId}.json");
                return new APSave();
            }

            return JsonConvert.DeserializeObject<APSave>(File.ReadAllText(path));
        }

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
        #endregion
    }
}