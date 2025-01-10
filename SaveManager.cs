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

        //TODO: make this async if possible
        public static void WriteSavedGameToFile(Dictionary<string, Unlock> game, SlugcatStats.Name slugcat, int saveSlot)
        {
            StreamWriter file = File.CreateText(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));

            file.WriteLine(Generation.currentSeed);
            foreach (var item in game)
            {
                string check = item.Key;
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

        public static Dictionary<string, Unlock> LoadSavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            Dictionary<string, Unlock> game = new Dictionary<string, Unlock>();

            if (IsThereALegacySavedGame(slugcat, saveSlot))
            {
                return DeserializeGameFromJson(slugcat, saveSlot);
            }

            string[] file = File.ReadAllLines(Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"saved_game_{slugcat.value}_{saveSlot}.txt"));

            Generation.currentSeed = int.Parse(file[0]);
            file = file.Skip(1).ToArray();
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
    }
}
