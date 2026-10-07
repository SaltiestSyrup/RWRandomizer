using RainWorldRandomizer.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using RainWorldRandomizer.Menu;
using RainWorldRandomizer.SaveData;

namespace RainWorldRandomizer
{
    /// <summary>
    /// The default randomizer mode.
    /// Used when no other modes are active
    /// </summary>
    public class ManagerVanilla : ManagerBase
    {
        // Constant for the minimum amount of gates that should be locked to make a valid seed
        public const int MIN_LOCKED_GATES = 0;
        public const int MIN_PASSAGE_TOKENS = 5;

        // Values for completed checks
        private Dictionary<string, Unlock> randomizerKey = [];

        // Called when player starts or continues a run
        public override void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved)
        {
            base.StartNewGameSession(storyGameCharacter, continueSaved);
            ManagerArchipelago.LoadAPItemNames(); // For display names

            if (!Constants.CompatibleSlugcats.Contains(storyGameCharacter))
            {
                Plugin.Log.LogWarning("Selected incompatible save, disabling randomizer");
                isRandomizerActive = false;
                Plugin.QueueNotify(new MessageText(
                    $"WARNING: This campaign is not currently supported by Check Randomizer. It will not be active for this session.",
                    Color.red));
                return;
            }

            // Reset tracking variables
            _currentMaxKarma = RandoOptions.StartMinimumKarma
                ? 0
                : SlugcatStats.SlugcatStartingKarma(storyGameCharacter);
            _hunterBonusCyclesGiven = 0;
            _givenNeuronGlow = false;
            _givenMark = false;
            _givenRobo = false;
            _givenLongerCycles = false;
            _givenPebblesOff = false;
            _givenSpearPearlRewrite = false;
            customStartDen = "";

            Plugin.SetupProperRegionMap(storyGameCharacter);

            // Init passage list
            foreach (string passage in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
            {
                if (passage == "Gourmand") continue;
                passageTokensStatus.Add(new(passage), false);
            }

            // Continue existing game
            if (continueSaved)
            {
                // Add all gates to status dict
                foreach (string roomName in Plugin.Singleton.rainWorld.progression.karmaLocks)
                {
                    string gate = Regex.Split(roomName, " : ")[0];
                    if (!gatesStatus.ContainsKey(gate)) gatesStatus.Add(gate, false);
                }

                // Load save game
                try
                {
                    Plugin.Log.LogInfo("Continuing randomizer game...");
                    InitSavedGame(SaveTracker.CurrentRandomizerSlot);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to load saved game. \n{e}");
                    isRandomizerActive = false;
                    Plugin.QueueNotify(
                        new MessageText($"Randomizer failed to find valid save for current file", Color.red));
                    return;
                }
            }
            // Load fresh legacy slot 
            else if (SaveTracker.ActiveLegacySlot >= 0)
            {
                try
                {
                    SaveFile file = SaveManager
                        .LoadLegacyStandaloneSavedGame(
                            Path.Combine(ModManager.ActiveMods.First(m => m.id == Plugin.PLUGIN_GUID).NewestPath,
                                $"saved_game_{currentSlugcat.value}_{SaveTracker.OrigSaveSlot}.txt"));
                    randomizerKey = file.locationMap.ToDictionary(kvp => kvp.Key, kvp =>
                        new Unlock(ExtEnumBase.TryParse(typeof(Unlock.UnlockType), kvp.Value.type, true,
                                out ExtEnumBase t)
                                ? (Unlock.UnlockType)t
                                : Unlock.UnlockType.Item,
                            kvp.Value.id, kvp.Value.collected));
                    locations = [.. randomizerKey.Select(kvp => new LocationInfo(kvp.Key, kvp.Value.IsGiven, false))];
                    customStartDen = file.startingDen;
                    currentSeed = file.seed;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to load saved game. \n{e}");
                    isRandomizerActive = false;
                    Plugin.QueueNotify(new MessageText("Randomizer failed to load legacy file",
                        Color.red));
                    return;
                }

                (itemDeliveryQueue, pendingTrapQueue) =
                    SaveManager.LoadItemQueue(currentSlugcat, SaveTracker.OrigSaveSlot);

                lastItemDeliveryQueue = new Queue<Unlock.Item>(Plugin.RandoManager.itemDeliveryQueue);
            }
            // Generate new game
            else
            {
                Plugin.Log.LogInfo("Starting new randomizer game...");

                if (!TokenCachePatcher.hasLoadedCache)
                {
                    Plugin.QueueNotify(new MessageText(
                        "Failed to start randomizer, token cache data missing or corrupt. Try reloading mods to update cache",
                        Color.red));
                    return;
                }

                VanillaGenerator generator = new(currentSlugcat, SlugcatStats.SlugcatToTimeline(currentSlugcat),
                    RandoOptions.LoadedOptions);

                Exception generationException = null;
                bool timedOut = false;
                try
                {
                    timedOut = !generator.BeginGeneration().Wait(10000);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError(e);
                    generationException = e;
                }

                Plugin.Log.LogDebug(generator.generationLog);

                if (generator.CurrentStage == VanillaGenerator.GenerationStep.Complete)
                {
                    // Load gates from generator
                    // Existing gates that didn't have an item placed start open
                    foreach (string gate in generator.AllGates.Where(gate => !gatesStatus.ContainsKey(gate)))
                    {
                        gatesStatus.Add(gate, generator.UnplacedGates.Contains(gate));
                    }

                    // Write new save game
                    randomizerKey = generator.GetCompletedSeed();
                    locations = [..randomizerKey.Select(kvp => new LocationInfo(kvp.Key, false, false))];
                    customStartDen = generator.customStartDen;
                    currentSeed = generator.generationSeed;
                    SaveManager.WriteToFile(Plugin.Singleton.rainWorld, this);
                }
                else
                {
                    if (timedOut) Plugin.Log.LogDebug("Generation timed out.");

                    // Log reason for expected generation exceptions
                    if (generator.CurrentStage == VanillaGenerator.GenerationStep.FailedGen
                        && generationException?.InnerException is VanillaGenerator.GenerationFailureException)
                    {
                        Plugin.QueueNotify(new MessageText(
                            $"Randomizer failed to generate with error: {generationException.InnerException.Message}. " +
                            $"More details found in BepInEx/LogOutput.log",
                            Color.red));
                    }
                    else
                    {
                        Plugin.QueueNotify(new MessageText(
                            $"Randomizer failed to generate. More details found in BepInEx/LogOutput.log", Color.red));
                    }

                    return;
                }
            }

            isRandomizerActive = true;
        }

        private void InitSavedGame(int saveSlot)
        {
            if (!SaveManager.TryReadFromFile(saveSlot, out SaveFile file))
            {
                throw new FileNotFoundException();
            }

            RandoOptions.LoadedOptions = file.options;

            randomizerKey = file.locationMap.ToDictionary(kvp => kvp.Key, kvp =>
                new Unlock(ExtEnumBase.TryParse(typeof(Unlock.UnlockType), kvp.Value.type, true, out ExtEnumBase t)
                        ? (Unlock.UnlockType)t
                        : Unlock.UnlockType.Item,
                    kvp.Value.id, kvp.Value.collected));
            locations = [.. randomizerKey.Select(kvp => new LocationInfo(kvp.Key, kvp.Value.IsGiven, false))];

            // Set unlocked gates and passage tokens
            foreach (Unlock item in randomizerKey.Values)
            {
                switch (item.Type.value)
                {
                    case "Gate":
                        if (gatesStatus.ContainsKey(item.ID))
                        {
                            gatesStatus[item.ID] =
                                gatesStatus[item.ID] ||
                                item.IsGiven; // If the gate was already opened by an identical unlock, keep it open
                        }

                        break;
                    case "Token":
                        if (passageTokensStatus.ContainsKey(new WinState.EndgameID(item.ID)))
                        {
                            passageTokensStatus[new WinState.EndgameID(item.ID)] = item.IsGiven;
                        }

                        break;
                    case "Karma":
                        if (item.IsGiven) IncreaseKarma();
                        break;
                    case "Neuron_Glow":
                        if (item.IsGiven) _givenNeuronGlow = true;
                        break;
                    case "The_Mark":
                        if (item.IsGiven) _givenMark = true;
                        break;
                    case "HunterCycles":
                        if (item.IsGiven) _hunterBonusCyclesGiven++;
                        break;
                    case "DamageUpgrade":
                        if (item.IsGiven) _numDamageUpgrades++;
                        break;
                    case "ExpeditionPerk":
                        if (item.IsGiven) GrantExpeditionPerk(item.ID);
                        break;
                    case "IdDrone":
                        if (item.IsGiven) _givenRobo = true;
                        break;
                    case "DisconnectFP":
                        if (item.IsGiven)
                        {
                            _givenLongerCycles = true;
                            _givenPebblesOff = true;
                        }

                        break;
                    case "Longer_Cycles":
                        if (item.IsGiven) _givenLongerCycles = true;
                        break;
                    case "Disconnect_Pebbles":
                        if (item.IsGiven) _givenPebblesOff = true;
                        break;
                    case "RewriteSpearPearl":
                        if (item.IsGiven) _givenSpearPearlRewrite = true;
                        break;
                }
            }

            itemDeliveryQueue = [];
            foreach (Unlock.Item item in file.pendingFiller.Select(f =>
                         Unlock.IDToItem(f.id, f.type == nameof(DataPearl.AbstractDataPearl.DataPearlType))))
            {
                itemDeliveryQueue.Enqueue(item);
            }

            pendingTrapQueue = [];
            foreach (TrapsHandler.Trap item in file.pendingTraps.Select(t => new TrapsHandler.Trap(t)))
            {
                pendingTrapQueue.Enqueue(item);
            }

            lastItemDeliveryQueue = new Queue<Unlock.Item>(Plugin.RandoManager.itemDeliveryQueue);
        }

        public override bool LocationExists(string location)
        {
            if (base.LocationExists(location)) return true;

            // TODO: Remove 1.4 backwards compat in 1.6
            // Backwards compat for pre-1.4 save files
            // As of 1.4, tokens and pearls have their origin region appended to the end.
            // This is not the case when loading pre-1.4 files, so it needs to be checked for
            string[] split = location.Split('-');
            if (split.Length == 3 && (split[0] == "Token" || split[0] == "Pearl"))
                location = $"{split[0]}-{split[1]}";
            return base.LocationExists(location);
        }

        public override bool? IsLocationGiven(string location)
        {
            if (base.LocationExists(location)) return base.IsLocationGiven(location);

            // Backwards compat w/ 1.3 save files
            string[] split = location.Split('-');
            if (split.Length == 3 && (split[0] == "Token" || split[0] == "Pearl"))
                location = $"{split[0]}-{split[1]}";
            return base.IsLocationGiven(location);
        }

        public override void GiveLocation(string location)
        {
            if (!base.LocationExists(location))
            {
                // Backwards compat w/ 1.3 save files
                string[] split = location.Split('-');
                if (split.Length == 3 && (split[0] == "Token" || split[0] == "Pearl"))
                    location = $"{split[0]}-{split[1]}";
            }

            if (IsLocationGiven(location) is true or null) return;

            randomizerKey[location].GiveUnlock();
            locations.FirstOrDefault(l => l.internalName == location)?.MarkCollected();

            Plugin.QueueNotify(new MessageText(
                [
                    "Found ",
                    ManagerArchipelago.ClientNameToAPItem.TryGetValue(randomizerKey[location].ID, out string name)
                        ? name
                        : randomizerKey[location].ID,
                    " from ",
                    LocationInfo.ClientNameToDisplayName(location)
                ],
                [
                    Color.white,
                    new Color(0.0f, 0.4f, 0.7f),
                    Color.white,
                    new Color(0.7f, 0.3f, 0.7f),
                ]));
            Plugin.Log.LogInfo($"Completed Check: {location}");
        }

        public override Unlock GetUnlockAtLocation(string location)
        {
            return !LocationExists(location) ? null : randomizerKey[location];
        }

        public override void SaveGame(bool saveCurrentState)
        {
            SaveManager.WriteToFile(Plugin.Singleton.rainWorld, this, saveCurrentState);

            if (SaveTracker.ActiveLegacySlot >= 0)
            {
                SaveManager.DestroyLegacySave(currentSlugcat, SaveTracker.OrigSaveSlot);
            }
        }
    }
}