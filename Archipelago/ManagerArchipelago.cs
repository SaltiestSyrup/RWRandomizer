using Newtonsoft.Json;
using RainWorldRandomizer.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainWorldRandomizer
{
    public class ManagerArchipelago : ManagerBase
    {
        public bool locationsLoaded = false;
        public bool gameCompleted = false;

        // Mapping AP item names to the string IDs the mod uses for items
        public static Dictionary<string, string> ClientNameToAPItem = [];
        public static Dictionary<string, string> APItemToClientName = [];
        // Mapping numerical AP location IDs to the string IDs the mod uses for locations
        public static Dictionary<string, long> LocationToID = [];
        public static Dictionary<long, string> IDToLocation = [];

        internal Dictionary<string, bool> locationsStatus = [];

        public override void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved)
        {
            if (!ArchipelagoConnection.SocketConnected)
            {
                Plugin.Log.LogError("Tried to start AP campaign without first connecting to server");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText("Archipelago failed to start: Not connected to a server", UnityEngine.Color.red));
            }

            base.StartNewGameSession(storyGameCharacter, continueSaved);
            LoadAPLocationDicts();

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

            // Load save file
            string saveId = $"{ArchipelagoConnection.generationSeed}_{ArchipelagoConnection.playerName}";
            try
            {
                // TODO: There should be some validation that the save game being loaded is the correct one for the AP slot
                // Probably do this when integrating data into save file. Then we can mine save data to find if player is loading the correct save
                if (SaveManager.IsThereAnAPSave(saveId)) LoadSave(saveId);
                else CreateNewSave(saveId);
            }
            catch (Exception e) { Plugin.Log.LogError(e); }

            // Ask for fresh items list if there isn't one waiting
            if (!ArchipelagoConnection.waitingItemPackets.Any(p => p.Index == 0))
            {
                ArchipelagoConnection.SendSyncPacket();
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

        /// <summary>
        /// Soft reset, clears all items in preperation for a new inventory
        /// </summary>
        public void ResetStateForSync()
        {
            _currentMaxKarma = 0;
            _hunterBonusCyclesGiven = 0;
            _numDamageUpgrades = 0;
            _numMovementUpgrades = 0;
            _givenNeuronGlow = false;
            _givenMark = false;
            _givenRobo = false;
            _givenPebblesOff = false;
            _givenSpearPearlRewrite = false;

            gatesStatus.Clear();
            passageTokensStatus.Clear();
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
                if (IDToLocation.TryGetValue(locID, out string loc)
                    && locationsStatus.TryGetValue(loc, out bool found)
                    && !found)
                {
                    locationsStatus[loc] = true;
                }
            }

            // Release checks we found while offline
            List<long> offlineLocs = [];
            foreach (long locID in ArchipelagoConnection.Session.Locations.AllMissingLocations)
            {
                if (IDToLocation.TryGetValue(locID, out string loc)
                    && locationsStatus.TryGetValue(loc, out bool found)
                    && found)
                {
                    offlineLocs.Add(locID);
                }
            }
            if (offlineLocs.Count > 0)
            {
                ArchipelagoConnection.Session.Locations.CompleteLocationChecks([.. offlineLocs]);
                Plugin.Log.LogInfo($"Sent offline locations: {string.Join(", ", offlineLocs)}");
            }

            // Load the item delivery queue from file as normal
            (itemDeliveryQueue, pendingTrapQueue) = SaveManager.LoadItemQueue(ArchipelagoConnection.Slugcat, Plugin.Singleton.rainWorld.options.saveSlot);
            lastItemDeliveryQueue = new(itemDeliveryQueue);

            Plugin.Log.LogInfo($"Loaded save game {saveId}");
            locationsLoaded = true;
        }

        public void CreateNewSave(string saveId)
        {
            currentSlugcat = ArchipelagoConnection.Slugcat;
            locationsStatus.Clear();
            Plugin.Log.LogInfo($"Found no saved game, creating new save");

            if (IDToLocation.Count == 0)
            {
                Plugin.Log.LogError("Cannot create Archipelago save game without datapackage");
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText(
                    $"Failed to create new Archipelago save, no datapackage is present", UnityEngine.Color.red));
                return;
            }

            foreach (long locID in ArchipelagoConnection.Session.Locations.AllLocations)
            {
                if (!IDToLocation.TryGetValue(locID, out string loc))
                {
                    Plugin.Log.LogError($"Error writing save, location {locID} does not exist in DataPackage");
                    continue;
                }
                locationsStatus.Add(loc, false);
            }
            
            locationsLoaded = true;
            SaveGame(false);
        }

        public void TryAquireNextItemPacket()
        {
            if (ArchipelagoConnection.waitingItemPackets.Count == 0) return;

            Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket itemPacket = ArchipelagoConnection.waitingItemPackets.Dequeue();

            Plugin.Log.LogInfo($"Received items packet. Index: {itemPacket.Index} | Last index: {ArchipelagoConnection.lastItemIndex} | Item count: {itemPacket.Items.Length}");

            bool isNewInventory = false;
            if (itemPacket.Index == 0)
            {
                ResetStateForSync();
                isNewInventory = true;
            }
            // Multiclient sends Sync packet for us, the new inventory should arrive soon
            else if (itemPacket.Index < ArchipelagoConnection.lastItemIndex)
            {
                // Could happen if host save file was reverted to a previous state.
                // If that happens, "new" consumable items that are before our last stored ID would be lost.
                // This is an edge case though, and there are no progression consumables so it isn't worth the effort to fix.
                Plugin.Log.LogWarning($"New item index is lower than last index. Server is confused?");
                return;
            }
            else if (itemPacket.Index > ArchipelagoConnection.lastItemIndex)
            {
                Plugin.Log.LogWarning($"New item index is greater than last index. Missed an item?");
                return;
            }

            for (int i = 0; i < itemPacket.Items.Length; i++)
            {
                string APItemName = ArchipelagoConnection.Session.Items.GetItemName(itemPacket.Items[i].Item);
                // Even if item has no client map, try to give it anyway, maybe it works
                if (!APItemToClientName.TryGetValue(APItemName, out string clientName)) Plugin.Log.LogError($"Could not find client name mapping for AP item: {APItemName}");

                // If item packet index is 0, items before our stored last index are not new
                AquireItem(clientName, !isNewInventory || i >= ArchipelagoConnection.lastItemIndex);
            }

            ArchipelagoConnection.lastItemIndex = itemPacket.Index + itemPacket.Items.Length;
        }

        /// <summary>
        /// Takes in a client item name and awards it to the player.
        /// </summary>
        /// <param name="item">Client name of item</param>
        /// <param name="isNew">If false, if the item is a consumable it will be ignored</param>
        public void AquireItem(string item, bool isNew)
        {
            if (item.StartsWith("GATE_"))
            {
                if (!gatesStatus.ContainsKey(item))
                {
                    gatesStatus.Add(item, true);
                }
            }
            else if (item.StartsWith("Passage-"))
            {
                WinState.EndgameID endgameId = new(item.Substring(8));
                if (!passageTokensStatus.ContainsKey(endgameId))
                {
                    passageTokensStatus.Add(endgameId, true);
                }
            }
            else if (item.StartsWith("Object-"))
            {
                if (!isNew) return; // Don't double gift items, unused ones will be read from file
                Unlock unlock = new(Unlock.UnlockType.Item, Unlock.IDToItem(item[7..]));
                unlock.GiveUnlock();
            }
            else if (item.StartsWith("PearlObject-"))
            {
                if (!isNew) return;
                Unlock unlock = new(Unlock.UnlockType.ItemPearl, Unlock.IDToItem(item[12..], true));
                unlock.GiveUnlock();
            }
            else if (item.StartsWith("Trap-"))
            {
                if (!isNew) return;
                TrapsHandler.EnqueueTrap(item);
            }
            else
            {
                switch (item)
                {
                    case "Karma":
                        IncreaseKarma();
                        break;
                    case "Upgrade-SpearDamage":
                        _numDamageUpgrades++;
                        break;
                    case "The Glow":
                        _givenNeuronGlow = true;
                        if (Plugin.Singleton.Game?.GetStorySession?.saveState is not null)
                            Plugin.Singleton.Game.GetStorySession.saveState.theGlow = true;
                        break;
                    case "The Mark":
                        _givenMark = true;
                        if (Plugin.Singleton.Game?.GetStorySession?.saveState is not null)
                            Plugin.Singleton.Game.GetStorySession.saveState.deathPersistentSaveData.theMark = true;
                        break;
                    case "IdDrone":
                        _givenRobo = true;
                        if (Plugin.Singleton.Game?.GetStorySession?.saveState is not null)
                            Plugin.Singleton.Game.GetStorySession.saveState.hasRobo = true;
                        break;
                    case "Disconnect_FP":
                        _givenPebblesOff = true;
                        if (Plugin.Singleton.Game?.GetStorySession?.saveState is not null)
                            Plugin.Singleton.Game.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = true;
                        break;
                    case "Rewrite_Spear_Pearl":
                        _givenSpearPearlRewrite = true;
                        if (Plugin.Singleton.Game?.GetStorySession?.saveState is not null)
                            Plugin.Singleton.Game.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = true;
                        break;
                }
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

        public override List<string> GetLocations()
        {
            return [.. locationsStatus.Keys];
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
            if (IsLocationGiven(location) is null or true) return false;

            locationsStatus[location] = true;

            if (!LocationToID.TryGetValue(location, out long locID))
            {
                Plugin.Log.LogError($"Failed to find ID for found location: {location}");
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText(
                    $"Checked \"{location}\""));
                return true;
            }

            // We still gave the location, but we're offline so it can't be sent yet
            if (!ArchipelagoConnection.SocketConnected)
            {
                Plugin.Log.LogInfo($"Found location while offline: {location}");
                string logName = ArchipelagoConnection.Session?.Locations.GetLocationNameFromId(locID) ?? location;
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText(
                    $"Checked \"{logName}\""));
                return true;
            }

            ArchipelagoConnection.Session.Locations.CompleteLocationChecks(locID);
            Plugin.Log.LogInfo($"Found location: {location}!");

            return true;
        }

        // This will have to ask the server to scout the location, which takes time.
        // Thankfully, the only place that uses this is the spoiler menu, which can be re-written for AP
        public override Unlock GetUnlockAtLocation(string location) => null;

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
            SaveManager.WriteItemQueueToFile(
                saveCurrentState ? itemDeliveryQueue : lastItemDeliveryQueue,
                pendingTrapQueue,
                currentSlugcat,
                Plugin.Singleton.rainWorld.options.saveSlot);

            // Don't save if locations are not loaded
            if (!locationsLoaded) return;

            // Set locations the server says we found
            if (ArchipelagoConnection.Session is not null)
            {
                foreach (long locID in ArchipelagoConnection.Session.Locations.AllLocationsChecked)
                {
                    if (IDToLocation.TryGetValue(locID, out string loc)
                        && locationsStatus.TryGetValue(loc, out bool found)
                        && !found)
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

        private struct APReadableNames(Dictionary<string, string> locations, Dictionary<string, string> items)
        {
            public Dictionary<string, string> locations = locations;
            public Dictionary<string, string> items = items;
        }

        public static void LoadAPLocationDicts()
        {
            string path = Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath, $"ap_names_map.json");

            if (!File.Exists(path))
            {
                Plugin.Log.LogError("AP locations map does not exist");
                return;
            }

            IDToLocation.Clear();
            LocationToID.Clear();
            ClientNameToAPItem.Clear();
            APItemToClientName.Clear();

            APReadableNames names = JsonConvert.DeserializeObject<APReadableNames>(File.ReadAllText(path));

            // Create alternate datapackage with client names
            try
            {
                IDToLocation = names.locations.Keys.ToDictionary((clientName)
                    => ArchipelagoConnection.Session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME_NAME, names.locations[clientName]));
                foreach (var kvp in IDToLocation) LocationToID.Add(kvp.Value, kvp.Key);
            }
            catch (ArgumentException e)
            {
                // Argument exception happens when GetLocationIdFromName() returns the same ID (-1) multiple times
                Plugin.Log.LogError("Failed to load datapackage location IDs. Datapackage is either missing or doesn't match this client version's location names.");
            }

            // Create translation from AP item names to client ones.
            // Would prefer to map to the numerical AP item IDs, but MultiClient doesn't provide an easy way to convert item names to ID
            ClientNameToAPItem = names.items;
            foreach (var kvp in ClientNameToAPItem) APItemToClientName.Add(kvp.Value, kvp.Key);
        }
    }
}
