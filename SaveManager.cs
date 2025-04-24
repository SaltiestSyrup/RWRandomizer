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
            return File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"))
                || IsThereALegacySavedGame(slugcat, saveSlot);
        }

        public static bool IsThereALegacySavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            return File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.json"));
        }

        // Meant for vanilla saves only
        public static void WriteSavedGameToFile(Dictionary<string, Unlock> game, SlugcatStats.Name slugcat, int saveSlot)
        {
            StreamWriter file = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));

            file.WriteLine($"StartingDen->{Plugin.RandoManager.customStartDen}");
            file.WriteLine(Plugin.RandoManager.currentSeed);
            foreach (var item in game)
            {
                string serializedUnlock = $"{{{(int)item.Value.Type},{item.Value.ID},{item.Value.IsGiven}}}";

                file.Write($"{item.Key}->{serializedUnlock}");
                file.WriteLine();
            }

            file.Close();

            // If this game was loaded from a legacy save, delete it now
            if (IsThereALegacySavedGame(slugcat, saveSlot))
            {
                File.Delete(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.json"));
            }
        }

        // Meant for vanilla saves only
        public static Dictionary<string, Unlock> LoadSavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            Dictionary<string, Unlock> game = new Dictionary<string, Unlock>();

            if (IsThereALegacySavedGame(slugcat, saveSlot))
            {
                return DeserializeGameFromJson(slugcat, saveSlot);
            }

            string[] file = File.ReadAllLines(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));

            if (int.TryParse(file[0], out int seed))
            {
                Plugin.RandoManager.currentSeed = seed;
                file = file.Skip(1).ToArray();
            }
            else
            {
                // New save file includes additional line to store starting den
                Plugin.RandoManager.customStartDen = Regex.Split(file[0], "->")[1]; // StartingDen->SU_S01
                Plugin.RandoManager.currentSeed = int.Parse(file[1]);
                file = file.Skip(2).ToArray();
            }
            
            foreach (string line in file)
            {
                string[] keyValue = Regex.Split(line, "->");

                string[] unlockString = Regex.Split(keyValue[1]
                    .TrimStart('{')
                    .TrimEnd('}'), ",");

                Unlock unlock = new Unlock(
                    (Unlock.UnlockType)int.Parse(unlockString[0]),
                    unlockString[1],
                    bool.Parse(unlockString[2]));

                game.Add(keyValue[0], unlock);
            }

            return game;
        }

        // Load game from legacy .json format
        private static Dictionary<string, Unlock> DeserializeGameFromJson(SlugcatStats.Name slugcat, int saveSlot)
        {
            Dictionary<string, Unlock> game = new Dictionary<string, Unlock>();

            string[] json = File.ReadAllLines(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.json"));

            foreach (string line in json)
            {
                string[] keyValue = Regex.Split(line
                    .Replace("\\u0022", "")
                    .TrimStart("[\"".ToCharArray())
                    .TrimEnd("\"]".ToCharArray()), "\",\"");

                string[] unlockString = keyValue[1]
                    .Replace("ID:", "")
                    .Replace("IsGiven:", "")
                    .Replace("Type:", "")
                    .TrimStart('{')
                    .TrimEnd('}')
                    .Split(',');

                Unlock unlock = new Unlock(
                    (Unlock.UnlockType)int.Parse(unlockString[1]),
                    unlockString[0],
                    bool.Parse(unlockString[2]));

                game.Add(keyValue[0], unlock);
            }

            return game;
        }

        public static void WriteItemQueueToFile(Queue<Unlock.Item> itemQueue, SlugcatStats.Name slugcat, int saveSlot)
        {
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt");

            // If there is nothing to store, delete any stored data
            if (itemQueue.Count == 0)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return;
            }

            StreamWriter file = File.CreateText(path);

            foreach (Unlock.Item item in itemQueue)
            {
                file.WriteLine($"{item.type.enumType.Name},{item.id}");
            }

            file.Close();
        }

        public static Queue<Unlock.Item> LoadItemQueue(SlugcatStats.Name slugcat, int saveSlot)
        {
            Queue<Unlock.Item> itemQueue = new Queue<Unlock.Item>();

            if (!File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt")))
                return itemQueue;

            string[] text = File.ReadAllLines(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"item_delivery_{slugcat.value}_{saveSlot}.txt"));

            foreach (string line in text)
            {
                string[] itemString = Regex.Split(line, ",");
                Unlock.Item item;

                if (itemString[0] == nameof(DataPearl.AbstractDataPearl.DataPearlType))
                {
                    item = Unlock.IDToItem(itemString[1], true);
                    //item = new Unlock.Item(itemString[2], new DataPearl.AbstractDataPearl.DataPearlType(itemString[1]));
                }
                else if (itemString[0] == nameof(AbstractPhysicalObject.AbstractObjectType))
                {
                    item = Unlock.IDToItem(itemString[1]);
                    //item = new Unlock.Item(itemString[2], new AbstractPhysicalObject.AbstractObjectType(itemString[1]));
                }
                else
                {
                    Plugin.Log.LogError($"Encountered error in LoadItemQueue:\n\t'{itemString[0]}' is not a valid type");
                    continue;
                }

                //if (itemString.Length >= 4)
                //{
                //    item.id = itemString[3];
                //}

                itemQueue.Enqueue(item);
            }

            return itemQueue;
        }

        public static int CountRedsCycles(int saveSlot)
        {
            if (!IsThereASavedGame(SlugcatStats.Name.Red, saveSlot))
            {
                return -1;
            }

            Dictionary<string, Unlock> game = LoadSavedGame(SlugcatStats.Name.Red, saveSlot);
            return game.Values.Where(u => u.Type == Unlock.UnlockType.HunterCycles && u.IsGiven).Count();
        }

        #region Archipelago saved data
        // Fetch locally saved checksum
        /*
        public static string GetDataPackageChecksum()
        {
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, "ap_datapackage_checksum.txt");
            if (!File.Exists(path))
            {
                return "";
            }

            string[] file = File.ReadAllLines(path);

            return file.Length > 0 ? file[0] : "";
        }
        
        public static void WriteDataPackageToFile(Dictionary<string, long> itemLookup, Dictionary<string, long> locationLookup, string checksum)
        {
            Plugin.Log.LogDebug("Writing Data package files...");
            StreamWriter itemsFile = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, "ap_datapackage_items.txt"));

            foreach (var item in itemLookup)
            {
                itemsFile.Write($"{item.Key}->{item.Value}");
                itemsFile.WriteLine();
            }
            itemsFile.Close();

            StreamWriter locationsFile = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, "ap_datapackage_locations.txt"));

            foreach (var item in locationLookup)
            {
                locationsFile.Write($"{item.Key}->{item.Value}");
                locationsFile.WriteLine();
            }
            locationsFile.Close();

            StreamWriter checksumFile = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, "ap_datapackage_checksum.txt"));
            checksumFile.Write(checksum);
            checksumFile.Close();

            Plugin.Log.LogDebug("DataPackage file write complete");
        }

        public static bool LoadDataPackage(out Dictionary<string, long> itemLookup, out Dictionary<string, long> locationLookup)
        {
            itemLookup = new Dictionary<string, long>();
            locationLookup = new Dictionary<string, long>();

            string itemsPath = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, "ap_datapackage_items.txt");
            string locationsPath = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, "ap_datapackage_locations.txt");

            if (!(File.Exists(itemsPath) && File.Exists(locationsPath)))
            {
                return false;
            }

            string[] itemsFile = File.ReadAllLines(itemsPath);
            foreach (string line in itemsFile)
            {
                string[] keyValue = Regex.Split(line, "->");
                itemLookup.Add(keyValue[0], long.Parse(keyValue[1]));
            }

            string[] locationsFile = File.ReadAllLines(locationsPath);
            foreach (string line in locationsFile)
            {
                string[] keyValue = Regex.Split(line, "->");
                locationLookup.Add(keyValue[0], long.Parse(keyValue[1]));
            }

            return true;
        }
        */

        public struct APSave
        {
            public APSave(long lastIndex, Dictionary<string, bool> locationsStatus)
            {
                this.lastIndex = lastIndex;
                this.locationsStatus = locationsStatus;
            }

            public long lastIndex;
            public Dictionary<string, bool> locationsStatus;
        }

        // AP saves store the found locations under a save ID, which is a string of pattern "[Generation Seed]_[Player Name]"
        public static bool IsThereAnAPSave(string saveId)
        {
            return File.Exists(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json"));
        }

        public static void WriteAPSaveToFile(string saveId, long lastIndex, Dictionary<string, bool> locationsStatus)
        {
            if (locationsStatus == null || locationsStatus.Count == 0) return;

            StreamWriter saveFile = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json"));

            APSave save = new APSave(lastIndex, locationsStatus);

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

        public static void WriteLastItemIndexToFile(string saveId, long lastIndex)
        {
            Dictionary<string, long> origRegistry = LoadLastItemIndices();
            if (origRegistry.ContainsKey(saveId))
            {
                origRegistry[saveId] = lastIndex;
            }
            else
            {
                origRegistry.Add(saveId, lastIndex);
            }

            StreamWriter indexFile = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, "ap_save_registry.txt"));

            indexFile.Write(JsonConvert.SerializeObject(origRegistry));
            indexFile.Close();
        }

        public static Dictionary<string, long> LoadLastItemIndices()
        {
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, "ap_save_registry.txt");
            if (!File.Exists(path))
            {
                return new Dictionary<string, long>();
            }

            return JsonConvert.DeserializeObject<Dictionary<string, long>>(File.ReadAllText(path)) ?? new Dictionary<string, long>();
        }

        #endregion
    }
}
