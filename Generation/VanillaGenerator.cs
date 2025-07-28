using MonoMod.Utils;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Random = System.Random;

namespace RainWorldRandomizer.Generation
{
    public class VanillaGenerator
    {
        public const float OTHER_PROG_PLACEMENT_CHANCE = 0.2f;
        /// <summary> Constant storing the ID for the Passage region </summary>
        public const string PASSAGE_REG = "Passages";
        /// <summary> Constant storing the ID for the Food Quest region </summary>
        public const string FOODQUEST_REG = "FoodQuest";
        /// <summary> Constant storing the ID for the Special region </summary>
        public const string SPECIAL_REG = "Special";
        /// <summary>
        /// Constant storing the ID for the dummy start region used with non-random starts
        /// </summary>
        public const string START_REG = "StartDummy";

        /// <summary>
        /// Used to override rules for locations. To modify the rules of a location, add its location ID to this dict with the new rule it should follow.
        /// </summary>
        public static Dictionary<string, AccessRule> globalRuleOverrides = [];
        /// <summary>
        /// Used to override rules for locations as specific slugcats. Applies on top of <see cref="globalRuleOverrides"/>, 
        /// taking priority over all if playing as the relevant slugcat
        /// </summary>
        public static Dictionary<SlugcatStats.Name, Dictionary<string, AccessRule>> slugcatRuleOverrides = [];
        /// <summary>
        /// Used to override rules for a connection. To modify the rules of a connection, add its connection ID to this dict with the new rules it should follow. 
        /// Rules must be an array of size 2, 0 being the rule for travelling right, and 1 being the rule for travelling left
        /// </summary>
        public static Dictionary<string, AccessRule[]> connectionRuleOverrides = [];
        /// <summary>
        /// Used to divide regions into multiple parts. To create ome, define a SubregionBlueprint with the base region, 
        /// a new subregion ID, and the locations and connections it should affect
        /// </summary>
        public static List<SubregionBlueprint> manualSubregions = [];
        /// <summary>
        /// Used to create connections where they wouldn't be auto-generated.
        /// </summary>
        public static List<ConnectionBlueprint> manualConnections = [];

        /// <summary>
        /// Combination of <see cref="globalRuleOverrides"/> and <see cref="slugcatRuleOverrides"/> populated in instance constructor.
        /// Any additions to rule overrides must be completed by the time the generator instance is created
        /// </summary>
        private Dictionary<string, AccessRule> ruleOverrides = [];

        private SlugcatStats.Name slugcat;
        private SlugcatStats.Timeline timeline;

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
        public GenerationStep CurrentStage { get; private set; }
        public bool InProgress
        {
            get
            {
                return CurrentStage is > GenerationStep.NotStarted
                    and < GenerationStep.Complete;
            }
        }

        private Task generationThread;
        private Random randomState;

        private State state;
        private List<Item> itemsToPlace = [];
        private Dictionary<string, RandoRegion> allRegions = [];
        //private HashSet<string> AllRegions = [];
        public HashSet<string> AllGates { get; private set; }
        public HashSet<string> UnplacedGates { get; private set; }
        public HashSet<string> AllPassages { get; private set; }
        public Dictionary<Location, Item> RandomizedGame { get; private set; }

        public StringBuilder generationLog = new();
        public string customStartDen = "";
        public int generationSeed;
        public bool logVerbose;


        public VanillaGenerator(SlugcatStats.Name slugcat, SlugcatStats.Timeline timeline, int generationSeed = 0)
        {
            this.slugcat = slugcat;
            this.timeline = timeline;
            CurrentStage = GenerationStep.NotStarted;

            AllGates = [];
            UnplacedGates = [];
            AllPassages = [];
            RandomizedGame = [];

            // Initialize RNG
            // Using inferior System.Random because it's instanced rather than static.
            // UnityEngine.Random doesn't play well with threads
            this.generationSeed = generationSeed;
            randomState = new Random(generationSeed);

            // Combine custom rules together
            ruleOverrides = globalRuleOverrides;
            // Slugcat specific rules take priority over global ones
            foreach (var slugcatRule in slugcatRuleOverrides[slugcat])
            {
                if (ruleOverrides.ContainsKey(slugcatRule.Key))
                {
                    ruleOverrides[slugcatRule.Key] = slugcatRule.Value;
                }
                else
                {
                    ruleOverrides.Add(slugcatRule.Key, slugcatRule.Value);
                }
            }
        }

        public Task BeginGeneration(bool logVerbose = false)
        {
            this.logVerbose = logVerbose;
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
            state = new State(slugcat, timeline, RandoOptions.StartMinimumKarma ? 0 : SlugcatStats.SlugcatStartingKarma(slugcat));

            // Load Tokens
            if (RandoOptions.UseSandboxTokenChecks)
            {
                lock (Plugin.Singleton.collectTokenHandler)
                {
                    if (Plugin.Singleton.collectTokenHandler.tokensLoadedFor != slugcat)
                    {
                        Plugin.Singleton.collectTokenHandler.LoadAvailableTokens(Plugin.Singleton.rainWorld, slugcat);
                    }
                }
            }

            // Regions loop
            bool regionKitEchoes = RandoOptions.UseEchoChecks && RegionKitCompatibility.Enabled;
            bool doPearlLocations = RandoOptions.UsePearlChecks && (ModManager.MSC || slugcat != SlugcatStats.Name.Yellow);
            bool spearBroadcasts = ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear && RandoOptions.UseSMBroadcasts;
            List<string> slugcatRegions = [.. SlugcatStats.SlugcatStoryRegions(slugcat), .. SlugcatStats.SlugcatOptionalRegions(slugcat)];
            // Add Metropolis to region list if option set
            if (ModManager.MSC && RandoOptions.ForceOpenMetropolis) slugcatRegions.Add("LC");
            // Remove Submerged Superstructure from list if not desired
            if (ModManager.MSC && !RandoOptions.ForceOpenSubmerged && slugcat != MoreSlugcatsEnums.SlugcatStatsName.Rivulet) slugcatRegions.Remove("MS");

            foreach (string regionShort in Region.GetFullRegionOrder())
            {
                HashSet<Location> regionLocations = [];

                // Filter out slugcat inaccessible regions unless there is a special rule defined
                if (!slugcatRegions.Contains(regionShort)) continue;

                string regionLower = regionShort.ToLowerInvariant();

                // Add Echoes from RegionKit if present
                if (regionKitEchoes && RegionKitCompatibility.RegionHasEcho(regionShort, slugcat))
                {
                    regionLocations.Add(new($"Echo-{regionShort}", Location.Type.Echo, new()));
                }

                // Create Pearl locations
                if (doPearlLocations && Plugin.Singleton.rainWorld.regionDataPearls.ContainsKey(regionLower))
                {
                    for (int i = 0; i < Plugin.Singleton.rainWorld.regionDataPearls[regionLower].Count; i++)
                    {
                        if (Plugin.Singleton.rainWorld.regionDataPearlsAccessibility[regionLower][i].Contains(slugcat))
                        {
                            regionLocations.Add(new($"Pearl-{Plugin.Singleton.rainWorld.regionDataPearls[regionLower][i].value}",
                                Location.Type.Pearl, new()));
                        }
                    }
                }

                // Create Token locations
                if (RandoOptions.UseSandboxTokenChecks
                    && Plugin.Singleton.collectTokenHandler.availableTokens.ContainsKey(regionShort))
                {
                    foreach (string token in Plugin.Singleton.collectTokenHandler.availableTokens[regionShort])
                    {
                        regionLocations.Add(new Location($"Token-{token}", Location.Type.Token, new()));
                    }
                }

                // Create Broadcast locations
                if (spearBroadcasts && Plugin.Singleton.rainWorld.regionGreyTokens.ContainsKey(regionLower))
                {
                    foreach (ChatlogData.ChatlogID token in Plugin.Singleton.rainWorld.regionGreyTokens[regionLower])
                    {
                        regionLocations.Add(new Location($"Broadcast-{token.value}", Location.Type.Token, new()));
                    }
                }

                // Create Dev token locations
                if (ModManager.MSC && RandoOptions.UseDevTokenChecks && TokenCachePatcher.regionDevTokens.ContainsKey(regionLower))
                {
                    for (int i = 0; i < TokenCachePatcher.regionDevTokens[regionLower].Count; i++)
                    {
                        if (TokenCachePatcher.regionDevTokensAccessibility[regionLower][i].Contains(slugcat))
                        {
                            regionLocations.Add(new Location($"DevToken-{TokenCachePatcher.regionDevTokens[regionLower][i]}", Location.Type.Token, new()));
                        }
                    }
                }

                // Find shelters
                HashSet<string> shelters = [];
                for (int i = 0; i < TokenCachePatcher.regionShelters[regionLower].Count; i++)
                {
                    if (TokenCachePatcher.regionSheltersAccessibility[regionLower][i].Contains(timeline))
                    {
                        shelters.Add(TokenCachePatcher.regionShelters[regionLower][i]);
                        // Create Shelter locations
                        if (RandoOptions.UseShelterChecks)
                        {
                            regionLocations.Add(new Location($"Shelter-{TokenCachePatcher.regionShelters[regionLower][i]}", Location.Type.Shelter, new()));
                        }
                    }
                }

                // Create region
                allRegions[regionShort] = new(regionShort, regionLocations)
                {
                    shelters = shelters
                };
            }

            // Create Gate items
            foreach (string karmaLock in Plugin.Singleton.rainWorld.progression.karmaLocks)
            {
                string gate = Regex.Split(karmaLock, " : ")[0];
                string[] split = Regex.Split(gate, "_");
                if (split.Length < 3) continue; // Ignore abnormal gates
                string[] regionShorts = [split[1], split[2]];

                // Skip if gate already accounted for
                if (AllGates.Contains(gate)) continue;
                // Gates that have to always be open to avoid softlocks

                bool skipThisGate = false;
                foreach (string regionShort in regionShorts)
                {
                    // If this region does not exist in the timeline
                    // and is not an alias of an existing region, skip the gate
                    string properRegionShort = Plugin.ProperRegionMap.TryGetValue(regionShort, out string alias) ? alias : regionShort;
                    skipThisGate |= !allRegions.ContainsKey(properRegionShort);

                    // If this gate is impossible to reach for the current slugcat, skip it
                    skipThisGate |= TokenCachePatcher.GetRoomAccessibility(regionShort).TryGetValue(gate.ToLowerInvariant(), out List<SlugcatStats.Name> accessibleTo)
                        && !accessibleTo.Contains(slugcat);
                }

                if (skipThisGate) continue;
                regionShorts[0] = Plugin.ProperRegionMap[regionShorts[0]];
                regionShorts[1] = Plugin.ProperRegionMap[regionShorts[1]];

                // Create connection
                // Gates defined as always open are given free passage,
                // though there is likely a custom one-way definition
                Connection connection = new(gate,
                [
                    allRegions[regionShorts[0]],
                    allRegions[regionShorts[1]]
                ], Constants.ForceOpenGates.Contains(gate) ? new AccessRule() : new GateAccessRule(gate));
                connection.Create();

                AllGates.Add(gate);

                itemsToPlace.Add(new Item(gate, Item.Type.Gate, Item.Importance.Progression));
            }

            Dictionary<string, AccessRule> passageRules = CreatePassageRules();
            if (RandoOptions.GivePassageItems)
            {
                itemsToPlace.AddRange([.. passageRules.Select(kv => new Item(kv.Key, Item.Type.Passage, Item.Importance.Filler))]);
            }
            if (RandoOptions.UsePassageChecks)
            {
                HashSet<Location> locs = [.. passageRules.Select(kv => new Location($"Passage-{kv.Key}", Location.Type.Passage, kv.Value))];
                allRegions[PASSAGE_REG] = new RandoRegion(PASSAGE_REG, locs);
            }

            // Create Echo locations
            if (RandoOptions.UseEchoChecks)
            {
                foreach (string echo in ExtEnumBase.GetNames(typeof(GhostWorldPresence.GhostID)))
                {
                    // No worry for duplicates from the RegionKit check,
                    // as the HashSet should ignore duplicate additions
                    if (!echo.Equals("NoGhost")
                        && World.CheckForRegionGhost(slugcat, echo)
                        && allRegions.ContainsKey(echo))
                    {
                        Location echoLoc = new($"Echo-{echo}", Location.Type.Echo, new());
                        allRegions[echo].allLocations.Add(echoLoc);
                    }
                }
            }

            // Create Karma items
            int karmaInPool = 8 - (RandoOptions.StartMinimumKarma ? 0 : SlugcatStats.SlugcatStartingKarma(slugcat));
            karmaInPool += RandoOptions.ExtraKarmaIncreases;
            for (int i = 0; i < karmaInPool; i++)
            {
                itemsToPlace.Add(new Item("Karma", Item.Type.Karma, Item.Importance.Progression));
            }

            // TODO: Add support for expanded food quest
            // Create Food Quest locations
            if (ModManager.MSC && RandoOptions.UseFoodQuest)
            {
                List<AccessRule> allGourmRules = [];
                HashSet<Location> foodQuestLocs = [];
                foreach (WinState.GourmandTrackerData data in WinState.GourmandPassageTracker)
                {
                    if (data.type == AbstractPhysicalObject.AbstractObjectType.Creature)
                    {
                        List<CreatureAccessRule> rules = [];
                        foreach (CreatureTemplate.Type type in data.crits)
                        {
                            rules.Add(new CreatureAccessRule(type));
                        }

                        AccessRule rule;
                        if (rules.Count > 1) rule = new CompoundAccessRule([.. rules], CompoundAccessRule.CompoundOperation.Any);
                        else rule = rules[0];

                        allGourmRules.Add(rule);
                        foodQuestLocs.Add(new Location($"FoodQuest-{data.crits[0].value}", Location.Type.Food, rule));
                    }
                    else
                    {
                        AccessRule rule = new ObjectAccessRule(data.type);
                        allGourmRules.Add(rule);
                        foodQuestLocs.Add(new Location($"FoodQuest-{data.type.value}", Location.Type.Food, rule));
                    }
                }

                allRegions.Add(FOODQUEST_REG, new(FOODQUEST_REG, foodQuestLocs));

                if (RandoOptions.UsePassageChecks)
                {
                    Location gourmPassage = new("Passage-Gourmand", Location.Type.Passage,
                        new CompoundAccessRule([.. allGourmRules], CompoundAccessRule.CompoundOperation.All));
                    allRegions[PASSAGE_REG].allLocations.Add(gourmPassage);
                }
            }

            // Create Special locations
            if (RandoOptions.UseSpecialChecks)
            {
                HashSet<Location> specialLocs = [];

                specialLocs.Add(new Location("Eat_Neuron", Location.Type.Story, new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer)));

                switch (slugcat.value)
                {
                    // Normal Iterator goals
                    case "White":
                    case "Yellow":
                    case "Gourmand":
                    case "Sofanthiel":
                        allRegions["SL"].allLocations.Add(new("Meet_LttM", Location.Type.Story, new("The_Mark")));
                        allRegions["SS"].allLocations.Add(new("Meet_FP", Location.Type.Story, new()));
                        break;
                    // Spear finds LttM in LM
                    case "Spear":
                        allRegions["LM"].allLocations.Add(new("Meet_LttM_Spear", Location.Type.Story, new()));
                        allRegions["SS"].allLocations.Add(new("Meet_FP", Location.Type.Story, new()));
                        break;
                    // Hunter Saves LttM, which is a seperate check
                    case "Red":
                        allRegions["SL"].allLocations.Add(new("Save_LttM", Location.Type.Story, new("Object-NSHSwarmer")));
                        allRegions["SL"].allLocations.Add(new("Meet_LttM", Location.Type.Story, new("The_Mark")));
                        allRegions["SS"].allLocations.Add(new("Meet_FP", Location.Type.Story, new()));
                        break;
                    // Artificer cannot meet LttM
                    case "Artificer":
                        allRegions["SS"].allLocations.Add(new("Meet_FP", Location.Type.Story, new()));
                        break;
                    // Rivulet does a murder in RM, seperate check
                    case "Rivulet":
                        allRegions["SL"].allLocations.Add(new("Meet_LttM", Location.Type.Story, new("The_Mark")));
                        if (RandoOptions.UseEnergyCell)
                        {
                            allRegions["RM"].allLocations.Add(new("Kill_FP", Location.Type.Story, new()));
                        }
                        break;
                    // Saint has 2 seperate checks for ascending
                    case "Saint":
                        allRegions["SL"].allLocations.Add(new("Ascend_LttM", Location.Type.Story, new KarmaAccessRule(10)));
                        allRegions["CL"].allLocations.Add(new("Ascend_FP", Location.Type.Story, new KarmaAccessRule(10)));
                        break;
                }

                allRegions.Add(SPECIAL_REG, new(SPECIAL_REG, specialLocs));
            }

            // Create Special items
            if (!ModManager.MSC || slugcat != MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                itemsToPlace.Add(new Item("Neuron_Glow", Item.Type.Other, Item.Importance.Progression));
                itemsToPlace.Add(new Item("The_Mark", Item.Type.Other, Item.Importance.Progression));
            }

            switch (slugcat.value)
            {
                case "Red":
                    itemsToPlace.Add(new Item("Object-NSHSwarmer", Item.Type.Object, Item.Importance.Progression));
                    itemsToPlace.Add(new Item("PearlObject-Red_stomach", Item.Type.Object, Item.Importance.Progression));
                    break;
                case "Artificer":
                    itemsToPlace.Add(new Item("IdDrone", Item.Type.Other, Item.Importance.Progression));
                    break;
                case "Rivulet":
                    if (RandoOptions.UseEnergyCell)
                    {
                        itemsToPlace.Add(new Item("Object-EnergyCell", Item.Type.Object, Item.Importance.Progression));
                        itemsToPlace.Add(new Item("DisconnectFP", Item.Type.Other, Item.Importance.Progression));
                    }
                    break;
                case "Spear":
                    itemsToPlace.Add(new Item("PearlObject-Spearmasterpearl", Item.Type.Object, Item.Importance.Progression));
                    itemsToPlace.Add(new Item("RewriteSpearPearl", Item.Type.Other, Item.Importance.Progression));
                    break;
            }

            state.DefineLocs([.. allRegions.Values]);
        }

        /// <summary>
        /// Applies all manually defined logic, including subregion creation 
        /// and <see cref="AccessRule"/> changes for locations / connections
        /// </summary>
        private void ApplyRuleOverrides()
        {
            generationLog.AppendLine("APPLY SPECIAL RULES");

            // Individual locations
            foreach (var rule in ruleOverrides)
            {
                // Find the location by id and set its rule to the override
                Location loc = state.AllLocations.FirstOrDefault(l => l.ID == rule.Key);
                if (loc is null)
                {
                    generationLog.AppendLine($"Skipping override for non-existing location {rule.Key}");
                    continue;
                }

                if (rule.Value.IsPossible(state))
                {
                    loc.accessRule = rule.Value;
                    generationLog.AppendLine($"Applied custom rule to location: {rule.Key}");
                }
                else
                {
                    // Impossible locations are removed from state
                    state.PurgeLocation(loc);
                    generationLog.AppendLine($"Removed impossible location: {rule.Key}");
                }
            }

            // Create Subregions
            foreach (SubregionBlueprint subBlueprint in manualSubregions)
            {
                RandoRegion baseRegion = state.AllRegions.FirstOrDefault(r => r.ID == subBlueprint.baseRegion);
                if (baseRegion is null)
                {
                    generationLog.AppendLine($"Skipping creating subregion in non-existing region {subBlueprint.baseRegion}");
                    continue;
                }

                // Defined subregion locations / connections with invalid or not present IDs are simply ignored
                HashSet<Location> locs = [.. state.AllLocations.Where(l => subBlueprint.locations.Contains(l.ID))];
                HashSet<Connection> connections = [.. state.AllConnections.Where(l => subBlueprint.connections.Contains(l.ID))];
                HashSet<string> shelters = [.. state.AllShelters.Where(s => subBlueprint.shelters.Contains(s))];

                state.DefineSubRegion(baseRegion, subBlueprint.ID, locs, connections, shelters, subBlueprint.rules);
                generationLog.AppendLine($"Created new subregion: {subBlueprint.ID}");
            }

            // Connection Overrides
            foreach (var rule in connectionRuleOverrides)
            {
                // Find the connection by id and set its rule to the override
                Connection connection = state.AllConnections.FirstOrDefault(c => c.ID == rule.Key);
                if (connection is null)
                {
                    generationLog.AppendLine($"Skipping override for non-existing connection {rule.Key}");
                    continue;
                }
                if (rule.Value.Length != 2)
                {
                    generationLog.AppendLine($"Connection override for {rule.Key} is invalid as rules are not of length 2");
                    continue;
                }

                connection.requirements = rule.Value;
                generationLog.AppendLine($"Applied custom rule to connection: {rule.Key}");
            }

            // Create manual Connections
            foreach (ConnectionBlueprint connectionBlueprint in manualConnections)
            {
                RandoRegion regionA = state.AllRegions.FirstOrDefault(r => r.ID == connectionBlueprint.regions[0]);
                RandoRegion regionB = state.AllRegions.FirstOrDefault(r => r.ID == connectionBlueprint.regions[1]);
                if (regionA is null || regionB is null)
                {
                    generationLog.AppendLine($"Skipping creation of connection to non-existing region {connectionBlueprint.regions[0]} or {connectionBlueprint.regions[1]}");
                    continue;
                }

                Connection connection = new(connectionBlueprint.ID, [regionA, regionB], connectionBlueprint.rules);
                connection.Create();
                generationLog.AppendLine($"Created new connection between {regionA.ID} and {regionB.ID}");
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

            if (state.RegionFromID(PASSAGE_REG) is not null)
            {
                connectionsToAdd.Add(new("TO_PASSAGES", [startRegion, state.RegionFromID(PASSAGE_REG)], new AccessRule()));
            }
            if (state.RegionFromID(SPECIAL_REG) is not null)
            {
                connectionsToAdd.Add(new("TO_SPECIAL", [startRegion, state.RegionFromID(SPECIAL_REG)], new AccessRule()));
            }
            if (state.RegionFromID(FOODQUEST_REG) is not null)
            {
                connectionsToAdd.Add(new("TO_FOOD_QUEST", [startRegion, state.RegionFromID(FOODQUEST_REG)], new AccessRule()));
            }

            if (RandoOptions.RandomizeSpawnLocation)
            {
                // From state, find a random region that has at least one location, one shelter, and one connection
                List<RandoRegion> contenderRegions = [.. state.AllRegions.Where(r => r.allLocations.Count > 0 && r.shelters.Count > 0 && r.connections.Count > 0)];
                RandoRegion chosenRegion = contenderRegions[randomState.Next(0, contenderRegions.Count)];
                // Choose a random shelter within the chosen region
                customStartDen = chosenRegion.shelters.ElementAt(randomState.Next(0, chosenRegion.shelters.Count));
                connectionsToAdd.Add(new("START_PATH", [startRegion, chosenRegion], new AccessRule()));

                generationLog.AppendLine($"Chosen {chosenRegion.ID} as random starting region, in shelter {customStartDen}");
            }
            else
            {
                // Find the default starting den within state's regions
                RandoRegion destination = state.RegionOfShelter(Constants.SlugcatDefaultStartingDen[slugcat])
                    ?? throw new GenerationFailureException($"Failed to define starting region for {slugcat}, no region has shelter {Constants.SlugcatDefaultStartingDen[slugcat]}");
                customStartDen = Constants.SlugcatDefaultStartingDen[slugcat];
                connectionsToAdd.Add(new("START_PATH", [startRegion, destination], new AccessRule()));

                generationLog.AppendLine($"Starting in default region {Constants.SlugcatStartingRegion[slugcat]}, in shelter {Constants.SlugcatDefaultStartingDen[slugcat]}");
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
                        generationLog.AppendLine($"Purged locations and connections for impossible subregion {region.ID}");
                        state.PurgeRegion(region);
                        anyPurged = true;
                    }
                }
            } while (anyPurged);

            // Log all logic
            if (logVerbose)
            {
                generationLog.AppendLine("Full logic:");
                foreach (RandoRegion region in state.AllRegions)
                {
                    generationLog.AppendLine($"\t{region}");
                }
                generationLog.AppendLine();
            }
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
            generationLog.AppendLine($"Item balancing start with {state.AllLocations.Count} locations and {itemsToPlace.Count} items");

            // Manage case where there are not enough locations for the amount of items in pool
            while (state.AllLocations.Count < itemsToPlace.Count)
            {
                // Remove a passage token
                IEnumerable<Item> passageTokens = itemsToPlace.Where(i => i.type == Item.Type.Passage);
                if (passageTokens.Count() > ManagerVanilla.MIN_PASSAGE_TOKENS)
                {
                    itemsToPlace.Remove(passageTokens.First());
                    continue;
                }

                // Cannot remove more passages, unlock gates
                IEnumerable<Item> gateItems = itemsToPlace.Where(i => i.type == Item.Type.Gate);
                if (gateItems.Count() > 0)
                {
                    Item item = gateItems.ElementAt(randomState.Next(gateItems.Count()));
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
            int hunterCyclesAdded = 0;
            while (state.AllLocations.Count > itemsToPlace.Count + itemsToAdd.Count)
            {
                if (slugcat == SlugcatStats.Name.Red
                    && hunterCyclesAdded < state.AllLocations.Count * RandoOptions.HunterCycleIncreaseDensity)
                {
                    // Add cycle increases for Hunter
                    itemsToAdd.Add(new Item("HunterCycles", Item.Type.Other, Item.Importance.Filler));
                    hunterCyclesAdded++;
                }
                else if (RandoOptions.GiveObjectItems)
                {
                    // Add junk items
                    itemsToAdd.Add(Item.RandomJunkItem(ref randomState));
                }
                else
                {
                    // Duplicate a random gate item
                    IEnumerable<Item> gateItems = itemsToPlace.Where(i => i.type == Item.Type.Gate);
                    Item gate = new(gateItems.ElementAt(randomState.Next(gateItems.Count())))
                    {
                        importance = Item.Importance.Filler
                    };
                    itemsToAdd.Add(gate);
                    generationLog.AppendLine($"Added duplicate gate item: {gate}");
                }
            }

            if (itemsToAdd.Count > 0)
            {
                itemsToPlace.AddRange(itemsToAdd);
            }

            generationLog.AppendLine($"Item balancing ended with {state.AllLocations.Count} locations and {itemsToPlace.Count} items");
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
                foreach (Item i in itemsToPlace)
                {
                    if (i.importance == Item.Importance.Progression)
                    {
                        if (i.type == Item.Type.Gate)
                        {
                            string[] gate = Regex.Split(i.id, "_");
                            if (state.Gates.Contains(i.id)) continue;

                            // If there is a Connection associated with this gate ID
                            // and exactly one side is currently reachable, then consider this gate placeable.
                            if (state.AllConnections.Any(c => c.ID == i.id && c.ConnectedStatus == Connection.ConnectedLevel.OneReached))
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
                }
                // Determine which type of prog to place
                bool useOtherProgThisCycle;
                if (placeableOtherProg.Count == 0) useOtherProgThisCycle = false;
                else if (placeableGates.Count == 0) useOtherProgThisCycle = true;
                // If we have locations to spare, chance to place less important "misc" progression
                else useOtherProgThisCycle = state.AvailableLocations.Count > 5 && randomState.NextDouble() < OTHER_PROG_PLACEMENT_CHANCE;
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
                        generationLog.AppendLine($"\t{(Plugin.RegionNamesMap.TryGetValue(region.ID, out string name) ? name : region.ID)}");
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
            List<Item> placeableProg2 = [.. itemsToPlace.Where((i) =>
            {
                return i.importance == Item.Importance.Progression;
            })];
            do
            {
                // Detect possible failure
                if (state.AvailableLocations.Count == 0)
                {
                    generationLog.AppendLine($"ERROR: Ran out of possible locations");

                    generationLog.AppendLine("Failed to aquire access to:");
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
            }
            while (placeableProg2.Count > 0);

            if (state.UnreachedLocations.Count > 0)
            {
                generationLog.AppendLine($"ERROR: Progression step ended with impossible locations");
                generationLog.AppendLine("Failed to aquire access to:");
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
            foreach (var placement in RandomizedGame)
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
                    if (item.id.StartsWith("PearlObject-")) outputType = Unlock.UnlockType.ItemPearl;
                    else outputType = Unlock.UnlockType.Item;
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
                return new Unlock(Unlock.UnlockType.Item, Unlock.IDToItem(item.id[7..]));
            }
            if (outputType == Unlock.UnlockType.ItemPearl)
            {
                return new Unlock(Unlock.UnlockType.ItemPearl, Unlock.IDToItem(item.id[12..], true));
            }
            return new Unlock(outputType, item.id);
        }

        public static void GenerateCustomRules()
        {
            // Clear any rules that may have been populated in a previous OnModsInit()
            slugcatRuleOverrides.Clear();
            globalRuleOverrides.Clear();
            connectionRuleOverrides.Clear();
            manualConnections.Clear();
            manualSubregions.Clear();

            slugcatRuleOverrides.Add(SlugcatStats.Name.White, []);
            slugcatRuleOverrides.Add(SlugcatStats.Name.Yellow, []);
            slugcatRuleOverrides.Add(SlugcatStats.Name.Red, []);

            if (ModManager.MSC)
            {
                slugcatRuleOverrides.Add(MoreSlugcatsEnums.SlugcatStatsName.Gourmand, []);
                slugcatRuleOverrides.Add(MoreSlugcatsEnums.SlugcatStatsName.Artificer, []);
                slugcatRuleOverrides.Add(MoreSlugcatsEnums.SlugcatStatsName.Rivulet, []);
                slugcatRuleOverrides.Add(MoreSlugcatsEnums.SlugcatStatsName.Spear, []);
                slugcatRuleOverrides.Add(MoreSlugcatsEnums.SlugcatStatsName.Saint, []);
                slugcatRuleOverrides.Add(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, []);
            }

            // Cannot climb SB Ravine
            manualSubregions.Add(new("SB", "SBRavine",
                ["Echo-SB", "Pearl-SB_ravine", "Broadcast-Chatlog_SB0", "Shelter-SB_S09"],
                ["GATE_LF_SB"],
                ["SB_S09"],
                [new(AccessRule.IMPOSSIBLE_ID), new()]));

            // MSC specific rules
            if (ModManager.MSC)
            {
                // Subeterranean to Outer Expanse
                connectionRuleOverrides["GATE_SB_OE"] =
                [
                    // Gate AND (Survivor OR Monk OR (Gourmand AND The Mark))
                    new CompoundAccessRule(
                    [
                        new GateAccessRule("GATE_SB_OE"),
                        new CompoundAccessRule(
                        [
                            new MultiSlugcatAccessRule([SlugcatStats.Name.White, SlugcatStats.Name.Yellow]),
                            new CompoundAccessRule(
                            [
                                new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Gourmand),
                                new("The_Mark")
                            ], CompoundAccessRule.CompoundOperation.All)
                        ], CompoundAccessRule.CompoundOperation.Any)
                    ], CompoundAccessRule.CompoundOperation.All),
                    // Gate
                    new GateAccessRule("GATE_SB_OE")
                ];

                // Outer Expanse to Outskirts
                connectionRuleOverrides["GATE_OE_SU"] =
                [
                    // Free, gate always open
                    new(),
                    // Impossible
                    new(AccessRule.IMPOSSIBLE_ID)
                ];

                // Exterior to Metropolis 
                connectionRuleOverrides["GATE_UW_LC"] =
                [
                    // Gate AND (Metro option OR (Artificer AND The Mark AND Citizen ID Drone))
                    new CompoundAccessRule(
                    [
                        new GateAccessRule("GATE_UW_LC"),
                        new CompoundAccessRule(
                        [
                            new OptionAccessRule("ForceOpenMetropolis"),
                            new CompoundAccessRule(
                            [
                                new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Artificer),
                                new("The_Mark"),
                                new("IdDrone")
                            ], CompoundAccessRule.CompoundOperation.All)
                        ], CompoundAccessRule.CompoundOperation.Any)
                    ], CompoundAccessRule.CompoundOperation.All),
                    // Gate
                    new GateAccessRule("GATE_UW_LC")
                ];

                // Shoreline to Submerged Superstructure
                connectionRuleOverrides["GATE_MS_SL"] =
                [
                    // Gate
                    new GateAccessRule("GATE_MS_SL"),
                    // Gate AND (Submerged option OR Rivulet)
                    new CompoundAccessRule(
                    [
                        new GateAccessRule("GATE_MS_SL"),
                        new CompoundAccessRule(
                        [
                            new OptionAccessRule("ForceOpenSubmerged"),
                            new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
                        ], CompoundAccessRule.CompoundOperation.Any)
                    ], CompoundAccessRule.CompoundOperation.All)
                ];

                // Submerged Superstructure (Bitter Aerie) to Shoreline
                connectionRuleOverrides["GATE_SL_MS"] =
                [
                    // Impossible to enter from above LttM
                    new AccessRule(AccessRule.IMPOSSIBLE_ID),
                    // Free, if the gate is reachable. The Bitter Aerie subregion will handle that logic
                    new AccessRule()
                ];

                AccessRule sumpTunnelRule = new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Artificer, true);
                // Shoreline to Pipeyard (Sump Tunnel)
                connectionRuleOverrides["GATE_SL_VS"] =
                [
                    // Cannot traverse Sump Tunnel as Artificer
                    sumpTunnelRule, sumpTunnelRule
                ];

                // The Exterior is split in half at UW_C02, as Rivulet has a hard time crossing it
                manualSubregions.Add(new("UW", "UWWall",
                    ["Pearl-UW", "Echo-UW", "Token-S-UW", "Token-L-UW", "Token-YellowLizard", 
                        "Broadcast-Chatlog_Broadcast0", "Shelter-UW_S01", "Shelter-UW_S03", "Shelter-UW_S04",
                        "DevToken-UW_H01", "DevToken_UW_F01"],
                    ["GATE_SS_UW", "GATE_CC_UW", "GATE_UW_LC"],
                    ["UW_S01", "UW_S03", "UW_S04"],
                    [new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Rivulet, true), new()]));

                // Cannot reach filtration from Outskirts, except as Saint
                manualSubregions.Add(new SubregionBlueprint("SU", "SU_Filt",
                    ["Pearl-SU_filt", "Shelter-SU_S05", "DevToken-SU_CAVE01", "DevToken-SU_PMPSTATION01"],
                    ["GATE_OE_SU"],
                    ["SU_S05"],
                    [new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Saint), new()]));

                // Precipice is disconnected from Shoreline
                manualSubregions.Add(new("SL", "SLPrecipice",
                    ["Shelter-SL_S13", "DevToken-SL_BRIDGE01"],
                    ["GATE_UW_SL"],
                    ["SL_S13"],
                    [new(AccessRule.IMPOSSIBLE_ID), new(AccessRule.IMPOSSIBLE_ID)]));

                // Saint OR (Rivulet AND ((OptionUseEnergyCell AND EnergyCell) OR (Not OptionUseEnergyCell AND RegionRM)))
                // I hate this one
                AccessRule bitterAerieAccess = new CompoundAccessRule(
                [
                    new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Saint),
                    new CompoundAccessRule(
                    [
                        new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Rivulet),
                        new CompoundAccessRule(
                        [
                            new CompoundAccessRule(
                            [
                                new OptionAccessRule("UseEnergyCell"),
                                new AccessRule("Object-EnergyCell")
                            ], CompoundAccessRule.CompoundOperation.All),
                            new CompoundAccessRule(
                            [
                                new OptionAccessRule("UseEnergyCell", true),
                                new RegionAccessRule("RM")
                            ], CompoundAccessRule.CompoundOperation.All),
                        ], CompoundAccessRule.CompoundOperation.Any),
                    ], CompoundAccessRule.CompoundOperation.All)
                ], CompoundAccessRule.CompoundOperation.Any);
                // Bitter Aerie is only for Saint or after Rivulet completion
                manualSubregions.Add(new("MS", "MSBitterAerie",
                    ["Token-S-MS", "Token-MirosVulture", "Echo-MS", "Shelter-MS_S07", "Shelter-MS_S10",
                        "DevToken-MS_SEWERBRIDGE", "DevToken-MS_X02", "DevToken-MS_BITTEREDGE"],
                    ["GATE_SL_MS"],
                    ["MS_S07", "MS_S10"],
                    [bitterAerieAccess, new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Saint)]
                    ));

                // Only Saint can climb up to above LttM
                manualSubregions.Add(new("SL", "SLAboveLttM",
                    ["Echo-SL", "Shelter-SL_STOP", "DevToken-SL_ROOF04", "DevToken-SL_TEMPLE", "DevToken-SL_ROOF03"],
                    ["GATE_SL_MS"],
                    ["SL_STOP"],
                    [new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Saint), new()]
                    ));

                // Artificer cannot traverse Sump Tunnel
                manualSubregions.Add(new("VS", "VSSumpTunnel",
                    ["Shelter-VS_S02"],
                    ["GATE_SL_VS"],
                    ["VS_S02"],
                    [sumpTunnelRule, sumpTunnelRule]
                    ));

                // Token cache fails to filter this pearl to only Past GW
                globalRuleOverrides.Add("Pearl-MS", new CompoundAccessRule(
                [
                    new TimelineAccessRule(SlugcatStats.Timeline.Artificer, TimelineAccessRule.TimelineOperation.AtOrBefore),
                    new RegionAccessRule("GW")
                ], CompoundAccessRule.CompoundOperation.All));

                // Artificer and Inv can't reach underwater GW token
                globalRuleOverrides.Add("Token-BrotherLongLegs", new MultiSlugcatAccessRule(
                [
                    MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                    MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel
                ], true));

                // Waterfront Safari token is in a very silly location for Spearmaster
                globalRuleOverrides.Add("Token-S-LM", new SlugcatAccessRule(MoreSlugcatsEnums.SlugcatStatsName.Spear, true));

                // Inv can't reach underwater GW token
                slugcatRuleOverrides[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel].Add("Token-RedLizard", new(AccessRule.IMPOSSIBLE_ID));

                // Create a connection to Rubicon, which has no gate to it
                manualConnections.Add(new ConnectionBlueprint("FALL_SB_HR", ["SB", "HR"],
                    [new KarmaAccessRule(10), new(AccessRule.IMPOSSIBLE_ID)]));
            }
            // *NOT* MSC specific rules
            else
            {
                // The Exterior is split in half at UW_D06 pre-MSC, as there are no grapple worms for crossing
                // You *could* bring a grapple worm from Chimney but that's too out of the way to be in logic
                manualSubregions.Add(new("UW", "UWWall",
                    ["Pearl-UW", "Echo-UW", "Token-L-UW", "Token-YellowLizard", "Shelter-UW_S01", 
                        "Shelter-UW_S03", "Shelter-UW_S04"],
                    ["GATE_SS_UW", "GATE_CC_UW"],
                    ["UW_S01", "UW_S03", "UW_S04"],
                    [new(), new SlugcatAccessRule(SlugcatStats.Name.Red)]));
            }

            // Inbuilt custom region rules
            globalRuleOverrides.AddRange(CustomRegionCompatability.GlobalRuleOverrides);
            slugcatRuleOverrides.AddRange(CustomRegionCompatability.SlugcatRuleOverrides);
        }

        public Dictionary<string, AccessRule> CreatePassageRules()
        {
            Dictionary<string, AccessRule> passageRules = [];

            bool motherUnlocked = ModManager.MSC && (Plugin.Singleton.rainWorld.progression.miscProgressionData.beaten_Gourmand_Full || MoreSlugcats.MoreSlugcats.chtUnlockSlugpups.Value);
            bool canFindSlugpups = slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Red || (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand);

            foreach (string passage in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
            {
                // Skip over impossible passages
                switch (passage)
                {
                    // Gourmand is handled later
                    case "Gourmand":
                        continue;
                    case "Mother":
                        if (!motherUnlocked || !canFindSlugpups) continue;
                        break;
                    case "Chieftain":
                        if (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer) continue;
                        break;
                    case "Monk":
                    case "Saint":
                        // Much simpler to exclude from logic than to figure out what's reasonable
                        if (slugcat == SlugcatStats.Name.Red) continue;
                        if (ModManager.MSC
                            && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear
                            || slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer) continue;
                        break;
                    case "Hunter":
                    case "Outlaw":
                    case "DragonSlayer":
                        if (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint) continue;
                        break;
                    case "Scholar":
                        if (ModManager.MSC)
                        {
                            if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint
                                || slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel) continue;
                        }
                        else
                        {
                            if (slugcat == SlugcatStats.Name.Yellow) continue;
                        }
                        break;
                }

                AccessRule accessRule = new();
                switch (passage)
                {
                    case "Martyr":
                        accessRule = new CompoundAccessRule(AccessRuleConstants.Regions,
                            CompoundAccessRule.CompoundOperation.AtLeast, 5);
                        break;
                    case "Mother":
                        // TODO: Add better check for pup regions if we find another use for property file parsing to justify it
                        // Surely there's a pup spawnable region within a group of 5.
                        // The correct way to do this is by reading pup spawn chances from region properties files,
                        // but it does not feel worth parsing those every OnModsInit just for this one rule
                        accessRule = new CompoundAccessRule(AccessRuleConstants.Regions,
                            CompoundAccessRule.CompoundOperation.AtLeast, 5);
                        break;
                    case "Pilgrim":
                        accessRule = new CompoundAccessRule(
                            [.. SlugcatStats.SlugcatStoryRegions(slugcat)
                                .Where(r => World.CheckForRegionGhost(slugcat, r))
                                .Select(r => new RegionAccessRule(r))],
                            CompoundAccessRule.CompoundOperation.All);
                        break;
                    case "Survivor":
                        accessRule = new KarmaAccessRule(5);
                        break;
                    case "DragonSlayer":
                        accessRule = new CompoundAccessRule(AccessRuleConstants.Lizards,
                            CompoundAccessRule.CompoundOperation.AtLeast, 6);
                        break;
                    case "Friend":
                        accessRule = new CompoundAccessRule(AccessRuleConstants.Lizards,
                            CompoundAccessRule.CompoundOperation.Any);
                        break;
                    case "Traveller":
                        accessRule = new CompoundAccessRule(
                            [.. SlugcatStats.SlugcatStoryRegions(slugcat)
                                .Select(r => new RegionAccessRule(r))],
                            CompoundAccessRule.CompoundOperation.All);
                        break;
                    case "Chieftain":
                        accessRule = new CreatureAccessRule(CreatureTemplate.Type.Scavenger);
                        break;
                    case "Hunter":
                        // Hunter passage for carnivores is easy pretty much anywhere,
                        // check for a single food object to ensure we aren't in SS or some similar region
                        int foodCount = AccessRuleConstants.strictCarnivores.Contains(slugcat) ? 1 : 3;
                        accessRule = new CompoundAccessRule(AccessRuleConstants.HunterFoods,
                            CompoundAccessRule.CompoundOperation.AtLeast, foodCount);
                        break;
                    case "Monk":
                        accessRule = new CompoundAccessRule(AccessRuleConstants.MonkFoods,
                            CompoundAccessRule.CompoundOperation.AtLeast, 3);
                        break;
                    case "Nomad":
                        accessRule = new CompoundAccessRule(AccessRuleConstants.Regions,
                            CompoundAccessRule.CompoundOperation.AtLeast, 4);
                        break;
                    case "Outlaw":
                        // Outlaw creatures aren't filtered exceptionally well,
                        // so the requirements are higher to compensate
                        accessRule = new CompoundAccessRule(AccessRuleConstants.OutlawCrits,
                            CompoundAccessRule.CompoundOperation.AtLeast, 8);
                        break;
                    case "Saint":
                        // Use same rule as Monk because they're fairly similar.
                        // Realistically the passage is easier than this
                        accessRule = new CompoundAccessRule(AccessRuleConstants.MonkFoods,
                            CompoundAccessRule.CompoundOperation.AtLeast, 3);
                        break;
                    case "Scholar":
                        List<AccessRule> rules =
                        [
                            new AccessRule("The_Mark"),
                            new CompoundAccessRule(AccessRuleConstants.Regions,
                                CompoundAccessRule.CompoundOperation.AtLeast, 4)
                        ];
                        if (slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Yellow
                            || (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand))
                        {
                            rules.Add(new RegionAccessRule("SL"));
                        }
                        accessRule = new CompoundAccessRule([.. rules],
                            CompoundAccessRule.CompoundOperation.All);
                        break;
                }

                passageRules[passage] = accessRule;
            }

            return passageRules;
        }

        public class GenerationFailureException : Exception
        {
            public GenerationFailureException() : base() { }

            public GenerationFailureException(string error) : base(error) { }
        }
    }
}
