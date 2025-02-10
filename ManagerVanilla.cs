using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public Dictionary<string, Unlock> randomizerKey = new Dictionary<string, Unlock>();

        public List<Unlock> AllUnlocks = new List<Unlock>();

        public static Dictionary<SlugcatStats.Name, List<string>> AllAccessibleRegions = new Dictionary<SlugcatStats.Name, List<string>>();
        // Certain regions either cannot or should not be accessed by some slugcats.
        // This dictionary allows the randomizer to remove the relevant gates from the unlock pool
        public static Dictionary<SlugcatStats.Name, List<string>> RegionBlacklists = new Dictionary<SlugcatStats.Name, List<string>>();
        // Some specific gates are inaccessible to some slugcats, storing these edge cases here
        public static Dictionary<SlugcatStats.Name, List<string>> GateBlackLists = new Dictionary<SlugcatStats.Name, List<string>>();
        // Same principle as regions. Auto-generated checks (passages, echoes, pearls) need to be filtered to match what is possible for each slugcat
        public static Dictionary<SlugcatStats.Name, List<string>> CheckBlacklists = new Dictionary<SlugcatStats.Name, List<string>>();

        public static List<string> LogicBlacklist = new List<string>()
        {
            "GATE_UW_SL",
            "Pearl-SU_filt"
        };

        // Called when player starts or continues a run
        public override void StartNewGameSession(SlugcatStats.Name storyGameCharacter, bool continueSaved)
        {
            base.StartNewGameSession(storyGameCharacter, continueSaved);

            if (!Plugin.CompatibleSlugcats.Contains(storyGameCharacter))
            {
                Plugin.Log.LogWarning("Selected incompatible save, disabling randomizer");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue($"WARNING: This campaign is not currently supported by Check Randomizer. It will not be active for this session.");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Attempt to initialize session
            if (!InitializeSession(storyGameCharacter))
            {
                Plugin.Log.LogError("Failed to initialize randomizer.");
                isRandomizerActive = false;
                Plugin.Singleton.notifQueue.Enqueue($"Randomizer failed to initialize. Check logs for details.");
                return;
            }

            Plugin.Log.LogDebug($"Initialized session in {stopwatch.ElapsedMilliseconds}");
            stopwatch.Stop();

            if (Input.GetKey("o"))
            {
                DebugBulkGeneration(100);
            }

            if (continueSaved)
            {
                if (SaveManager.IsThereASavedGame(storyGameCharacter, Plugin.Singleton.rainWorld.options.saveSlot))
                {
                    Plugin.Log.LogInfo("Continuing randomizer game...");
                    InitSavedGame(storyGameCharacter, Plugin.Singleton.rainWorld.options.saveSlot);
                }
                else
                {
                    Plugin.Log.LogError("Failed to load saved game.");
                    isRandomizerActive = false;
                    Plugin.Singleton.notifQueue.Enqueue($"Randomizer failed to find valid save for current file");
                    return;
                }
            }
            else
            {
                Plugin.Log.LogInfo("Starting new randomizer game...");

                stopwatch.Restart();
                if (GenerateRandomizer())
                {
                    SaveManager.WriteSavedGameToFile(randomizerKey, storyGameCharacter, Plugin.Singleton.rainWorld.options.saveSlot);
                }
                else
                {
                    Plugin.Log.LogError("Failed to generate randomizer. See above for details.");
                    isRandomizerActive = false;
                    Plugin.Singleton.notifQueue.Enqueue($"Randomizer failed to generate. More details found in BepInEx/LogOutput.log");
                    return;
                }
                Plugin.Log.LogDebug($"Gen complete in {stopwatch.ElapsedMilliseconds} ms");
                stopwatch.Stop();
            }

            isRandomizerActive = true;
        }

        public bool InitializeSession(SlugcatStats.Name slugcat)
        {
            Plugin.ProperRegionMap.Clear();

            // Reset tracking variables
            _currentMaxKarma = 4;
            _hunterBonusCyclesGiven = 0;
            _givenNeuronGlow = false;
            _givenMark = false;
            _givenRobo = false;
            _givenPebblesOff = false;
            _givenSpearPearlRewrite = false;
            customStartDen = "SU_S01";

            if (!RegionBlacklists.ContainsKey(slugcat) || !CheckBlacklists.ContainsKey(slugcat))
            {
                Plugin.Log.LogError("Tried to initialize session with unimplemented slugcat. Aborting...");
                return false;
            }

            if (ModManager.MSC)
            {
                if (Plugin.allowMetroForOthers.Value)
                {
                    OverrideBlacklist(slugcat, "LC");
                }
                if (Plugin.allowSubmergedForOthers.Value
                    && slugcat != MoreSlugcatsEnums.SlugcatStatsName.Artificer
                    && slugcat != MoreSlugcatsEnums.SlugcatStatsName.Spear)
                {
                    RegionBlacklists[slugcat].Remove("MS");
                }
            }

            foreach (string region in Region.GetFullRegionOrder())
            {
                Plugin.ProperRegionMap.Add(region, Region.GetProperRegionAcronym(slugcat, region));

                // Remove alternate regions that don't apply to this slugcat
                if (!(SlugcatStats.getSlugcatStoryRegions(slugcat).Contains(region)
                    || SlugcatStats.getSlugcatOptionalRegions(slugcat).Contains(region)))
                {
                    RegionBlacklists[slugcat].Add(region);
                    //Log.LogDebug($"Removed region {region}");
                }
            }

            // Load Tokens
            try
            {
                Plugin.Singleton.collectTokenHandler.LoadAvailableTokens(Plugin.Singleton.rainWorld, slugcat);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed loading sandbox tokens");
                Plugin.Log.LogError(e);
            }

            // Remove Outer Expanse if not unlocked
            if ((slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Yellow)
                && (!ModManager.MSC || !(Plugin.Singleton.rainWorld.progression.miscProgressionData.beaten_Gourmand || MoreSlugcats.MoreSlugcats.chtUnlockOuterExpanse.Value)))
            {
                RegionBlacklists[slugcat].Add("OE");
                CheckBlacklists[slugcat].Add("Pearl-OE");
            }

            List<string> allAccessibleRegions = Region.GetFullRegionOrder().Except(RegionBlacklists[slugcat]).ToList();
            _currentMaxKarma = SlugcatStats.SlugcatStartingKarma(slugcat);
            gatesStatus.Clear();
            AllUnlocks.Clear();
            randomizerKey.Clear();
            passageTokensStatus.Clear();

            // Populate gate unlocks
            foreach (string roomName in Plugin.Singleton.rainWorld.progression.karmaLocks)
            {
                string gate = Regex.Split(roomName, " : ")[0];
                if (gatesStatus.ContainsKey(gate)) continue;

                bool isBlacklisted = false;
                // Check region blacklists
                foreach (string region in RegionBlacklists[slugcat])
                {
                    // If this blacklisted region has an alternative for the slugcat, let it slide
                    if (Plugin.ProperRegionMap.ContainsKey(region)
                        && region == Plugin.ProperRegionMap[region] && gate.Contains(region))
                    {
                        isBlacklisted = true;
                        break;
                    }
                }

                // Check specific gate blacklists
                if (GateBlackLists[slugcat].Contains(gate)
                    // Check that both connecting regions actually exist
                    || !Region.GetFullRegionOrder().Contains(Regex.Split(gate, "_")[1])
                    || !Region.GetFullRegionOrder().Contains(Regex.Split(gate, "_")[2])
                    // Check if this gate room is not accessible to the current slugcat
                    || (CollectTokenHandler.GetRoomAccessibility(Regex.Split(gate, "_")[1]).ContainsKey(gate)
                        && !CollectTokenHandler.GetRoomAccessibility(Regex.Split(gate, "_")[1])[gate].Contains(slugcat)))
                {
                    isBlacklisted = true;
                }

                if (isBlacklisted) continue;

                gatesStatus.Add(gate, false);
                AllUnlocks.Add(new Unlock(Unlock.UnlockType.Gate, gate));

            }

            // Populate Passage Unlocks
            // Hunter can't use passages, so don't include the tokens
            foreach (string ID in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
            {
                bool motherUnlocked = ModManager.MSC && (Plugin.Singleton.rainWorld.progression.miscProgressionData.beaten_Gourmand_Full || MoreSlugcats.MoreSlugcats.chtUnlockSlugpups.Value);
                bool canFindSlugpups = slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Red || (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand);

                // Skip passages that aren't possible
                if (CheckBlacklists[slugcat].Contains($"Passage-{ID}")) continue;
                if (ModManager.MSC)
                {
                    switch (ID)
                    {
                        case "Gourmand":
                            if (slugcat != MoreSlugcatsEnums.SlugcatStatsName.Gourmand) continue;
                            break;
                        case "Mother":
                            if (!motherUnlocked || !canFindSlugpups) continue;
                            break;
                        case "Chieftain":
                            if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer) continue;
                            break;
                        case "Monk":
                        case "Saint":
                            if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear) continue;
                            break;
                        case "Hunter":
                        case "Outlaw":
                        case "DragonSlayer":
                        case "Scholar":
                            if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint) continue;
                            break;
                    }
                }

                if (Plugin.usePassageChecks.Value)
                {
                    randomizerKey.Add($"Passage-{ID}", null);
                }
                if (Plugin.givePassageUnlocks.Value
                    && (currentSlugcat != SlugcatStats.Name.Red
                        && (!ModManager.MSC || currentSlugcat != MoreSlugcatsEnums.SlugcatStatsName.Saint)) // Hunter and Saint can't use passages
                    && ID != "Gourmand")
                {
                    AllUnlocks.Add(new Unlock(Unlock.UnlockType.Token, ID));
                    passageTokensStatus.Add(new WinState.EndgameID(ID), false);
                }
            }

            // --- RegionKit Dependent ---
            if (RegionKitCompatibility.Enabled)
            {
                foreach (string regionInitials in allAccessibleRegions)
                {
                    if (RegionKitCompatibility.RegionHasEcho(regionInitials, slugcat)
                        && !CheckBlacklists[slugcat].Contains($"Echo-{regionInitials}"))
                    {
                        if (Plugin.useEchoChecks.Value)
                        {
                            randomizerKey.Add($"Echo-{regionInitials}", null);
                        }
                        AllUnlocks.Add(new Unlock(Unlock.UnlockType.Karma, "Karma")); // One karma increase per Echo
                    }
                }
            }

            // Populate Echoes
            foreach (string ID in ExtEnumBase.GetNames(typeof(GhostWorldPresence.GhostID)))
            {
                if (!ID.Equals("NoGhost")
                    && !CheckBlacklists[slugcat].Contains($"Echo-{ID}")
                    && !RegionBlacklists[slugcat].Contains(ID)
                    && !randomizerKey.ContainsKey($"Echo-{ID}"))
                {
                    if (Plugin.useEchoChecks.Value)
                    {
                        randomizerKey.Add($"Echo-{ID}", null);
                    }
                    AllUnlocks.Add(new Unlock(Unlock.UnlockType.Karma, "Karma")); // One karma increase per Echo
                }
            }
            // 7 karma increases (6 Echoes + FP)
            AllUnlocks.Add(new Unlock(Unlock.UnlockType.Karma, "Karma"));

            // Reduce max karma if setting
            if (Plugin.startMinKarma.Value)
            {
                int totalKarmaIncreases = AllUnlocks.Count(u => u.Type == Unlock.UnlockType.Karma);
                int cap = Mathf.Max(0, 8 - totalKarmaIncreases);
                _currentMaxKarma = cap;
                //rainWorld.progression.currentSaveState.deathPersistentSaveData.karma = cap;
            }

            // Populate Pearls
            // Monk do no pearls if not using MSC
            if (Plugin.usePearlChecks.Value && (ModManager.MSC || slugcat != SlugcatStats.Name.Yellow))
            {
                // For each region
                foreach (string region in allAccessibleRegions)
                {
                    string regionLower = region.ToLowerInvariant();
                    if (!Plugin.Singleton.rainWorld.regionDataPearls.ContainsKey(regionLower)) continue;

                    // For each pearl in region
                    for (int i = 0; i < Plugin.Singleton.rainWorld.regionDataPearls[regionLower].Count; i++)
                    {
                        // If this pearl is accessible to the current slugcat & is not blacklisted
                        if (Plugin.Singleton.rainWorld.regionDataPearlsAccessibility[regionLower][i].Contains(slugcat)
                            && !CheckBlacklists[slugcat].Contains($"Pearl-{Plugin.Singleton.rainWorld.regionDataPearls[regionLower][i].value}"))
                        {
                            randomizerKey.Add($"Pearl-{Plugin.Singleton.rainWorld.regionDataPearls[regionLower][i].value}", null);
                        }
                    }
                }
            }

            // Populate Sandbox Token unlocks
            if (Plugin.useSandboxTokenChecks.Value)
            {
                foreach (string regionShort in Plugin.Singleton.collectTokenHandler.availableTokens.Keys)
                {
                    if (allAccessibleRegions.Contains(regionShort))
                    {
                        foreach (string token in Plugin.Singleton.collectTokenHandler.availableTokens[regionShort])
                        {
                            if (!randomizerKey.ContainsKey($"Token-{token}"))
                            {
                                randomizerKey.Add($"Token-{token}", null);
                            }
                        }
                    }
                }
            }

            // Populate Spearmaster Broadcast tokens
            if (ModManager.MSC
                && currentSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear
                && Plugin.useSMTokens.Value)
            {
                foreach (string regionShort in allAccessibleRegions)
                {
                    if (!Plugin.Singleton.rainWorld.regionGreyTokens.ContainsKey(regionShort.ToLowerInvariant())) continue;
                    foreach (ChatlogData.ChatlogID token in Plugin.Singleton.rainWorld.regionGreyTokens[regionShort.ToLowerInvariant()])
                    {
                        if (!randomizerKey.ContainsKey($"Broadcast-{token.value}"))
                        {
                            randomizerKey.Add($"Broadcast-{token.value}", null);
                        }
                    }
                }
            }

            if (ModManager.MSC && Plugin.useFoodQuestChecks.Value && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                foreach (WinState.GourmandTrackerData data in WinState.GourmandPassageTracker)
                {
                    randomizerKey.Add($"FoodQuest-{(data.type == AbstractPhysicalObject.AbstractObjectType.Creature ? data.crits[0].value : data.type.value)}", null);
                }
            }

            // Misc Checks
            if (Plugin.useSpecialChecks.Value)
            {
                randomizerKey.Add("Eat_Neuron", null);

                switch (slugcat.value)
                {
                    case "White":
                    case "Yellow":
                    case "Gourmand":
                    case "Spear":
                        randomizerKey.Add("Meet_LttM_Spear", null);
                        randomizerKey.Add("Meet_FP", null);
                        break;
                    case "Red":
                        randomizerKey.Add("Save_LttM", null);
                        randomizerKey.Add("Meet_FP", null);
                        break;
                    case "Artificer":
                        randomizerKey.Add("Meet_FP", null);
                        break;
                    case "Rivulet":
                        randomizerKey.Add("Meet_LttM", null);
                        if (Plugin.useEnergyCell.Value) randomizerKey.Add("Kill_FP", null);
                        break;
                    case "Saint":
                        randomizerKey.Add("Ascend_LttM", null);
                        randomizerKey.Add("Ascend_FP", null);
                        break;
                }
            }

            // Misc Unlocks
            if (!ModManager.MSC || slugcat != MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                AllUnlocks.Add(new Unlock(Unlock.UnlockType.Glow, "Neuron_Glow"));
                AllUnlocks.Add(new Unlock(Unlock.UnlockType.Mark, "The_Mark"));
            }

            switch (slugcat.value)
            {
                case "Red":
                    AllUnlocks.Add(new Unlock(Unlock.UnlockType.Item, "NSHSwarmer"));
                    AllUnlocks.Add(new Unlock(Unlock.UnlockType.ItemPearl, "Red_stomach"));
                    break;
                case "Artificer":
                    AllUnlocks.Add(new Unlock(Unlock.UnlockType.IdDrone, "IdDrone"));
                    break;
                case "Rivulet":
                    if (Plugin.useEnergyCell.Value)
                    {
                        AllUnlocks.Add(new Unlock(Unlock.UnlockType.Item, new Unlock.Item("Mass Rarefaction Cell", MoreSlugcatsEnums.AbstractObjectType.EnergyCell)));
                        AllUnlocks.Add(new Unlock(Unlock.UnlockType.DisconnectFP, "FP_Disconnected"));
                    }
                    break;
                case "Spear":
                    AllUnlocks.Add(new Unlock(Unlock.UnlockType.ItemPearl, "Spearmasterpearl"));
                    AllUnlocks.Add(new Unlock(Unlock.UnlockType.RewriteSpearPearl, "Rewrite_Spear_Pearl"));
                    break;
            }

            return true;
        }

        public bool GenerateRandomizer()
        {
            Plugin.Log.LogInfo($"Playing as {currentSlugcat}");
            Plugin.Log.LogInfo($"Checks: {randomizerKey.Count}");
            Plugin.Log.LogInfo($"Unlocks: {AllUnlocks.Count}");

            List<Unlock> remainingUnlocks = new List<Unlock>(AllUnlocks);

            // Set the seed for the rest of the generation to use
            try
            {
                if (Plugin.useSeed.Value)
                {
                    currentSeed = Plugin.seed.Value;
                    Plugin.Log.LogInfo($"Using set seed: {Plugin.seed.Value}");
                }
                else
                {
                    currentSeed = UnityEngine.Random.Range(0, int.MaxValue);
                    Plugin.Log.LogInfo($"Using generated seed: {currentSeed}");
                }

                UnityEngine.Random.InitState(currentSeed);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed to load seed");
                Plugin.Log.LogError(e);
                return false;
            }

            #region Check / Unlock balancing
            // Add extra gate unlocks to fill upto amount of checks
            List<Unlock> unlocksToAdd = new List<Unlock>();
            int hunterCounter = 0;
            while (randomizerKey.Count > remainingUnlocks.Count + unlocksToAdd.Count)
            {
                if (currentSlugcat == SlugcatStats.Name.Red
                    && (int)(randomizerKey.Count * Plugin.hunterCyclesDensity.Value) > hunterCounter) // Hunter cycle increases can only occupy up to 20% of the total unlocks
                {
                    unlocksToAdd.Add(new Unlock(Unlock.UnlockType.HunterCycles, "HunterCycles"));
                    hunterCounter++;
                }
                else if (Plugin.giveItemUnlocks.Value)
                {
                    unlocksToAdd.Add(new Unlock(Unlock.UnlockType.Item, Unlock.RandomJunkItem()));
                }
                else
                {
                    IEnumerable<Unlock> unlocks = remainingUnlocks.Where(u => u.Type == Unlock.UnlockType.Gate);

                    unlocksToAdd.Add(unlocks.ElementAt(UnityEngine.Random.Range(0, unlocks.Count())));
                }
            }

            if (unlocksToAdd.Count > 0)
            {
                remainingUnlocks.AddRange(unlocksToAdd);
            }

            List<string> preOpenedGates = new List<string>();
            // Remove random unlocks until the checks and unlocks are equal
            while (randomizerKey.Count < remainingUnlocks.Count)
            {
                // Remove random token
                IEnumerable<Unlock> tokenUnlocks = remainingUnlocks.Where(u => u.Type == Unlock.UnlockType.Token);
                if (tokenUnlocks.Count() > MIN_PASSAGE_TOKENS)
                {
                    remainingUnlocks.Remove(tokenUnlocks.ElementAt(UnityEngine.Random.Range(0, tokenUnlocks.Count())));
                    continue;
                }

                // Tokens are at their minimum, unlock random gates
                IEnumerable<Unlock> unlocks = remainingUnlocks.Where(u => u.Type == Unlock.UnlockType.Gate);
                if (unlocks.Count() > MIN_LOCKED_GATES)
                {
                    Unlock unlock = unlocks.ElementAt(UnityEngine.Random.Range(0, unlocks.Count()));
                    remainingUnlocks.Remove(unlock);
                    gatesStatus[unlock.ID] = true;
                    preOpenedGates.Add(unlock.ID);
                    Plugin.Log.LogInfo($"Removing lock for {unlock.ID}");
                    continue;
                }

                Plugin.Log.LogError("Too few checks to make valid randomizer");
                return false;
            }

            // Add any pre-opened gates to the key so they can be tracked
            for (int i = 0; i < preOpenedGates.Count; i++)
            {
                randomizerKey.Add($"FreeCheck-{i}", new Unlock(Unlock.UnlockType.Gate, preOpenedGates[i], true));
            }
            #endregion

            Plugin.Log.LogInfo("Generating Seed...");

            List<string> allRegions = SlugcatStats.SlugcatStoryRegions(currentSlugcat).Except(RegionBlacklists[currentSlugcat]).ToList(); // List of all non-blacklisted regions
            //allRegions.ForEach(r => Logger.LogDebug(r));

            //allRegions = allRegions.Except(RegionBlacklists[currentSlugcat]).ToList(); 
            Plugin.Log.LogInfo($"Found {allRegions.Count} valid regions");

            List<string> regionsAvailable = new List<string>();
            // If option selected, start from a random den
            if (Plugin.randomizeSpawnLocation.Value)
            {
                customStartDen = FindRandomStart(currentSlugcat);
                Plugin.Log.LogInfo($"Using randomized starting den: {customStartDen}");
                regionsAvailable.Add(Region.GetVanillaEquivalentRegionAcronym(Regex.Split(customStartDen, "_")[0]));
            }
            else
            {
                switch (currentSlugcat.value)
                {
                    case "White":
                    case "Yellow":
                    case "Spear":
                        regionsAvailable.Add("SU");
                        break;
                    case "Red":
                        regionsAvailable.Add("LF");
                        break;
                    case "Gourmand":
                        regionsAvailable.Add("SH");
                        break;
                    case "Artificer":
                        regionsAvailable.Add("GW");
                        break;
                    case "Rivulet":
                        regionsAvailable.Add("DS");
                        break;
                    case "Saint":
                        regionsAvailable.Add("SI");
                        break;
                    default:
                        Plugin.Log.LogError("Failed to assign slugcat's starting region");
                        return false;
                }
            }

            // Find available regions from any pre-opened gates
            if (preOpenedGates.Count > 0)
            {
                regionsAvailable = regionsAvailable.Union(UpdateAvailableRegions(regionsAvailable, preOpenedGates)).ToList();
                Plugin.Log.LogInfo($"Regions pre-available: {regionsAvailable.Count}");
            }

            // Would it be more efficient to construct possibleChecks and adjacentGates outside and add to it?
            // Or would that come out to a similar number of comparisons?
            while (remainingUnlocks.Count > 0)
            {
                if (regionsAvailable.Count() >= allRegions.Count) // All regions are accessible 
                {
                    string key = "";
                    int index = -1;

                    // Special logic cases
                    switch (currentSlugcat.value)
                    {
                        case "Red":
                            index = remainingUnlocks.FindIndex(u => u.ID.Equals("NSHSwarmer"));
                            if (index > -1)
                            {
                                // Checks that aren't saving LttM
                                List<string> possibleChecks = randomizerKey.Where(k =>
                                {
                                    if (k.Value != null) return false;
                                    return !k.Key.Equals("Save_LttM");
                                }).ToList().ConvertAll(p => p.Key);

                                key = possibleChecks[UnityEngine.Random.Range(0, possibleChecks.Count)];
                            }
                            break;
                        case "Artificer":
                            index = remainingUnlocks.FindIndex(u => u.Type == Unlock.UnlockType.IdDrone);
                            if (index > -1)
                            {
                                // Checks that aren't in Metropolis
                                List<string> possibleChecks = randomizerKey.Where(k =>
                                {
                                    if (k.Value != null) return false;
                                    if (k.Key == "Echo_LC") return false;
                                    if (k.Key.StartsWith("Token-")
                                        && Plugin.Singleton.collectTokenHandler.availableTokens.ContainsKey("LC")
                                        && Plugin.Singleton.collectTokenHandler.availableTokens["LC"].Any(c => k.Key == $"Token-{c}")) return false;
                                    if (k.Key.StartsWith("Pearl-")
                                        && Plugin.Singleton.rainWorld.regionDataPearls.ContainsKey("lc")
                                        && Plugin.Singleton.rainWorld.regionDataPearls["lc"]
                                            .Contains(new DataPearl.AbstractDataPearl.DataPearlType(k.Key.Substring(6)))
                                        && Plugin.Singleton.rainWorld.regionDataPearlsAccessibility["lc"]
                                            [Plugin.Singleton.rainWorld.regionDataPearls["lc"].IndexOf(new DataPearl.AbstractDataPearl.DataPearlType(k.Key.Substring(6)))]
                                            .Contains(currentSlugcat)) return false;
                                    return true;
                                }).ToList().ConvertAll(p => p.Key);

                                key = possibleChecks[UnityEngine.Random.Range(0, possibleChecks.Count)];
                            }
                            break;
                        case "Saint":
                            index = remainingUnlocks.FindIndex(u => u.Type == Unlock.UnlockType.Karma);
                            if (index > -1)
                            {
                                // Checks that aren't ascending either iterator
                                List<string> possibleChecks = randomizerKey.Where(k =>
                                {
                                    if (k.Value != null) return false;
                                    return !k.Key.Equals("Ascend_LttM") && !k.Key.Equals("Ascend_FP");
                                }).ToList().ConvertAll(p => p.Key);

                                key = possibleChecks[UnityEngine.Random.Range(0, possibleChecks.Count)];
                            }
                            break;
                    }
                    // Save_LttM !> NSHSwarmer
                    // Any LC !> IDDrone
                    // Any Ascend !> +Karma

                    // Assign purely random check-unlock pair
                    if (key == "" || index == -1)
                    {
                        key = randomizerKey.First(k => k.Value == null).Key;
                        index = UnityEngine.Random.Range(0, remainingUnlocks.Count());
                    }
                    
                    randomizerKey[key] = remainingUnlocks[index];
                    remainingUnlocks.RemoveAt(index);
                }
                else
                {
                    // Ensure a possible path to all regions

                    string chosenCheck = null;
                    int chosenGateIndex = -1;
                    Unlock chosenGate = null;

                    try
                    {
                        // Logic to determine which checks are currently possible
                        List<string> possibleChecks = randomizerKey.Where(k =>
                        {
                            if (k.Value != null) return false;
                            if (LogicBlacklist.Contains(k.Key)) return false;
                            // If using Start Minimum Karma, don't consider echoes as always possible checks
                            if (Plugin.startMinKarma.Value && k.Key.StartsWith("Echo-")) return false;
                            // If this is a passage marked as 'easy', use it
                            if (GetFeasiblePassages(regionsAvailable, currentSlugcat).Any(c => k.Key == $"Passage-{c.value}")) return true;
                            foreach (string region in regionsAvailable)
                            {
                                if (k.Key.StartsWith("Echo-")
                                    && k.Key.Contains(region)) return true;
                                if (k.Key.StartsWith("Token-")
                                    && Plugin.Singleton.collectTokenHandler.availableTokens.ContainsKey(region)
                                    && Plugin.Singleton.collectTokenHandler.availableTokens[region].Any(c => k.Key == $"Token-{c}")) return true;
                                if (k.Key.StartsWith("Pearl-")
                                    && Plugin.Singleton.rainWorld.regionDataPearls.ContainsKey(region.ToLowerInvariant())
                                    && Plugin.Singleton.rainWorld.regionDataPearls[region.ToLowerInvariant()]
                                        .Contains(new DataPearl.AbstractDataPearl.DataPearlType(k.Key.Substring(6)))
                                    && Plugin.Singleton.rainWorld.regionDataPearlsAccessibility[region.ToLowerInvariant()]
                                        [Plugin.Singleton.rainWorld.regionDataPearls[region.ToLowerInvariant()].IndexOf(new DataPearl.AbstractDataPearl.DataPearlType(k.Key.Substring(6)))]
                                        .Contains(currentSlugcat)) return true;
                            }
                            return false;
                        }).ToList().ConvertAll(p => p.Key);
                        // Predicate to determine which gates we can open to ensure progress
                        List<Unlock> adjacentGates = remainingUnlocks.Where(u =>
                        {
                            if (u.Type == Unlock.UnlockType.Gate)
                            {
                                if (Plugin.OneWayGates.ContainsKey(u.ID)
                                || LogicBlacklist.Contains(u.ID))
                                    return false;

                                string[] split = Regex.Split(u.ID, "_");

                                // If exactly one of this gate's region connection matches one available
                                if (regionsAvailable.Contains(split[1]) ^ regionsAvailable.Contains(split[2]))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }).ToList();

                        if (possibleChecks.Count == 0
                            || adjacentGates.Count == 0)
                        {
                            Plugin.Log.LogError($"Ran out of checks or gates");
                            Plugin.Log.LogDebug($"Checks: {possibleChecks.Count} | Gates: {adjacentGates.Count}");

                            Plugin.Log.LogDebug($"Could not connect to:");
                            foreach (string region in allRegions.Except(regionsAvailable))
                            {
                                Plugin.Log.LogDebug($"\t{Region.GetRegionFullName(region, currentSlugcat)} ({region})");
                            }

                            Plugin.Log.LogDebug("Final State:");
                            foreach (KeyValuePair<string, Unlock> keyValue in randomizerKey)
                            {
                                if (keyValue.Value != null)
                                {
                                    Plugin.Log.LogDebug($"Check {keyValue.Key} assigned to {keyValue.Value?.ID}");
                                }
                            }
                            return false;
                        }

                        // Assign one of the possible checks to open a reachable gate
                        chosenCheck = possibleChecks[UnityEngine.Random.Range(0, possibleChecks.Count())];
                        chosenGateIndex = UnityEngine.Random.Range(0, adjacentGates.Count());
                        chosenGate = adjacentGates[chosenGateIndex];
                        randomizerKey[chosenCheck] = adjacentGates[chosenGateIndex];

                        regionsAvailable = regionsAvailable.Union(UpdateAvailableRegions(regionsAvailable, preOpenedGates, adjacentGates[chosenGateIndex].ID)).ToList();

                        remainingUnlocks.Remove(adjacentGates[chosenGateIndex]);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError("Encountered Error during generation");
                        Plugin.Log.LogDebug($"State Information\n\t Check: {chosenCheck}\n\t Chosen Gate index: {chosenGateIndex}\n\t Chosen Gate: {chosenGate?.ID}");
                        Plugin.Log.LogError(e);
                        return false;
                    }
                }
            }

            Plugin.Log.LogInfo("Generation Complete!");
            return true;
        }

        public void DebugBulkGeneration(int howMany)
        {
            Stopwatch sw = new Stopwatch();
            int numSucceeded = 0;
            int numFailed = 0;
            long sumRuntime = 0;

            Plugin.Log.LogDebug("Starting bulk generation test");
            for (int i = 0; i < howMany; i++)
            {
                sw.Restart();
                if (GenerateRandomizer())
                    numSucceeded++;
                else
                    numFailed++;
                sumRuntime += sw.ElapsedMilliseconds;
                Plugin.Log.LogDebug($"Gen complete in {sw.ElapsedMilliseconds} ms");

                // Reset key
                var keys = randomizerKey.Keys.ToList();
                foreach (var key in keys)
                {
                    randomizerKey[key] = null;
                }
            }

            Plugin.Log.LogDebug($"Bulk gen complete; \n\tSucceeded: {numSucceeded}\n\tFailed: {numFailed}\n\tRate: {(float)numSucceeded / howMany * 100}%\n\tAverage time: {(float)sumRuntime / howMany} ms");
        }

        private static List<string> UpdateAvailableRegions(List<string> availableRegions, List<string> preOpened, string newGate)
        {
            preOpened.Add(newGate);
            return UpdateAvailableRegions(availableRegions, preOpened);
        }

        private static List<string> UpdateAvailableRegions(List<string> availableRegions, List<string> preOpened)
        {
            List<string> newRegions = new List<string>();

            foreach (string gate in preOpened)
            {
                string[] connected = (from r in availableRegions
                                      where gate.Contains(r)
                                      select r).ToArray();

                if (connected.Length != 1)
                    continue;

                string[] split = Regex.Split(gate, "_");

                // Add whichever region is new
                newRegions.Add(connected[0] != split[1] ? split[1] : split[2]);
            }

            if (newRegions.Count > 0)
            {
                availableRegions = availableRegions.Union(newRegions).ToList();
                newRegions.AddRange(UpdateAvailableRegions(availableRegions, preOpened));
            }

            return newRegions;
        }

        private static List<WinState.EndgameID> GetFeasiblePassages(List<string> availableRegions, SlugcatStats.Name slugcat)
        {
            string[] noMonkRegions = new string[] { };
            string[] noSaintRegions = new string[] { };
            string[] noHunterRegions = new string[] { "SS" };
            string[] noOutlawRegions = new string[] { "SS" };

            List<WinState.EndgameID> passages = new List<WinState.EndgameID>() { WinState.EndgameID.Survivor };

            if (slugcat != SlugcatStats.Name.Red && slugcat != MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                if (availableRegions.Except(noMonkRegions).Count() > 0)
                {
                    passages.Add(WinState.EndgameID.Monk);
                }
                if (availableRegions.Except(noSaintRegions).Count() > 0)
                {
                    passages.Add(WinState.EndgameID.Saint);
                }
            }

            if (availableRegions.Except(noHunterRegions).Count() > 0)
            {
                passages.Add(WinState.EndgameID.Hunter);
            }
            if (availableRegions.Except(noOutlawRegions).Count() > 0)
            {
                passages.Add(WinState.EndgameID.Outlaw);
            }

            return passages;
        }

        public void InitSavedGame(SlugcatStats.Name slugcat, int saveSlot)
        {
            randomizerKey = SaveManager.LoadSavedGame(slugcat, saveSlot);

            // Set unlocked gates and passage tokens
            foreach (Unlock item in randomizerKey.Values)
            {
                switch (item.Type)
                {
                    case Unlock.UnlockType.Gate:
                        if (gatesStatus.ContainsKey(item.ID))
                        {
                            gatesStatus[item.ID] = gatesStatus[item.ID] || item.IsGiven; // If the gate was already opened by an identical unlock, keep it open
                        }
                        break;
                    case Unlock.UnlockType.Token:
                        if (passageTokensStatus.ContainsKey(new WinState.EndgameID(item.ID)))
                        {
                            passageTokensStatus[new WinState.EndgameID(item.ID)] = item.IsGiven;
                        }
                        break;
                    case Unlock.UnlockType.Karma:
                        if (item.IsGiven) IncreaseKarma();
                        break;
                    case Unlock.UnlockType.Glow:
                        if (item.IsGiven) _givenNeuronGlow = true;
                        break;
                    case Unlock.UnlockType.Mark:
                        if (item.IsGiven) _givenMark = true;
                        break;
                    case Unlock.UnlockType.HunterCycles:
                        if (item.IsGiven) _hunterBonusCyclesGiven++;
                        break;
                    case Unlock.UnlockType.IdDrone:
                        if (item.IsGiven) _givenRobo = true;
                        break;
                }
            }

            Plugin.Singleton.itemDeliveryQueue = SaveManager.LoadItemQueue(slugcat, saveSlot);
            Plugin.Singleton.lastItemDeliveryQueue = new Queue<Unlock.Item>(Plugin.Singleton.itemDeliveryQueue);
        }

        public static bool LoadBlacklist(SlugcatStats.Name slugcat)
        {
            if (!File.Exists(AssetManager.ResolveFilePath($"blacklist_{slugcat.value}.txt")))
            {
                Plugin.Log.LogWarning($"Failed to load blacklist from file for {slugcat.value}");
                if (!RegionBlacklists.ContainsKey(slugcat))
                    RegionBlacklists.Add(slugcat, new List<string>());

                if (!GateBlackLists.ContainsKey(slugcat))
                    GateBlackLists.Add(slugcat, new List<string>());

                if (!CheckBlacklists.ContainsKey(slugcat))
                    CheckBlacklists.Add(slugcat, new List<string>());
                return false;
            }

            string[] file = File.ReadAllLines(AssetManager.ResolveFilePath($"blacklist_{slugcat.value}.txt"));

            List<string> regions = new List<string>();
            List<string> gates = new List<string>();
            List<string> checks = new List<string>();
            foreach (string line in file)
            {
                if (!line.StartsWith("//") && line.Length > 0)
                {
                    string key;
                    try
                    {
                        key = Regex.Split(line, "-")[1];
                    }
                    catch
                    {
                        Plugin.Log.LogError("Blacklist values formatted incorrectly");
                        return false;
                    }

                    if (line.StartsWith("Region"))
                    {
                        regions.Add(key);
                    }
                    else if (line.StartsWith("Gate"))
                    {
                        gates.Add(key);
                    }
                    else
                    {
                        checks.Add(line);
                    }
                }
            }

            if (RegionBlacklists.ContainsKey(slugcat))
                RegionBlacklists[slugcat] = regions;
            else
                RegionBlacklists.Add(slugcat, regions);

            if (GateBlackLists.ContainsKey(slugcat))
                GateBlackLists[slugcat] = gates;
            else
                GateBlackLists.Add(slugcat, gates);

            if (CheckBlacklists.ContainsKey(slugcat))
                CheckBlacklists[slugcat] = checks;
            else
                CheckBlacklists.Add(slugcat, checks);

            return true;
        }

        public static string FindRandomStart(SlugcatStats.Name slugcat)
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
                        if (Region.GetFullRegionOrder().Contains(region)
                            && !RegionBlacklists[slugcat].Contains(region))
                        {
                            if (!contenders.ContainsKey(region))
                            {
                                contenders.Add(region, new List<string>());
                            }
                            contenders[region].Add(line);
                        }
                    }
                }

                string selectedRegion = contenders.Keys.ToArray()[UnityEngine.Random.Range(0, contenders.Count)];
                return contenders[selectedRegion][UnityEngine.Random.Range(0, contenders[selectedRegion].Count)];
            }

            return "SU_S01";
        }

        // Removes a region from a slugcat's blacklist
        // Used for options menu enabling regions
        public static void OverrideBlacklist(SlugcatStats.Name slugcat, string regionShort)
        {
            RegionBlacklists[slugcat].Remove(regionShort);

            List<string> toRemove = new List<string>();
            foreach (string gate in GateBlackLists[slugcat])
            {
                if (gate.Contains(regionShort))
                {
                    toRemove.Add(gate);
                }
            }
            GateBlackLists[slugcat] = GateBlackLists[slugcat].Except(toRemove).ToList();

            toRemove.Clear();
            foreach (string check in CheckBlacklists[slugcat])
            {
                if (check.Contains(regionShort))
                {
                    toRemove.Add(check);
                }
            }
            CheckBlacklists[slugcat] = CheckBlacklists[slugcat].Except(toRemove).ToList();
        }

        public override List<string> GetLocations()
        {
            return randomizerKey.Keys.ToList();
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
            Plugin.Singleton.notifQueue.Enqueue(randomizerKey[location].UnlockCompleteMessage());
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
