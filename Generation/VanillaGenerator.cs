using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watcher;
using Random = System.Random;

namespace RainWorldRandomizer.Generation
{
    public class VanillaGenerator(SlugcatStats.Name slugcat, SlugcatStats.Timeline timeline, OptionStruct options)
    {
        private const float OTHER_PROG_PLACEMENT_CHANCE = 0.2f;

        /// <summary>
        /// Constant storing the ID for the dummy start region used with non-random starts
        /// </summary>
        private const string START_REG = "StartDummy";

        public enum GenerationStep
        {
            NotStarted,
            InitializingState,
            BalancingItems,
            PlacingProg,
            PlacingFiller,
            Complete,
            FailedGen
        }

        private Task generationThread;
        public StringBuilder generationLog = new();

        public OptionStruct options = options;

        // Using inferior System.Random because it's instanced rather than static.
        // UnityEngine.Random doesn't play well with threads
        private Random randomState = new(options.useSeed
            ? options.seed.GetHashCode()
            : UnityEngine.Random.Range(0, int.MaxValue));

        private State state;
        private List<Item> itemsToPlace = [];
        public string customStartDen = "";
        public string generationSeed = options.seed;

        private Dictionary<string, RandoRegion> allRegions = [];
        public HashSet<string> AllGates { get; private set; } = [];
        public HashSet<string> UnplacedGates { get; private set; } = [];
        public HashSet<string> AllPassages { get; private set; } = [];
        public Dictionary<Location, Item> RandomizedGame { get; private set; } = [];

        public GenerationStep CurrentStage { get; private set; } = GenerationStep.NotStarted;

        public bool InProgress
        {
            get
            {
                return CurrentStage is > GenerationStep.NotStarted
                    and < GenerationStep.Complete;
            }
        }

        public Task BeginGeneration()
        {
            generationThread = new Task(Generate);
            generationThread.Start();
            return generationThread;
        }

        private void Generate()
        {
            generationLog.AppendLine("Begin Generation");
            generationLog.AppendLine($"Playing as {slugcat}");
            Stopwatch sw = Stopwatch.StartNew();

            InitializeState();
            ApplyRuleOverrides();
            DefineStartConditions();
            FinalizeState();
            BalanceItems();
            PlaceProgression();
            PlaceFiller();
            generationLog.AppendLine("Generation complete!");
            generationLog.AppendLine($"Gen time: {sw.ElapsedMilliseconds} ms");
            CurrentStage = GenerationStep.Complete;
        }

        /// <summary>
        /// Initializes all locations and progression items. 
        /// Most are generated from region data, but others like
        /// passages and story stuff are hard-coded
        /// </summary>
        private void InitializeState()
        {
            generationLog.AppendLine("INITIALIZE STATE");
            CurrentStage = GenerationStep.InitializingState;
            state = new State(slugcat, timeline, options);

            // Load Tokens
            if (options.useSandboxTokenChecks)
            {
                lock (CollectTokenHandler.AvailableTokens)
                {
                    if (!CollectTokenHandler.AvailableTokens.ContainsKey(slugcat))
                    {
                        CollectTokenHandler.LoadAvailableTokens(Plugin.Singleton.rainWorld, slugcat);
                    }
                }
            }

            // Regions loop
            List<string> slugcatRegions =
                [.. SlugcatStats.SlugcatStoryRegions(slugcat), .. SlugcatStats.SlugcatOptionalRegions(slugcat)];
            // Add Metropolis to region list if option set
            if (ModManager.MSC && options.allowMetroForOthers) slugcatRegions.Add("LC");
            // Remove regions from logic
            foreach (KeyValuePair<string, CustomLogicBuilder.RulePatch> region
                     in CustomLogicBuilder.GetLogicForSlugcat(slugcat).blacklistedRegions
                         .Where(region => region.Value.Collapse()?.IsPossible(state) is not (false or null)))
            {
                slugcatRegions.Remove(region.Key);
                generationLog.AppendLine($"Removed region {region.Key}");
            }

            foreach (string regionShort in Region.GetFullRegionOrder().Where(slugcatRegions.Contains))
            {
                HashSet<Location> regionLocations = [];

                // Create Echo locations
                if (LocationHelpers.MakeEchoOrSpinningTopLocation(slugcat, regionShort) is Location loc)
                {
                    regionLocations.Add(loc);
                }

                // Create Pearl locations
                if (options.usePearlChecks && (ModManager.MSC || slugcat != SlugcatStats.Name.Yellow))
                {
                    regionLocations.UnionWith(LocationHelpers.MakePearlLocations(slugcat, regionShort));
                }

                // Create Token locations
                if (options.useSandboxTokenChecks)
                {
                    regionLocations.UnionWith(LocationHelpers.MakeTokenLocations(slugcat, regionShort));
                }

                // Create Broadcast locations
                if (ModManager.MSC
                    && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear
                    && options.useSMTokens)
                {
                    regionLocations.UnionWith(LocationHelpers.MakeBroadcastLocations(slugcat, regionShort));
                }

                // Create Dev token locations
                if (ModManager.MSC && options.useDevTokenChecks)
                {
                    regionLocations.UnionWith(LocationHelpers.MakeDevTokenLocations(slugcat, regionShort));
                }

                // Create Karma flower locations
                if (slugcat != SlugcatStats.Name.Red && options.useKarmaFlowerChecks)
                {
                    regionLocations.UnionWith(LocationHelpers.MakeKarmaFlowerLocations(slugcat, regionShort));
                }

                // Find shelters
                (HashSet<string> shelters, HashSet<Location> locs) =
                    LocationHelpers.MakeShelters(timeline, regionShort, options.useShelterChecks);
                regionLocations.UnionWith(locs);

                // Create region
                allRegions[regionShort] = new RandoRegion(regionShort, regionLocations)
                {
                    shelters = shelters
                };
            }

            // Create Gate items
            (HashSet<string>, List<Item>) gatesTuple = 
                ModManager.Watcher && slugcat == WatcherEnums.SlugcatStatsName.Watcher 
                    ? ItemHelpers.MakeWarpConnections(allRegions)
                    : ItemHelpers.MakeGateConnections(slugcat, allRegions);
            AllGates.UnionWith(gatesTuple.Item1);
            itemsToPlace.AddRange(gatesTuple.Item2);

            // Passage Locations / Items
            Dictionary<string, AccessRule> passageRules = LocationHelpers.CreatePassageRules(slugcat);
            if (options.givePassageUnlocks)
            {
                itemsToPlace.AddRange([
                    .. passageRules.Select(kv => new Item(kv.Key, Item.Type.Passage, Item.Importance.Filler))
                ]);
            }

            if (options.usePassageChecks)
            {
                HashSet<Location> locs =
                    [.. passageRules.Select(kv => new Location($"Passage-{kv.Key}", Location.Type.Passage, kv.Value))];
                allRegions[RandoRegion.PASSAGE_REG] = new RandoRegion(RandoRegion.PASSAGE_REG, locs);
            }

            // Create Karma items
            int karmaInPool = 8 - (options.startMinKarma ? 0 : SlugcatStats.SlugcatStartingKarma(slugcat));
            karmaInPool += options.extraKarmaIncreases;
            for (int i = 0; i < karmaInPool; i++)
            {
                itemsToPlace.Add(new Item("Karma", Item.Type.Karma, Item.Importance.Progression));
            }

            // Create Food Quest locations
            if (ModManager.MSC && options.foodQuestBehavior >= RandoOptions.FoodQuestBehavior.Enabled)
            {
                allRegions.Add(RandoRegion.FOODQUEST_REG,
                    new RandoRegion(RandoRegion.FOODQUEST_REG,
                        LocationHelpers.MakeFoodQuest(slugcat,
                            options.usePassageChecks && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)));
            }

            // Create Special locations
            if (options.useSpecialChecks)
            {
                allRegions.Add(RandoRegion.SPECIAL_REG, new RandoRegion(RandoRegion.SPECIAL_REG, []));

                foreach (KeyValuePair<string, HashSet<Location>> kvp in LocationHelpers.MakeSpecial(slugcat))
                {
                    if (allRegions.TryGetValue(kvp.Key, out RandoRegion reg))
                    {
                        reg.allLocations.UnionWith(kvp.Value);
                    }
                    else
                    {
                        generationLog.AppendLine(
                            $"WARNING: Tried to add special location(s) to non-existent region {kvp.Key}");
                    }
                }
            }

            // Create Special items
            itemsToPlace.AddRange(ItemHelpers.MakeSpecialItems(slugcat, options));

            state.DefineLocs([.. allRegions.Values]);
        }

        /// <summary>
        /// Applies all manually defined logic, including subregion creation 
        /// and <see cref="AccessRule"/> changes for locations / connections
        /// </summary>
        private void ApplyRuleOverrides()
        {
            CustomLogicBuilder.LogicPackage customLogic = CustomLogicBuilder.GetLogicForSlugcat(slugcat);
            if (customLogic is null)
            {
                generationLog.AppendLine("Found no custom rule definitions, skipping custom rule step");
                return;
            }

            generationLog.AppendLine("APPLY SPECIAL RULES");

            // Individual locations
            foreach (KeyValuePair<string, CustomLogicBuilder.RulePatch> rule in customLogic.locationRules)
            {
                // Find the location by id and set its rule to the override
                Location loc = state.AllLocations.FirstOrDefault(l => l.ID == rule.Key);
                if (loc is null)
                {
                    generationLog.AppendLine($"Skipping override for non-existing location {rule.Key}");
                    continue;
                }

                rule.Value.Apply(ref loc.accessRule);
                generationLog.AppendLine($"Applied custom rule to location \"{rule.Key}\"");
            }

            // Create Subregions
            foreach (SubregionBlueprint subBlueprint in customLogic.newSubregions)
            {
                RandoRegion baseRegion = state.AllRegions.FirstOrDefault(r => r.ID == subBlueprint.baseRegion);
                if (baseRegion is null)
                {
                    generationLog.AppendLine(
                        $"Skipping creating subregion in non-existing region {subBlueprint.baseRegion}");
                    continue;
                }

                // Defined subregion locations / connections with invalid or not present IDs are simply ignored
                HashSet<Location> locs = [.. state.AllLocations.Where(l => subBlueprint.locations.Contains(l.ID))];
                HashSet<Connection> connections =
                    [.. state.AllConnections.Where(l => subBlueprint.connections.Contains(l.ID))];
                HashSet<string> shelters = [.. state.AllShelters.Where(s => subBlueprint.shelters.Contains(s))];

                state.DefineSubRegion(baseRegion, subBlueprint.ID, locs, connections, shelters, subBlueprint.rules);
                generationLog.AppendLine($"Created new subregion: {subBlueprint.ID}");
            }

            // Create manual Connections
            foreach (ConnectionBlueprint connectionBlueprint in customLogic.newConnections)
            {
                RandoRegion regionA = state.AllRegions.FirstOrDefault(r => r.ID == connectionBlueprint.regions[0]);
                RandoRegion regionB = state.AllRegions.FirstOrDefault(r => r.ID == connectionBlueprint.regions[1]);
                if (regionA is null || regionB is null)
                {
                    generationLog.AppendLine(
                        $"Skipping creation of connection to non-existing region {connectionBlueprint.regions[0]} or {connectionBlueprint.regions[1]}");
                    continue;
                }

                Connection connection = new(connectionBlueprint.ID, [regionA, regionB], connectionBlueprint.rules);
                connection.Create();
                generationLog.AppendLine($"Created new connection between {regionA.ID} and {regionB.ID}");
            }

            // Connection Overrides
            foreach (var rule in customLogic.connectionRules)
            {
                // Find the connection by id and set its rule to the override
                Connection connection = state.AllConnections.FirstOrDefault(c => c.ID == rule.Key);
                if (connection is null)
                {
                    generationLog.AppendLine($"Skipping override for non-existing connection {rule.Key}");
                    continue;
                }

                rule.Value.Item1.Apply(ref connection.requirements.Item1);
                rule.Value.Item2.Apply(ref connection.requirements.Item2);
                generationLog.AppendLine($"Applied custom rule to connection \"{rule.Key}\"");
            }
        }

        /// <summary>
        /// Create the starting region and its connections
        /// </summary>
        /// <exception cref="GenerationFailureException">Thrown if non-randomized starting den is invalid</exception>
        private void DefineStartConditions()
        {
            RandoRegion startRegion = new(START_REG, []);
            List<Connection> connectionsToAdd = [];

            if (state.RegionFromID(RandoRegion.PASSAGE_REG) is not null)
            {
                connectionsToAdd.Add(new("TO_PASSAGES", [startRegion, state.RegionFromID(RandoRegion.PASSAGE_REG)],
                    new AccessRule()));
            }

            if (state.RegionFromID(RandoRegion.SPECIAL_REG) is not null)
            {
                connectionsToAdd.Add(
                    new("TO_SPECIAL", [startRegion, state.RegionFromID(RandoRegion.SPECIAL_REG)], new AccessRule()));
            }

            if (state.RegionFromID(RandoRegion.FOODQUEST_REG) is not null)
            {
                connectionsToAdd.Add(new("TO_FOOD_QUEST", [startRegion, state.RegionFromID(RandoRegion.FOODQUEST_REG)],
                    new AccessRule()));
            }

            if (options.randomizeSpawnLocation)
            {
                // From state, find a random region that has at least one location, one shelter, and one connection that the player could leave with.
                // Additionally filter out regions manually set to not be start regions
                List<RandoRegion> contenderRegions =
                [
                    .. state.AllRegions.Where(r =>
                        r.allLocations.Count > 0
                        && r.shelters.Count > 0
                        && r.connections.Count > 0
                        && r.connections.All(c => c.TravelPossible(state, r))
                        && !CustomLogicBuilder.GetLogicForSlugcat(slugcat).blacklistedStarts.Contains(r.ID))
                ];
                RandoRegion chosenRegion = contenderRegions[randomState.Next(0, contenderRegions.Count)];
                // Choose a random shelter within the chosen region
                customStartDen = chosenRegion.shelters.ElementAt(randomState.Next(0, chosenRegion.shelters.Count));
                connectionsToAdd.Add(new("START_PATH", [startRegion, chosenRegion], new AccessRule()));

                generationLog.AppendLine(
                    $"Chosen {chosenRegion.ID} as random starting region, in shelter {customStartDen}");
            }
            else
            {
                // Find the default starting den within state's regions
                RandoRegion destination;
                if (ModManager.Watcher && slugcat == WatcherEnums.SlugcatStatsName.Watcher)
                {
                    // TODO: Find a more elegant way to get Watcher starting region 
                    destination = state.RegionFromID("WSKB");
                }
                else
                {
                    destination = state.RegionOfShelter(Constants.SlugcatDefaultStartingDen[slugcat])
                                              ?? throw new GenerationFailureException(
                                                  $"Failed to define starting region for {slugcat}, no region has shelter {Constants.SlugcatDefaultStartingDen[slugcat]}");
                }
                customStartDen = Constants.SlugcatDefaultStartingDen[slugcat];
                connectionsToAdd.Add(new Connection("START_PATH", [startRegion, destination], new AccessRule()));

                generationLog.AppendLine(
                    $"Starting in default region {Constants.SlugcatStartingRegion[slugcat]}, in shelter {Constants.SlugcatDefaultStartingDen[slugcat]}");
            }

            // Finalize connections
            connectionsToAdd.ForEach(c => c.Create());

            state.AllRegions.Add(startRegion);
            state.UnreachedRegions.Add(startRegion);
            state.AllConnections.UnionWith(connectionsToAdd);
        }

        /// <summary>
        /// Purge leftover impossible regions. No new additions to logic should be made after this point
        /// </summary>
        private void FinalizeState()
        {
            // Purge any regions that are now impossible to access
            bool anyPurged;
            do
            {
                anyPurged = false;
                foreach (RandoRegion region in state.AllRegions.ToList())
                {
                    if (!region.IsPossibleToReach(state))
                    {
                        generationLog.AppendLine(
                            $"Purged locations and connections for impossible subregion {region.ID}");
                        state.PurgeRegion(region);
                        anyPurged = true;
                    }
                }
            } while (anyPurged);

            // Purge any impossible individual locations
            foreach (Location loc in state.AllLocations.ToList())
            {
                if (!loc.accessRule.IsPossible(state))
                {
                    state.PurgeLocation(loc);
                    generationLog.AppendLine($"Removed impossible location: {loc.ID}");
                }
            }

            // Log all logic
            generationLog.AppendLine("Full logic:");
            foreach (RandoRegion region in state.AllRegions)
            {
                generationLog.AppendLine($"\t{region}");
            }
            
            generationLog.AppendLine();
        }

        /// <summary>
        /// Balance the number of locations and items to make them equal.
        /// This either adds random filler items, or removes non-critical items depending on starting counts
        /// </summary>
        /// <exception cref="GenerationFailureException">
        /// Thrown if there are not enough locations to place even bare minimum amount of items
        /// </exception>
        private void BalanceItems()
        {
            generationLog.AppendLine("BALANCE ITEMS");
            CurrentStage = GenerationStep.BalancingItems;
            generationLog.AppendLine(
                $"Item balancing start with {state.AllLocations.Count} locations and {itemsToPlace.Count} items");

            // Manage case where there are not enough locations for the amount of items in pool
            while (state.AllLocations.Count < itemsToPlace.Count)
            {
                // Remove a passage token
                List<Item> passageTokens = itemsToPlace.Where(i => i.type == Item.Type.Passage).ToList();
                if (passageTokens.Count > ManagerVanilla.MIN_PASSAGE_TOKENS)
                {
                    itemsToPlace.Remove(passageTokens.First());
                    continue;
                }

                // Cannot remove more passages, unlock gates
                List<Item> gateItems = itemsToPlace.Where(i => i.type == Item.Type.Gate).ToList();
                if (gateItems.Any())
                {
                    Item item = gateItems.ElementAt(randomState.Next(gateItems.Count));
                    itemsToPlace.Remove(item);
                    UnplacedGates.Add(item.id);
                    state.AddGate(item.ToString());
                    generationLog.AppendLine($"Pre-open gate: {item}");
                    continue;
                }

                generationLog.AppendLine("Too few locations present to make a valid seed");
                generationLog.AppendLine("Generation Failed");
                CurrentStage = GenerationStep.FailedGen;
                throw new GenerationFailureException("Too few locations present to make a valid seed");
            }

            List<Item> itemsToAdd = [];
            bool[] perksToAdd = options.expeditionPerks;

            // If there is space in item pool, add whichever perks we have selected in options
            if (state.AllLocations.Count >= itemsToPlace.Count + perksToAdd.Count(b => b))
            {
                for (int i = 0; i < perksToAdd.Length; i++)
                {
                    if (perksToAdd[i])
                    {
                        itemsToAdd.Add(new(((ManagerBase.ExpeditionPerks)i).ToString(), Item.Type.ExpPerk,
                            Item.Importance.Filler));
                    }
                }
            }

            int hunterCyclesAdded = 0;
            int trapsAdded = 0;
            int damageUpsAdded = 0;
            while (state.AllLocations.Count > itemsToPlace.Count + itemsToAdd.Count)
            {
                if (damageUpsAdded < options.numDamageIncreases)
                {
                    itemsToAdd.Add(new Item("DamageUpgrade", Item.Type.Other, Item.Importance.Filler));
                    damageUpsAdded++;
                }
                else if (slugcat == SlugcatStats.Name.Red
                         && hunterCyclesAdded < state.AllLocations.Count * options.hunterCyclesDensity)
                {
                    // Add cycle increases for Hunter
                    itemsToAdd.Add(new Item("HunterCycles", Item.Type.Other, Item.Importance.Filler));
                    hunterCyclesAdded++;
                }
                else if (trapsAdded < state.AllLocations.Count * options.trapsDensity)
                {
                    // Add trap items
                    itemsToAdd.Add(Item.RandomTrapItem(ref randomState));
                    trapsAdded++;
                }
                else
                {
                    // Add junk items
                    itemsToAdd.Add(Item.RandomJunkItem(ref randomState));
                }
            }

            if (itemsToAdd.Count > 0)
            {
                itemsToPlace.AddRange(itemsToAdd);
            }

            generationLog.AppendLine(
                $"Item balancing ended with {state.AllLocations.Count} locations and {itemsToPlace.Count} items");
        }

        /// <summary>
        /// The bulk of generation logic. Progression is placed in accessible locations until every location is reachable
        /// </summary>
        /// <exception cref="GenerationFailureException">
        /// Thrown if generation runs out of locations, 
        /// there is no more valid progression to place,
        /// or if function ends and not all locations are reachable
        /// </exception>
        private void PlaceProgression()
        {
            generationLog.AppendLine("PLACE PROGRESSION");
            CurrentStage = GenerationStep.PlacingProg;

            // Add the starting region and its connections into logic
            state.AddRegion(START_REG);

            // Continue until all regions are accessible
            // Note that a region is considered "accessible" by state regardless of
            // if there is some other rule blocking access to checks in that region
            while (!state.HasAllRegions())
            {
                // All gates adjacent to exactly one of the currently accessible regions
                // Additionally includes other progression to spread them throughout play
                List<Item> placeableGates = [];
                List<Item> placeableOtherProg = [];
                foreach (Item i in itemsToPlace.Where(i => i.importance == Item.Importance.Progression))
                {
                    if (i.type == Item.Type.Gate)
                    {
                        if (state.Gates.Contains(i.id)) continue;

                        // If there is a Connection associated with this gate ID
                        // and exactly one side is currently reachable, then consider this gate placeable.
                        if (state.AllConnections.Any(c =>
                                c.ID == i.id && c.ConnectedStatus == Connection.ConnectedLevel.OneReached))
                            //(state.HasRegion(Plugin.ProperRegionMap[gate[1]]) ^ state.HasRegion(Plugin.ProperRegionMap[gate[2]]))
                        {
                            placeableGates.Add(i);
                        }
                    }
                    else
                    {
                        placeableOtherProg.Add(i);
                    }
                }

                // Determine which type of prog to place
                bool useOtherProgThisCycle;
                if (placeableOtherProg.Count == 0) useOtherProgThisCycle = false;
                else if (placeableGates.Count == 0) useOtherProgThisCycle = true;
                // If we have locations to spare, chance to place less important "misc" progression
                else
                    useOtherProgThisCycle = state.AvailableLocations.Count > 5 &&
                                            randomState.NextDouble() < OTHER_PROG_PLACEMENT_CHANCE;
                List<Item> placeableProg = useOtherProgThisCycle ? placeableOtherProg : placeableGates;

                // Check if we have failed
                if (state.AvailableLocations.Count == 0 || placeableProg.Count == 0)
                {
                    string errorMessage = $"Ran out of " +
                                          $"{(placeableProg.Count == 0 ? "placeable progression" : "possible locations")}.";
                    generationLog.AppendLine($"ERROR: {errorMessage}");

                    generationLog.AppendLine("Failed to connect to:");
                    foreach (RandoRegion region in state.UnreachedRegions)
                    {
                        generationLog.AppendLine(
                            $"\t{(Plugin.RegionNamesMap.TryGetValue(region.ID, out string name) ? name : region.ID)}");
                    }

                    CurrentStage = GenerationStep.FailedGen;
                    throw new GenerationFailureException(errorMessage);
                }

                // Do the thing
                Location chosenLocation = state.PopRandomLocation(ref randomState);
                Item chosenItem = placeableProg[randomState.Next(placeableProg.Count)];
                RandomizedGame.Add(chosenLocation, chosenItem);

                // Update state with the new prog we added
                if (chosenItem.type == Item.Type.Gate) state.AddGate(chosenItem.id);
                else state.AddOtherProgItem(chosenItem.id);

                itemsToPlace.Remove(chosenItem);
                generationLog.AppendLine($"Placed progression \"{chosenItem.id}\" at {chosenLocation.ID}");
            }

            generationLog.AppendLine("PROGRESSION STEP 2");

            // Place the remaining progression items indiscriminately
            List<Item> placeableProg2 =
                [.. itemsToPlace.Where(i => i.importance == Item.Importance.Progression)];
            do
            {
                // Detect possible failure
                if (state.AvailableLocations.Count == 0)
                {
                    generationLog.AppendLine($"ERROR: Ran out of possible locations");

                    generationLog.AppendLine("Failed to acquire access to:");
                    foreach (Location loc in state.UnreachedLocations)
                    {
                        generationLog.AppendLine($"\t{loc.ID}; {loc.accessRule}");
                    }

                    CurrentStage = GenerationStep.FailedGen;
                    throw new GenerationFailureException("Ran out of possible locations");
                }

                // Do the thing
                int chosenItemIndex = randomState.Next(placeableProg2.Count);
                Location chosenLocation = state.PopRandomLocation(ref randomState);
                Item chosenItem = placeableProg2[chosenItemIndex];
                RandomizedGame.Add(chosenLocation, chosenItem);
                placeableProg2.RemoveAt(chosenItemIndex);

                // Update state with the new prog we added
                // Gates won't give any new locations by this point
                if (chosenItem.type == Item.Type.Gate) state.AddGate(chosenItem.id);
                else state.AddOtherProgItem(chosenItem.id);

                itemsToPlace.Remove(chosenItem);
                generationLog.AppendLine($"Placed progression \"{chosenItem.id}\" at {chosenLocation.ID}");
            } while (placeableProg2.Count > 0);

            if (state.UnreachedLocations.Count > 0)
            {
                generationLog.AppendLine($"ERROR: Progression step ended with impossible locations");
                generationLog.AppendLine("Failed to acquire access to:");
                foreach (Location loc in state.UnreachedLocations)
                {
                    generationLog.AppendLine($"\t{loc.ID}; {loc.accessRule}");
                }

                CurrentStage = GenerationStep.FailedGen;
                throw new GenerationFailureException("Failed to reach all locations");
            }
        }

        /// <summary>
        /// Last stage of generation, filler items are placed and game becomes complete
        /// </summary>
        private void PlaceFiller()
        {
            // All progression is placed at this point, state has full access
            generationLog.AppendLine("PLACE FILLER");
            CurrentStage = GenerationStep.PlacingFiller;

            //generationLog.AppendLine($"Remaining locations to fill: {state.AvailableLocations.Count}");
            //generationLog.AppendLine($"Remaining items to place: {itemsToPlace.Count}");

            // Just place the filler purely at random
            while (state.AvailableLocations.Count > 0)
            {
                Location chosenLocation = state.PopRandomLocation(ref randomState);
                Item chosenItem = itemsToPlace[randomState.Next(itemsToPlace.Count)];
                RandomizedGame.Add(chosenLocation, chosenItem);
                itemsToPlace.Remove(chosenItem);
                generationLog.AppendLine($"Placed filler \"{chosenItem.id}\" at {chosenLocation.ID}");
            }
        }

        public Dictionary<string, Unlock> GetCompletedSeed()
        {
            if (CurrentStage != GenerationStep.Complete) return null;

            Dictionary<string, Unlock> output = [];
            foreach (KeyValuePair<Location, Item> placement in RandomizedGame)
            {
                if (!output.ContainsKey(placement.Key.ID))
                {
                    output.Add(placement.Key.ID, ItemToUnlock(placement.Value));
                }
                else
                {
                    Plugin.Log.LogWarning($"Tried to place double location: {placement.Key.ID}");
                }
            }

            return output;
        }

        public static Unlock ItemToUnlock(Item item)
        {
            Unlock.UnlockType outputType = null;
            switch (item.type)
            {
                case Item.Type.Gate:
                    outputType = Unlock.UnlockType.Gate;
                    break;
                case Item.Type.Passage:
                    outputType = Unlock.UnlockType.Token;
                    break;
                case Item.Type.Karma:
                    outputType = Unlock.UnlockType.Karma;
                    break;
                case Item.Type.Object:
                    outputType = item.id.StartsWith("PearlObject-")
                        ? Unlock.UnlockType.ItemPearl
                        : Unlock.UnlockType.Item;
                    break;
                case Item.Type.Trap:
                    outputType = Unlock.UnlockType.Trap;
                    break;
                case Item.Type.ExpPerk:
                    outputType = Unlock.UnlockType.ExpeditionPerk;
                    break;
                case Item.Type.Other:
                    if (ExtEnumBase.TryParse(typeof(Unlock.UnlockType), item.id, false, out ExtEnumBase type))
                    {
                        outputType = (Unlock.UnlockType)type;
                    }
                    else
                    {
                        Plugin.Log.LogError($"ItemToUnlock could not find matching UnlockType for {item.id}");
                        return null;
                    }

                    break;
            }

            if (outputType == Unlock.UnlockType.Item)
            {
                return new Unlock(Unlock.UnlockType.Item, Unlock.IDToItem(item.id.Substring(7)));
            }

            if (outputType == Unlock.UnlockType.ItemPearl)
            {
                return new Unlock(Unlock.UnlockType.ItemPearl, Unlock.IDToItem(item.id.Substring(12), true));
            }

            return new Unlock(outputType, item.id);
        }

        public class GenerationFailureException : Exception
        {
            public GenerationFailureException()
            {
            }

            public GenerationFailureException(string error) : base(error)
            {
            }
        }
    }
}