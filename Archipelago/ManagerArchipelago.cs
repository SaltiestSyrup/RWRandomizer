using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RainWorldRandomizer
{
    public class ManagerArchipelago : ManagerBase
    {
        public bool isNewGame = true;
        public bool locationsLoaded = false;
        public bool gameCompleted = false;

        public static Dictionary<string, string> NameToAPLocation = new Dictionary<string, string>();
        public static Dictionary<string, string> APLocationToName = new Dictionary<string, string>();
        public static Dictionary<string, string> NameToAPItem = new Dictionary<string, string>();
        public static Dictionary<string, string> APItemToName = new Dictionary<string, string>();
        internal Dictionary<string, bool> locationsStatus = new Dictionary<string, bool>();

        public void Init()
        {
            if (NameToAPLocation.Count == 0) LoadAPLocationDicts();
        }

        public override void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved)
        {
            isNewGame = !continueSaved;

            if (!(ArchipelagoConnection.Session?.Socket.Connected ?? false))
            {
                Plugin.Log.LogError("Tried to start AP campaign without first connecting to server");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText("Archipelago failed to start: Not connected to a server", UnityEngine.Color.red));
            }

            base.StartNewGameSession(storyGameCharacter, continueSaved);

            // Verify slugcat
            if (storyGameCharacter != ArchipelagoConnection.Slugcat)
            {
                Plugin.Log.LogError("Selected campaign does not match archipelago options." +
                    $"\n Chosen campaign: {storyGameCharacter}" +
                    $"\n Chosen AP option: {ArchipelagoConnection.Slugcat}");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText("Archipelago failed to start: Selected campaign does not match archipelago options.", UnityEngine.Color.red));
                return;
            }

            // Attempt initialization
            if (!InitializeSession(storyGameCharacter))
            {
                Plugin.Log.LogError("Failed to initialize randomizer.");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText($"Randomizer failed to initialize. Check logs for details.", UnityEngine.Color.red));
                return;
            }

            isRandomizerActive = true;
            // All good, randomizer active
        }

        public void Reset()
        {
            // Reset all tracking variables
            _currentMaxKarma = 4;
            _hunterBonusCyclesGiven = 0;
            _givenNeuronGlow = false;
            _givenMark = false;
            _givenRobo = false;
            _givenPebblesOff = false;
            _givenSpearPearlRewrite = false;
            customStartDen = "";
            gameCompleted = false;
            locationsLoaded = false;

            // Reset unlock lists
            gatesStatus.Clear();
            passageTokensStatus.Clear();
            Plugin.Singleton.itemDeliveryQueue.Clear();
            Plugin.Singleton.lastItemDeliveryQueue.Clear();

            // Clear notifications
            Plugin.Singleton.notifQueue.Clear();
        }

        public void LoadSave(string saveId)
        {
            SaveManager.APSave save = SaveManager.LoadAPSave(saveId);
            ArchipelagoConnection.lastItemIndex = save.lastIndex;
            locationsStatus = save.locationsStatus;
            currentSlugcat = ArchipelagoConnection.Slugcat;

            // Set locations the server says we found
            foreach (long locID in ArchipelagoConnection.Session.Locations.AllLocationsChecked)
            {
                string loc = GetLocStringIDFromID(locID);
                if (locationsStatus.TryGetValue(loc, out bool found) && !found)
                {
                    locationsStatus[loc] = true;
                }
            }

            // Release checks we found while offline
            List<long> offlineLocs = new List<long>();
            foreach (long locID in ArchipelagoConnection.Session.Locations.AllMissingLocations)
            {
                string loc = GetLocStringIDFromID(locID);
                if (locationsStatus.TryGetValue(loc, out bool found) && found)
                {
                    offlineLocs.Add(locID);
                }
            }
            if (offlineLocs.Count > 0)
            {
                ArchipelagoConnection.Session.Locations.CompleteLocationChecks(offlineLocs.ToArray());
                Plugin.Log.LogInfo($"Sent offline locations: {string.Join(", ", offlineLocs)}");
            }

            // Load the item delivery queue from file as normal
            Plugin.Singleton.itemDeliveryQueue = SaveManager.LoadItemQueue(ArchipelagoConnection.Slugcat, Plugin.Singleton.rainWorld.options.saveSlot);
            Plugin.Singleton.lastItemDeliveryQueue = new Queue<Unlock.Item>(Plugin.Singleton.itemDeliveryQueue);

            Plugin.Log.LogInfo($"Loaded save game {saveId}");
            locationsLoaded = true;
        }

        public void CreateNewSave(string saveId)
        {
            isNewGame = true;
            currentSlugcat = ArchipelagoConnection.Slugcat;

            locationsStatus.Clear();
            foreach (long loc in ArchipelagoConnection.Session.Locations.AllLocations)
            {
                if (GetLocStringIDFromID(loc) == null)
                {
                    Plugin.Log.LogError($"Location {loc} does not exist in DataPackage");
                    continue;
                }
                locationsStatus.Add(GetLocStringIDFromID(loc), false);
            }
            Plugin.Log.LogInfo($"Found no saved game, creating new save");
            SaveManager.WriteAPSaveToFile(saveId, ArchipelagoConnection.lastItemIndex, locationsStatus);

            locationsLoaded = true;
        }

        public void InitNewInventory(List<string> newItems)
        {
            _currentMaxKarma = 0;
            _hunterBonusCyclesGiven = 0;
            _givenNeuronGlow = false;
            _givenMark = false;
            _givenRobo = false;
            _givenPebblesOff = false;
            _givenSpearPearlRewrite = false;

            foreach (string item in newItems)
            {
                AquireItem(item, false, false);
            }
        }

        public void AquireItem(string item, bool printLog = true, bool isNew = true)
        {
            // If this doesn't pass it will just try to parse the AP name
            if (APItemToName.ContainsKey(item))
            {
                item = APItemToName[item];
            }

            if (item.StartsWith("GATE_"))
            {
                if (!gatesStatus.ContainsKey(item))
                {
                    gatesStatus.Add(item, true);
                }
            }
            else if (item.StartsWith("Passage-"))
            {
                WinState.EndgameID endgameId = new WinState.EndgameID(item.Substring(8));
                if (!passageTokensStatus.ContainsKey(endgameId))
                {
                    passageTokensStatus.Add(endgameId, true);
                }
            }
            else if (item.StartsWith("Object-"))
            {
                if (!isNew) return; // Don't double gift items, unused ones will be read from file
                Unlock unlock = new Unlock(Unlock.UnlockType.Item, Unlock.IDToItem(item.Substring(7)));
                unlock.GiveUnlock();
            }
            else if (item.StartsWith("PearlObject-"))
            {
                if (!isNew) return;
                Unlock unlock = new Unlock(Unlock.UnlockType.ItemPearl, Unlock.IDToItem(item.Substring(12), true));
                unlock.GiveUnlock();
            }
            else if (item.StartsWith("Trap-"))
            {
                if (!isNew) return;
                TrapsHandler.EnqueueTrap(item);
            }
            else if (item == "Karma")
            {
                IncreaseKarma();
            }
            else if (item == "The Glow")
            {
                _givenNeuronGlow = true;
                if (Plugin.Singleton.Game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.Game.GetStorySession.saveState.theGlow = true;
            }
            else if (item == "The Mark")
            {
                _givenMark = true;
                if (Plugin.Singleton.Game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.Game.GetStorySession.saveState.deathPersistentSaveData.theMark = true;
            }
            else if (item == "IdDrone")
            {
                _givenRobo = true;
                if (Plugin.Singleton.Game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.Game.GetStorySession.saveState.hasRobo = true;
            }
            else if (item == "Disconnect_FP")
            {
                _givenPebblesOff = true;
                if (Plugin.Singleton.Game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.Game.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = true;
            }
            else if (item == "Rewrite_Spear_Pearl")
            {
                _givenSpearPearlRewrite = true;
                if (Plugin.Singleton.Game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.Game.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = true;
            }
            else
            {
                WatcherIntegration.Items.ReceiveItem(item);
            }

            Plugin.Log.LogInfo($"Received item: {item}");
        }

        public bool InitializeSession(SlugcatStats.Name slugcat)
        {
            if (ArchipelagoConnection.useRandomStart)
            {
                customStartDen = ArchipelagoConnection.desiredStartDen;
                Plugin.Log.LogInfo($"Using randomized starting den: {customStartDen}");
            }
            else
            {
                customStartDen = Constants.SlugcatDefaultStartingDen[slugcat];
            }

            // Populate region mapping for display purposes
            foreach (string region in Region.GetFullRegionOrder())
            {
                if (!Plugin.ProperRegionMap.ContainsKey(region))
                    Plugin.ProperRegionMap.Add(region, Region.GetProperRegionAcronym(SlugcatStats.SlugcatToTimeline(slugcat), region));
            }

            return true;
        }

        /// <summary>
        /// Finds a random den in the given region
        /// </summary>
        public static string FindRandomStart(string selectedRegion)
        {
            Dictionary<string, List<string>> contenders = new Dictionary<string, List<string>>();
            if (File.Exists(AssetManager.ResolveFilePath($"chkrand_randomstarts.txt")))
            {
                string[] file = File.ReadAllLines(AssetManager.ResolveFilePath($"chkrand_randomstarts.txt"));
                foreach (string line in file)
                {
                    if (!line.StartsWith("//") && line.Length > 0)
                    {
                        string region = Regex.Split(line, "_")[0];
                        if (Region.GetFullRegionOrder().Contains(region))
                        {
                            if (!contenders.ContainsKey(region))
                            {
                                contenders.Add(region, new List<string>());
                            }
                            contenders[region].Add(line);
                        }
                    }
                }
                return contenders[selectedRegion][UnityEngine.Random.Range(0, contenders[selectedRegion].Count)];
            }

            return "SU_S01";
        }

        public override List<string> GetLocations()
        {
            return locationsStatus.Keys.ToList();
        }

        public override bool LocationExists(string location)
        {
            return locationsStatus.ContainsKey(location);
        }

        public override bool? IsLocationGiven(string location)
        {
            if (!LocationExists(location)) return null;

            return locationsStatus[location];
        }

        public override bool GiveLocation(string location)
        {
            if (IsLocationGiven(location) ?? true) return false;

            locationsStatus[location] = true;

            // We still gave the location, but we're offline so it can't be sent yet
            if (!(ArchipelagoConnection.Session?.Socket.Connected ?? false))
            {
                Plugin.Log.LogInfo($"Found location while offline: {location}");
                return true;
            }

            long locId = GetIDFromLocStringID(location);
            if (locId == -1L)
            {
                Plugin.Log.LogError($"Failed to find ID for location: {location}");
            }
            ArchipelagoConnection.Session.Locations.CompleteLocationChecks(locId);
            Plugin.Log.LogInfo($"Found location: {location}!");

            return true;
        }

        // This will have to ask the server to scout the location, which takes time.
        // Thankfully, the only place that uses this is the spoiler menu, which can be re-written for AP
        public override Unlock GetUnlockAtLocation(string location)
        {
            return null;
        }

        public void GiveCompletionCondition(ArchipelagoConnection.CompletionCondition condition)
        {
            if (condition != ArchipelagoConnection.completionCondition)
            {
                Plugin.Log.LogInfo("Game completed through the wrong condition, not sending completion");
                return;
            }

            gameCompleted = true;
            ArchipelagoConnection.SendCompletion();
            Plugin.Log.LogInfo("Game Complete!");
            Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText("Game Complete!", UnityEngine.Color.green));
        }

        public override void SaveGame(bool saveCurrentState)
        {
            if (saveCurrentState)
            {
                SaveManager.WriteItemQueueToFile(
                    Plugin.Singleton.itemDeliveryQueue,
                    currentSlugcat,
                    Plugin.Singleton.rainWorld.options.saveSlot);
            }

            // Don't save if locations are not loaded
            if (!locationsLoaded) return;

            // Set locations the server says we found
            if (ArchipelagoConnection.Session != null)
            {
                foreach (long locID in ArchipelagoConnection.Session.Locations.AllLocationsChecked)
                {
                    string loc = GetLocStringIDFromID(locID);
                    if (locationsStatus.TryGetValue(loc, out bool found) && !found)
                    {
                        locationsStatus[loc] = true;
                    }
                }
            }

            SaveManager.WriteAPSaveToFile(
                $"{ArchipelagoConnection.generationSeed}_{ArchipelagoConnection.playerName}",
                ArchipelagoConnection.lastItemIndex,
                locationsStatus);
        }

        private struct APReadableNames
        {
            public APReadableNames(Dictionary<string, string> locations, Dictionary<string, string> items)
            {
                this.locations = locations;
                this.items = items;
            }

            public Dictionary<string, string> locations;
            public Dictionary<string, string> items;
        }

        private static void LoadAPLocationDicts()
        {
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_names_map.json");

            if (!File.Exists(path))
            {
                Plugin.Log.LogError("AP locations map does not exist");
                return;
            }

            APReadableNames names = JsonConvert.DeserializeObject<APReadableNames>(File.ReadAllText(path));

            NameToAPLocation = names.locations;
            NameToAPItem = names.items;

            foreach (var keyVal in NameToAPLocation)
            {
                //Plugin.Log.LogDebug($"{keyVal.Key} | {keyVal.Value}");
                APLocationToName.Add(keyVal.Value, keyVal.Key);
            }
            foreach (var keyVal in NameToAPItem)
            {
                //Plugin.Log.LogDebug($"{keyVal.Key} | {keyVal.Value}");
                APItemToName.Add(keyVal.Value, keyVal.Key);
            }
        }

        public static string GetLocStringIDFromID(long locID)
        {
            if (APLocationToName.TryGetValue(ArchipelagoConnection.Session.Locations.GetLocationNameFromId(locID), out string stringID))
            {
                return stringID;
            }
            else
            {
                return ArchipelagoConnection.Session.Locations.GetLocationNameFromId(locID);
            }
        }

        public long GetIDFromLocStringID(string stringID)
        {
            if (NameToAPLocation.TryGetValue(stringID, out string loc))
            {
                return ArchipelagoConnection.Session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME_NAME, loc);
            }
            else
            {
                return ArchipelagoConnection.Session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME_NAME, stringID);
            }
        }
    }
}
