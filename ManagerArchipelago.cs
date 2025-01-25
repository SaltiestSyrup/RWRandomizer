using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class ManagerArchipelago : ManagerBase
    {
        // AP TODO:
        // Stop player from starting game without first connecting
        // Auto connect on startup??
        // Console logging
        // make objects actaully give to player
        // remove Hunter time limit

        public bool isPopulated = false;
        public bool locationsLoaded = false;

        private Dictionary<string, bool> locationsStatus = new Dictionary<string, bool>();

        public override void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved)
        {
            if (!ArchipelagoConnection.IsConnected)
            {
                Plugin.Log.LogError("Tried to start AP campaign without first connecting to server");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue("Archipelago failed to start: Not connected to a server");
            }

            base.StartNewGameSession(storyGameCharacter, continueSaved);

            // Verify slugcat
            if (storyGameCharacter != ArchipelagoConnection.Slugcat)
            {
                Plugin.Log.LogError("Selected campaign does not match archipelago options." +
                    $"\n Chosen campaign: {storyGameCharacter}" +
                    $"\n Chosen AP option: {ArchipelagoConnection.Slugcat}");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue("Archipelago failed to start: Selected campaign does not match archipelago options.");
                return;
            }

            // Attempt initialization
            if (!InitializeSession(storyGameCharacter))
            {
                Plugin.Log.LogError("Failed to initialize randomizer.");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue($"Randomizer failed to initialize. Check logs for details.");
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
            customStartDen = "SU_S01";

            // Reset unlock lists
            gatesStatus.Clear();
            passageTokensStatus.Clear();
        }

        public void Populate()
        {
            if (isPopulated) return;
            Reset();

            foreach (var item in ArchipelagoConnection.ItemNameToID.Keys)
            {
                if (item.StartsWith("GATE_") && !gatesStatus.ContainsKey(item))
                {
                    gatesStatus.Add(item, false);
                }

                if (item.StartsWith("Passage-") && !passageTokensStatus.ContainsKey(new WinState.EndgameID(item.Substring(8))))
                {
                    passageTokensStatus.Add(new WinState.EndgameID(item.Substring(8)), false);
                }
            }
            isPopulated = true;
        }

        public void LoadSave(string saveId)
        {
            SaveManager.APSave save = SaveManager.LoadAPSave(saveId);
            ArchipelagoConnection.lastItemIndex = save.lastIndex;
            locationsStatus = save.locationsStatus;
            Plugin.Log.LogInfo($"Loaded save game {saveId}");

            locationsLoaded = true;
        }

        public void CreateNewSave(string saveId)
        {
            locationsStatus.Clear();
            foreach (var loc in ArchipelagoConnection.LocationNameToID.Keys)
            {
                locationsStatus.Add(loc, false);
            }
            Plugin.Log.LogInfo($"Found no saved game, creating new save");
            SaveManager.WriteAPSaveToFile(saveId, ArchipelagoConnection.lastItemIndex, locationsStatus);

            locationsLoaded = true;
        }

        public void InitNewInventory(List<string> newItems)
        {
            _currentMaxKarma = 4;
            _hunterBonusCyclesGiven = 0;
            _givenNeuronGlow = false;
            _givenMark = false;
            _givenRobo = false;
            _givenPebblesOff = false;
            _givenSpearPearlRewrite = false;

            List<string> gates = gatesStatus.Keys.ToList();
            foreach (string gate in gates)
            {
                gatesStatus[gate] = false;
            }
            List<WinState.EndgameID> tokens = passageTokensStatus.Keys.ToList();
            foreach (WinState.EndgameID token in tokens)
            {
                passageTokensStatus[token] = false;
            }

            foreach (string item in newItems)
            {
                AquireItem(item, false, false);
            }
        }

        public void AquireItem(string item, bool printLog = true, bool isNew = true)
        {
            Unlock unlock = null;

            if (item.StartsWith("GATE_"))
            {
                unlock = new Unlock(Unlock.UnlockType.Gate, item, true);
                gatesStatus[item] = true;
            }
            else if (item.StartsWith("Passage-"))
            {
                unlock = new Unlock(Unlock.UnlockType.Token, item, true);
                passageTokensStatus[new WinState.EndgameID(item.Substring(8))] = true;
            }
            else if (item.StartsWith("Object-"))
            {
                unlock = new Unlock(Unlock.UnlockType.Item, Unlock.IDToItem(item), true);
                unlock.GiveUnlock();
            }
            else if (item.StartsWith("PearlObject-"))
            {
                unlock = new Unlock(Unlock.UnlockType.ItemPearl, Unlock.IDToItem(item, true), true);
                unlock.GiveUnlock();
            }
            else if (item == "Karma")
            {
                unlock = new Unlock(Unlock.UnlockType.Karma, item, true);
                IncreaseKarma();
            }
            else if (item == "The_Glow")
            {
                unlock = new Unlock(Unlock.UnlockType.Glow, item, true);
                _givenNeuronGlow = true;
                if (Plugin.Singleton.game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.game.GetStorySession.saveState.theGlow = true;
            }
            else if (item == "The_Mark")
            {
                unlock = new Unlock(Unlock.UnlockType.Mark, item, true);
                _givenMark = true;
                if (Plugin.Singleton.game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.game.GetStorySession.saveState.deathPersistentSaveData.theMark = true;
            }
            else if (item == "IdDrone")
            {
                unlock = new Unlock(Unlock.UnlockType.IdDrone, item, true);
                _givenRobo = true;
                if (Plugin.Singleton.game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.game.GetStorySession.saveState.hasRobo = true;
            }
            else if (item == "Disconnect_FP")
            {
                unlock = new Unlock(Unlock.UnlockType.DisconnectFP, item, true);
                _givenPebblesOff = true;
                if (Plugin.Singleton.game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.game.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = true;
            }
            else if (item == "Rewrite_Spear_Pearl")
            {
                unlock = new Unlock(Unlock.UnlockType.RewriteSpearPearl, item, true);
                _givenSpearPearlRewrite = true;
                if (Plugin.Singleton.game?.GetStorySession?.saveState != null)
                    Plugin.Singleton.game.GetStorySession.saveState.miscWorldSaveData.smPearlTagged = true;
            }

            Plugin.Log.LogInfo($"Received item: {item}");
            //if (printLog && unlock != null)
            //{
            //    Plugin.Singleton.notifQueue.Enqueue(unlock.UnlockCompleteMessage());
            //}
        }

        public bool InitializeSession(SlugcatStats.Name slugcat)
        {
            // Set max karma depending on setting and current slugcat
            // If starting min karma
            if (false)
            {
                int totalKarmaIncreases = 0; // Count karma increases from datapackage. Maybe not needed? Could just set based on slugcat
                int cap = Mathf.Max(0, 8 - totalKarmaIncreases);
                _currentMaxKarma = cap;
            }
            else
            {
                _currentMaxKarma = SlugcatStats.SlugcatStartingKarma(slugcat);
            }

            // Load the item delivery queue from file as normal
            Plugin.Singleton.itemDeliveryQueue = SaveManager.LoadItemQueue(slugcat, Plugin.Singleton.rainWorld.options.saveSlot);
            Plugin.Singleton.lastItemDeliveryQueue = new Queue<Unlock.Item>(Plugin.Singleton.itemDeliveryQueue);

            return true;
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
            if (!ArchipelagoConnection.IsConnected || (IsLocationGiven(location) ?? true)) return false;

            if (!ArchipelagoConnection.LocationNameToID.ContainsKey(location))
            {
                Plugin.Log.LogError($"Failed to find ID for location: {location}");
            }

            ArchipelagoConnection.Session.Locations.CompleteLocationChecks(new long[] {ArchipelagoConnection.LocationNameToID[location]});
            locationsStatus[location] = true;
            // TODO: ask server what that item was for logging purposes
            Plugin.Log.LogInfo($"Found location: {location}!");
            return true;
        }

        // This will have to ask the server to scout the location, which takes time.
        // Thankfully, the only place that uses this is the spoiler menu, which can be re-written for AP
        public override Unlock GetUnlockAtLocation(string location)
        {
            return null;
        }

        public void SaveGame()
        {
            if (!ArchipelagoConnection.IsConnected || !isPopulated || !locationsLoaded) return;

            SaveManager.WriteAPSaveToFile(
                $"{ArchipelagoConnection.generationSeed}_{ArchipelagoConnection.playerName}",
                ArchipelagoConnection.lastItemIndex,
                locationsStatus);
        }
    }
}
