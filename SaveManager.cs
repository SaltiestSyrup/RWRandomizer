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
        public static void WriteSavedGameToFile(Dictionary<string, Unlock> game, SlugcatStats.Name slugcat, int saveSlot)
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
        public static Dictionary<string, Unlock> LoadSavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            Dictionary<string, Unlock> game = [];

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

                Unlock.UnlockType type = Unlock.UnlockType.Item;
                if (int.TryParse(unlockString[0], out int typeIndex))
                {
                    type = Unlock.UnlockType.typeOrder[typeIndex];
                }
                else if (ExtEnumBase.TryParse(typeof(Unlock.UnlockType), unlockString[0], true, out ExtEnumBase t))
                {
                    type = (Unlock.UnlockType)t;
                }

                Unlock unlock = new(
                    type,
                    unlockString[1],
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
            Queue<Unlock.Item> itemQueue = [];

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
                }
                else if (itemString[0] == nameof(AbstractPhysicalObject.AbstractObjectType))
                {
                    item = Unlock.IDToItem(itemString[1]);
                }
                else
                {
                    Plugin.Log.LogError($"Encountered error in LoadItemQueue:\n\t'{itemString[0]}' is not a valid type");
                    continue;
                }

                itemQueue.Enqueue(item);
            }

            return itemQueue;
        }

        [Obsolete("Unsafe when RandoManager is null, which is the only case where it is useful")]
        public static int CountRedsCycles(int saveSlot)
        {
            if (!IsThereASavedGame(SlugcatStats.Name.Red, saveSlot))
            {
                return -1;
            }

            Dictionary<string, Unlock> game = LoadSavedGame(SlugcatStats.Name.Red, saveSlot);
            return game.Values.Where(u => u.Type == Unlock.UnlockType.HunterCycles && u.IsGiven).Count();
        }

        #region Archipelago save data
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

        public static void WriteAPSaveToFile(string saveId, long lastIndex, Dictionary<string, bool> locationsStatus)
        {
            if (locationsStatus == null || locationsStatus.Count == 0) return;

            StreamWriter saveFile = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_save_{saveId}.json"));

            APSave save = new(lastIndex, locationsStatus);

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