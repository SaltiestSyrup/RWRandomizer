using RainWorldRandomizer.Generation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

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
        public Dictionary<string, Unlock> randomizerKey = [];

        // Called when player starts or continues a run
        public override void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved)
        {
            base.StartNewGameSession(storyGameCharacter, continueSaved);

            if (!Constants.CompatibleSlugcats.Contains(storyGameCharacter))
            {
                Plugin.Log.LogWarning("Selected incompatible save, disabling randomizer");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText($"WARNING: This campaign is not currently supported by Check Randomizer. It will not be active for this session.", Color.red));
                return;
            }

            Plugin.ProperRegionMap.Clear();

            // Reset tracking variables
            _currentMaxKarma = RandoOptions.StartMinimumKarma ? 0 : SlugcatStats.SlugcatStartingKarma(storyGameCharacter);
            _hunterBonusCyclesGiven = 0;
            _givenNeuronGlow = false;
            _givenMark = false;
            _givenRobo = false;
            _givenPebblesOff = false;
            _givenSpearPearlRewrite = false;
            customStartDen = "";

            // Init alternate region mapping
            foreach (string region in Region.GetFullRegionOrder())
            {
                Plugin.ProperRegionMap.Add(region, Region.GetProperRegionAcronym(SlugcatStats.SlugcatToTimeline(storyGameCharacter), region));
            }

            if (Input.GetKey("o"))
            {
                DebugBulkGeneration(500);
            }

            if (continueSaved)
            {
                // Add all gates to status dict
                foreach (string roomName in Plugin.Singleton.rainWorld.progression.karmaLocks)
                {
                    string gate = Regex.Split(roomName, " : ")[0];
                    if (!gatesStatus.ContainsKey(gate)) gatesStatus.Add(gate, false);
                }

                // Load save game
                if (SaveManager.IsThereASavedGame(storyGameCharacter, Plugin.Singleton.rainWorld.options.saveSlot))
                {
                    Plugin.Log.LogInfo("Continuing randomizer game...");
                    InitSavedGame(storyGameCharacter, Plugin.Singleton.rainWorld.options.saveSlot);
                }
                else
                {
                    Plugin.Log.LogError("Failed to load saved game.");
                    isRandomizerActive = false;
                    Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText($"Randomizer failed to find valid save for current file", Color.red));
                    return;
                }
            }
            else
            {
                Plugin.Log.LogInfo("Starting new randomizer game...");

                if (!TokenCachePatcher.hasLoadedCache)
                {
                    Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText("Failed to start randomizer, missing token cache data. Try reloading mods to update cache", Color.red));
                    return;
                }

                VanillaGenerator generator = new(currentSlugcat, SlugcatStats.SlugcatToTimeline(currentSlugcat),
                    RandoOptions.UseSetSeed ? RandoOptions.SetSeed : UnityEngine.Random.Range(0, int.MaxValue));

                Exception generationException = null;
                bool timedOut = false;
                try
                {
                    timedOut = !generator.BeginGeneration(true).Wait(10000);
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
                    foreach (string gate in generator.AllGates)
                    {
                        if (!gatesStatus.ContainsKey(gate)) gatesStatus.Add(gate, generator.UnplacedGates.Contains(gate));
                    }

                    // Write new save game
                    randomizerKey = generator.GetCompletedSeed();
                    customStartDen = generator.customStartDen;
                    currentSeed = generator.generationSeed;
                    SaveManager.WriteSavedGameToFile(randomizerKey, storyGameCharacter, Plugin.Singleton.rainWorld.options.saveSlot);
                }
                else
                {
                    if (timedOut) Plugin.Log.LogDebug("Generation timed out.");

                    // Log reason for expected generation exceptions
                    if (generator.CurrentStage == VanillaGenerator.GenerationStep.FailedGen
                        && generationException.InnerException is VanillaGenerator.GenerationFailureException)
                    {
                        Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText(
                            $"Randomizer failed to generate with error: {generationException.InnerException.Message}. More details found in BepInEx/LogOutput.log",
                            Color.red));
                    }
                    else
                    {
                        Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText($"Randomizer failed to generate. More details found in BepInEx/LogOutput.log", Color.red));
                    }
                    return;
                }
            }

            isRandomizerActive = true;
        }

        public void DebugBulkGeneration(int howMany)
        {
            VanillaGenerator[] generators = new VanillaGenerator[howMany];
            Task[] genTask = new Task[howMany];
            int numSucceeded = 0;
            int numFailed = 0;

            Stopwatch sw = Stopwatch.StartNew();

            Plugin.Log.LogDebug("Starting bulk generation test");
            for (int i = 0; i < howMany; i++)
            {
                generators[i] = new VanillaGenerator(currentSlugcat, SlugcatStats.SlugcatToTimeline(currentSlugcat), UnityEngine.Random.Range(0, int.MaxValue));
                genTask[i] = generators[i].BeginGeneration();
            }

            // Only gen for up to 30 seconds
            // Try block here to stop WaitAll from throwing innner task's exceptions
            try { Task.WaitAll(genTask, 30000); }
            catch { }

            sw.Stop();

            for (int j = 0; j < howMany; j++)
            {
                if (genTask[j].Exception != null)
                {
                    Plugin.Log.LogError($"Generation failure with Exception:");
                    Plugin.Log.LogError(genTask[j].Exception);
                    Plugin.Log.LogDebug($"Log for failed gen:");
                    Plugin.Log.LogDebug(generators[j].generationLog);
                    numFailed++;
                }
                else if (generators[j].CurrentStage == VanillaGenerator.GenerationStep.Complete)
                {
                    numSucceeded++;
                }
                else
                {
                    Plugin.Log.LogError($"Generation was timed out before completion during stage: {generators[j].CurrentStage}");
                    //Plugin.Log.LogDebug(generators[j].generationLog);
                }
            }
            Plugin.Log.LogDebug($"Bulk gen complete; \n\tSucceeded: {numSucceeded}\n\tFailed: {numFailed}\n\tRate: {(float)numSucceeded / howMany * 100}%\n\tTime: {sw.ElapsedMilliseconds} ms");
        }

        public void InitSavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            randomizerKey = SaveManager.LoadSavedGame(slugcat, saveSlot);

            // Set unlocked gates and passage tokens
            foreach (Unlock item in randomizerKey.Values)
            {
                switch (item.Type.value)
                {
                    case "Gate":
                        if (gatesStatus.ContainsKey(item.ID))
                        {
                            gatesStatus[item.ID] = gatesStatus[item.ID] || item.IsGiven; // If the gate was already opened by an identical unlock, keep it open
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
                    case "IdDrone":
                        if (item.IsGiven) _givenRobo = true;
                        break;
                    case "DisconnectFP":
                        if (item.IsGiven) _givenPebblesOff = true;
                        break;
                    case "RewriteSpearPearl":
                        if (item.IsGiven) _givenSpearPearlRewrite = true;
                        break;
                }

            }

            Plugin.Singleton.itemDeliveryQueue = SaveManager.LoadItemQueue(slugcat, saveSlot);
            Plugin.Singleton.lastItemDeliveryQueue = new Queue<Unlock.Item>(Plugin.Singleton.itemDeliveryQueue);
        }

        public override List<string> GetLocations()
        {
            return [.. randomizerKey.Keys];
        }

        public override bool LocationExists(string location)
        {
            return randomizerKey.ContainsKey(location);
        }

        public override bool? IsLocationGiven(string location)
        {
            if (!LocationExists(location)) return null;

            return randomizerKey[location].IsGiven;
        }

        public override bool GiveLocation(string location)
        {
            if (IsLocationGiven(location) ?? true) return false;

            randomizerKey[location].GiveUnlock();
            Plugin.Singleton.notifQueue.Enqueue(new ChatLog.MessageText(randomizerKey[location].UnlockCompleteMessage()));
            Plugin.Log.LogInfo($"Completed Check: {location}");

            return true;
        }

        public override Unlock GetUnlockAtLocation(string location)
        {
            if (!LocationExists(location)) return null;

            return randomizerKey[location];
        }

        public override void SaveGame(bool saveCurrentState)
        {
            SaveManager.WriteSavedGameToFile(
                        randomizerKey,
                        currentSlugcat,
                        Plugin.Singleton.rainWorld.options.saveSlot);

            if (saveCurrentState)
            {
                SaveManager.WriteItemQueueToFile(
                    Plugin.Singleton.itemDeliveryQueue,
                    currentSlugcat,
                    Plugin.Singleton.rainWorld.options.saveSlot);
            }
        }
    }
}
