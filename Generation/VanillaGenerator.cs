using Mono.Cecil;
using MoreSlugcats;
using RegionKit.Modules.CustomProjections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Random = UnityEngine.Random;

namespace RainWorldRandomizer.Generation
{
    public class VanillaGenerator
    {
        private SlugcatStats.Name slugcat;
        private SlugcatStats.Timeline timeline;
        private int generationSeed;

        public enum GenerationStep
        {
            NotStarted,
            InitializingState,
            BalancingItems,
            PlacingProgGates,
            PlacingOtherProg,
            PlacingFillerGates,
            PlacingFiller,
            Complete,
            FailedGen
        }
        public GenerationStep CurrentStage { get; private set; }
        public bool InProgress
        { 
            get 
            {
                return CurrentStage > GenerationStep.NotStarted
                    && CurrentStage < GenerationStep.Complete;
            } 
        }

        private Task generationThread;

        private State state;
        private List<Item> ItemsToPlace = new List<Item>();
        private HashSet<string> AllRegions = new HashSet<string>();
        public HashSet<string> AllGates { get; private set; }
        public HashSet<string> UnplacedGates { get; private set; }
        public HashSet<string> AllPassages { get; private set; }
        public Dictionary<Location, Item> RandomizedGame { get; private set; }

        public StringBuilder generationLog;
        public string customStartDen = "NONE";


        public VanillaGenerator(SlugcatStats.Name slugcat, SlugcatStats.Timeline timeline, int generationSeed = 0)
        {
            this.slugcat = slugcat;
            this.timeline = timeline;
            CurrentStage = GenerationStep.NotStarted;

            AllGates = new HashSet<string>();
            UnplacedGates = new HashSet<string>();
            AllPassages = new HashSet<string>();
            RandomizedGame = new Dictionary<Location, Item>();

            // Initialize RNG
            this.generationSeed = generationSeed == 0 ? Random.Range(0, int.MaxValue) : generationSeed;
            Random.InitState(generationSeed);
        }

        public void BeginGeneration()
        {
            generationThread = new Task(Generate);
            generationThread.Start();
        }

        private void Generate()
        {
            InitializeState();
            CustomLocationRules();
            BalanceItems();
            PlaceProgGates();
        }

        private void InitializeState()
        {
            CurrentStage = GenerationStep.InitializingState;
            HashSet<Location> locations = new HashSet<Location>();

            // Load Tokens
            if (Options.UseSandboxTokenChecks)
            {
                Plugin.Singleton.collectTokenHandler.LoadAvailableTokens(Plugin.Singleton.rainWorld, slugcat);
            }

            // Regions loop
            bool regionKitEchoes = Options.UseEchoChecks && RegionKitCompatibility.Enabled;
            bool doPearlLocations = Options.UsePearlChecks && (ModManager.MSC || slugcat != SlugcatStats.Name.Yellow);
            bool spearBroadcasts = ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear && Options.UseSMBroadcasts;
            foreach (string region in Region.GetFullRegionOrder(timeline))
            {
                AccessRule regionAccessRule = new RegionAccessRule(region);
                if (ModManager.MSC)
                {
                    // Limit OE access as timeline filtering isn't enough
                    if (region.Equals("OE"))
                    {
                        switch (slugcat.value)
                        {
                            case "Gourmand":
                                regionAccessRule = new CompoundAccessRule(new AccessRule[]
                                {
                                    regionAccessRule,
                                    new AccessRule("The_Mark")
                                }, CompoundAccessRule.CompoundOperation.All);
                                break;
                            case "White":
                            case "Yellow":
                                break;
                            default:
                                continue;
                        }
                    }

                    // Limit LC access unless option chosen to allow it
                    if (region.Equals("LC"))
                    {
                        if (!(Options.ForceOpenMetropolis
                            || slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer))
                        {
                            continue;
                        }
                        
                        if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                        {
                            regionAccessRule = new CompoundAccessRule(new AccessRule[]
                            {
                                regionAccessRule,
                                new AccessRule("The_Mark"),
                                new AccessRule("IdDrone")
                            }, CompoundAccessRule.CompoundOperation.All);
                        }
                    }
                }
                AllRegions.Add(region);
                string regionLower = region.ToLowerInvariant();

                // Add Echoes from RegionKit if present
                if (regionKitEchoes && RegionKitCompatibility.RegionHasEcho(region, slugcat))
                {
                    locations.Add(new Location($"Echo-{region}", Location.Type.Echo, regionAccessRule));
                }

                // Create Pearl locations
                if (doPearlLocations && Plugin.Singleton.rainWorld.regionDataPearls.ContainsKey(region.ToLowerInvariant()))
                {
                    for (int i = 0; i < Plugin.Singleton.rainWorld.regionDataPearls[regionLower].Count; i++)
                    {
                        if (Plugin.Singleton.rainWorld.regionDataPearlsAccessibility[regionLower][i].Contains(slugcat))
                        {
                            locations.Add(new Location($"Pearl-{Plugin.Singleton.rainWorld.regionDataPearls[regionLower][i].value}",
                                Location.Type.Pearl, regionAccessRule));
                        }
                    }
                }

                // Create Token locations
                if (Options.UseSandboxTokenChecks
                    && Plugin.Singleton.collectTokenHandler.availableTokens.ContainsKey(region))
                {
                    foreach(string token in Plugin.Singleton.collectTokenHandler.availableTokens[region])
                    {
                        locations.Add(new Location($"Token-{token}", Location.Type.Token, regionAccessRule));
                    }
                }

                // Create Broadcast locations
                if (spearBroadcasts && Plugin.Singleton.rainWorld.regionGreyTokens.ContainsKey(regionLower))
                {
                    foreach (ChatlogData.ChatlogID token in Plugin.Singleton.rainWorld.regionGreyTokens[regionLower])
                    {
                        locations.Add(new Location($"Broadcast-{token.value}", Location.Type.Token, regionAccessRule));
                    }
                }
            }

            // Create Gate items
            foreach (string karmaLock in Plugin.Singleton.rainWorld.progression.karmaLocks)
            {
                string gate = Regex.Split(karmaLock, " : ")[0];
                string[] split = Regex.Split(gate, "_");
                string[] regions = new string[2] { split[1], split[2] };

                bool skipThisGate = false;
                foreach (string region in regions)
                {
                    // If this region does not exist in the timeline
                    // and is not an alias of an existing region, skip the gate
                    if (!AllRegions.Contains(region) 
                        && (!Plugin.ProperRegionMap.TryGetValue(region, out string alias)
                        || region == alias))
                    {
                        skipThisGate = true;
                    }

                    // If this gate is impossible to reach for the current slugcat, skip it
                    if (CollectTokenHandler
                        .GetRoomAccessibility(region)
                        .TryGetValue(gate.ToLowerInvariant(), out List<SlugcatStats.Name> accessibleTo)
                        && accessibleTo.Contains(slugcat))
                    {
                        skipThisGate = true;
                    }

                    // Gates that have to always be open to avoid softlocks
                    if (Constants.ForceOpenGates.Contains(gate)) skipThisGate = true;
                }

                if (skipThisGate) continue;

                AllGates.Add(gate);
                ItemsToPlace.Add(new Item(gate, Item.Type.Gate, Item.Importance.Progression));
            }

            // Create Passage locations and items
            foreach (string passage in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
            {
                bool motherUnlocked = ModManager.MSC && (Plugin.Singleton.rainWorld.progression.miscProgressionData.beaten_Gourmand_Full || MoreSlugcats.MoreSlugcats.chtUnlockSlugpups.Value);
                bool canFindSlugpups = slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Red || (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand);

                // Filter out impossible passages
                if (ModManager.MSC)
                {
                    switch (passage)
                    {
                        case "Gourmand":
                            if (Options.UseFoodQuest) continue;
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
                            if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint
                                || slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel) continue;
                            break;
                    }
                }

                // Passage locations
                if (Options.UsePassageChecks)
                {
                    AccessRule accessRule = new AccessRule();
                    switch (passage)
                    {
                        case "Martyr":
                            break;
                        // TODO: Find a way to parse pup spawn chance in regions without loading them
                        case "Mother":
                            accessRule = new CompoundAccessRule(new AccessRule[]
                            {
                                new RegionAccessRule("HI"),
                                new RegionAccessRule("DS"),
                                new RegionAccessRule("GW"),
                                new RegionAccessRule("SH"),
                                new RegionAccessRule("CC"),
                                new RegionAccessRule("SI"),
                                new RegionAccessRule("LF"),
                                new RegionAccessRule("SB"),
                                new RegionAccessRule("VS"),
                            }, CompoundAccessRule.CompoundOperation.Any);
                            break;
                        // TODO: Populate Pilgrim passage requirements
                        case "Pilgrim":
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
                            List<AccessRule> regions = new List<AccessRule>();
                            foreach (string reg in SlugcatStats.SlugcatStoryRegions(slugcat))
                            {
                                regions.Add(new RegionAccessRule(reg));
                            }
                            accessRule = new CompoundAccessRule(regions.ToArray(), 
                                CompoundAccessRule.CompoundOperation.All);
                            break;
                        case "Chieftain":
                            accessRule = new CreatureAccessRule(CreatureTemplate.Type.Scavenger);
                            break;
                        // TODO: Populate Hunter and Monk passage requirements
                        case "Hunter":
                        case "Monk":
                            break;
                        case "Nomad":
                            accessRule = new CompoundAccessRule(AccessRuleConstants.Regions, 
                                CompoundAccessRule.CompoundOperation.AtLeast, 4);
                            break;
                        // TODO: Figure out Outlaw and Saint passage requirements
                        case "Outlaw":
                        case "Saint":
                            break;
                        case "Scholar":
                            List<AccessRule> rules = new List<AccessRule>()
                            {
                                new AccessRule("The Mark"),
                                new CompoundAccessRule(AccessRuleConstants.Regions, 
                                    CompoundAccessRule.CompoundOperation.AtLeast, 3)
                            };
                            if (slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Yellow
                                || (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand))
                            {
                                rules.Add(new RegionAccessRule("SL"));
                            }
                            accessRule = new CompoundAccessRule(rules.ToArray(), 
                                CompoundAccessRule.CompoundOperation.All);
                            break;
                        // TODO: Populate Food Quest passage requirements
                        case "Gourmand":
                            break;
                    }

                    locations.Add(new Location($"Passage-{passage}", Location.Type.Passage, accessRule));
                }

                // Passage items
                if (Options.GivePassageItems && passage != "Gourmand")
                {
                    AllPassages.Add(passage);
                    ItemsToPlace.Add(new Item(passage, Item.Type.Passage, Item.Importance.Filler));
                }
            }

            // Create Echo locations
            if (Options.UseEchoChecks)
            {
                foreach (string echo in ExtEnumBase.GetNames(typeof(GhostWorldPresence.GhostID)))
                {
                    // No worry for duplicates from the RegionKit check,
                    // as the HashSet should ignore duplicate additions
                    if (!echo.Equals("NoGhost")
                        && World.CheckForRegionGhost(slugcat, echo)
                        && AllRegions.Contains(echo))
                    {
                        locations.Add(new Location($"Echo-{echo}", Location.Type.Echo, new RegionAccessRule(echo)));
                    }
                }
            }

            // Create Karma items
            // TODO: Add a setting to change the amount of Karma increases in pool
            for (int i = 0; i < 10; i++)
            {
                ItemsToPlace.Add(new Item("Karma", Item.Type.Karma, Item.Importance.Progression));
            }

            // TODO: Add support for expanded food quest
            // Create Food Quest locations
            if (ModManager.MSC && Options.UseFoodQuest)
            {
                foreach (WinState.GourmandTrackerData data in WinState.GourmandPassageTracker)
                {
                    if (data.type == AbstractPhysicalObject.AbstractObjectType.Creature)
                    {
                        List<CreatureAccessRule> rules = new List<CreatureAccessRule>();
                        foreach (CreatureTemplate.Type type in data.crits)
                        {
                            rules.Add(new CreatureAccessRule(type));
                        }

                        locations.Add(new Location($"FoodQuest-{data.crits[0].value}", Location.Type.Food,
                            new CompoundAccessRule(rules.ToArray(), CompoundAccessRule.CompoundOperation.Any)));
                    }
                    else
                    {
                        locations.Add(new Location($"FoodQuest-{data.type.value}", Location.Type.Food,
                            new ObjectAccessRule(data.type)));
                    }
                }
            }

            // Create Special locations
            if (Options.UseSpecialChecks)
            {
                locations.Add(new Location("Eat_Neuron", Location.Type.Story, 
                    new CompoundAccessRule(new AccessRule[]
                    {
                        new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer),
                        new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer),
                    }, CompoundAccessRule.CompoundOperation.Any)));

                switch (slugcat.value)
                {
                    // Normal Iterator goals
                    case "White":
                    case "Yellow":
                    case "Gourmand":
                    case "Sofanthiel":
                        locations.Add(new Location("Meet_LttM", Location.Type.Story,
                            new CompoundAccessRule(new AccessRule[]
                            {
                                new RegionAccessRule("SL"),
                                new AccessRule("The_Mark")
                            }, CompoundAccessRule.CompoundOperation.All)));
                        locations.Add(new Location("Meet_FP", Location.Type.Story,
                            new RegionAccessRule("SS")));
                        break;
                    // Spear finds LttM in LM
                    case "Spear":
                        locations.Add(new Location("Meet_LttM_Spear", Location.Type.Story,
                            new RegionAccessRule("LM")));
                        locations.Add(new Location("Meet_FP", Location.Type.Story,
                            new RegionAccessRule("SS")));
                        break;
                    // Hunter Saves LttM, which is a seperate check
                    case "Red":
                        locations.Add(new Location("Save_LttM", Location.Type.Story,
                            new CompoundAccessRule(new AccessRule[]
                            {
                                new RegionAccessRule("SL"),
                                new AccessRule("NSHSwarmer")
                            }, CompoundAccessRule.CompoundOperation.All)));
                        locations.Add(new Location("Meet_FP", Location.Type.Story,
                            new RegionAccessRule("SS")));
                        break;
                    // Artificer cannot meet LttM
                    case "Artificer":
                        locations.Add(new Location("Meet_FP", Location.Type.Story,
                            new RegionAccessRule("SS")));
                        break;
                    // Rivulet does a murder in RM, seperate check
                    case "Rivulet":
                        locations.Add(new Location("Meet_LttM", Location.Type.Story,
                            new CompoundAccessRule(new AccessRule[]
                            {
                                new RegionAccessRule("SL"),
                                new AccessRule("The_Mark")
                            }, CompoundAccessRule.CompoundOperation.All)));
                        if (Options.UseEnergyCell)
                        {
                            locations.Add(new Location("Kill_FP", Location.Type.Story,
                                new RegionAccessRule("RM")));
                        }
                        break;
                    // Saint has 2 seperate checks here for ascending
                    case "Saint":
                        locations.Add(new Location("Ascend_LttM", Location.Type.Story,
                            new RegionAccessRule("SL")));
                        locations.Add(new Location("Ascend_FP", Location.Type.Story,
                            new RegionAccessRule("CL")));
                        break;
                }
            }

            // Create Special items
            if (!ModManager.MSC || slugcat != MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                ItemsToPlace.Add(new Item("Neuron_Glow", Item.Type.Story, Item.Importance.Progression));
                ItemsToPlace.Add(new Item("The_Mark", Item.Type.Story, Item.Importance.Progression));
            }

            switch (slugcat.value)
            {
                case "Red":
                    ItemsToPlace.Add(new Item("NSHSwarmer", Item.Type.Story, Item.Importance.Progression));
                    ItemsToPlace.Add(new Item("Red_stomach", Item.Type.Story, Item.Importance.Progression));
                    break;
                case "Artificer":
                    ItemsToPlace.Add(new Item("IdDrone", Item.Type.Story, Item.Importance.Progression));
                    break;
                case "Rivulet":
                    if (Options.UseEnergyCell)
                    {
                        ItemsToPlace.Add(new Item("EnergyCell", Item.Type.Story, Item.Importance.Progression));
                        ItemsToPlace.Add(new Item("FP_Disconnected", Item.Type.Story, Item.Importance.Progression));
                    }
                    break;
                case "Spear":
                    ItemsToPlace.Add(new Item("Spearmasterpearl", Item.Type.Story, Item.Importance.Progression));
                    ItemsToPlace.Add(new Item("Rewrite_Spear_Pearl", Item.Type.Story, Item.Importance.Progression));
                    break;
            }

            CollectTokenHandler.ClearRoomAccessibilities();

            state = new State(locations, Options.StartMinimumKarma ? 1 : 5);
        }

        /// <summary>
        /// Hook this function to modify rules for specific locations
        /// </summary>
        private void CustomLocationRules()
        {
            if (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                Location loc = state.AllLocations.First(l => l.id == "Token-BrotherLongLegs");
                loc.accessRule = new AccessRule("IMPOSSIBLE");
            }
        }

        private void BalanceItems()
        {
            CurrentStage = GenerationStep.BalancingItems;
            // Manage case where there are not enough locations for the amount of items in pool
            while (state.AllLocations.Count < ItemsToPlace.Count)
            {
                // Remove a passage token
                IEnumerable<Item> passageTokens = ItemsToPlace.Where(i => i.type == Item.Type.Passage);
                if (passageTokens.Count() > ManagerVanilla.MIN_PASSAGE_TOKENS)
                {
                    ItemsToPlace.Remove(passageTokens.First());
                    continue;
                }

                // Cannot remove more passages, unlock gates
                IEnumerable<Item> gateItems = ItemsToPlace.Where(i => i.type == Item.Type.Gate);
                if (gateItems.Count() > 0)
                {
                    Item item = gateItems.ElementAt(Random.Range(0, gateItems.Count()));
                    ItemsToPlace.Remove(item);
                    UnplacedGates.Add(item.ToString());
                    state.AddGate(item.ToString());
                    continue;
                }

                generationLog.AppendLine("Too few locations present to make a valid seed");
                generationLog.AppendLine("Generation Failed");
                CurrentStage = GenerationStep.FailedGen;
                throw new GenerationFailureException();
            }

            List<Item> itemsToAdd = new List<Item>();
            int hunterCyclesAdded = 0;
            while(state.AllLocations.Count > ItemsToPlace.Count + itemsToAdd.Count)
            {
                if (slugcat == SlugcatStats.Name.Red
                    && hunterCyclesAdded < state.AllLocations.Count * Options.HunterCycleIncreaseDensity)
                {
                    // Add cycle increases for Hunter
                    itemsToAdd.Add(new Item("HunterCycles", Item.Type.Other, Item.Importance.Filler));
                    hunterCyclesAdded++;
                }
                else if (Options.GiveObjectItems)
                {
                    // Add junk items
                    itemsToAdd.Add(Item.RandomJunkItem()); 
                }
                else
                {
                    // Duplicate a random gate item
                    IEnumerable<Item> gateItems = ItemsToPlace.Where(i => i.type == Item.Type.Gate);
                    itemsToAdd.Add(gateItems.ElementAt(Random.Range(0, gateItems.Count())));
                }
            }

            if (itemsToAdd.Count > 0)
            {
                ItemsToPlace.AddRange(itemsToAdd);
            }
        }
        
        private void PlaceProgGates()
        {
            CurrentStage = GenerationStep.PlacingProgGates;
            // Determine starting region
            if (Options.RandomizeSpawnLocation)
            {
                customStartDen = Expedition.ExpeditionGame.ExpeditionRandomStarts(
                    Plugin.Singleton.rainWorld, slugcat);
                generationLog.AppendLine($"Using custom start den: {customStartDen}");
                state.AddRegion(Plugin.ProperRegionMap[Regex.Split(customStartDen, "_")[0]]);
            }
            else
            {
                state.AddRegion(Constants.SlugcatStartingRegion[slugcat]);
            }

            // Continue until all regions are accessible
            // Note that a region is considered "accessible" by state regardless of
            // if there is some other rule blocking access to checks in that region
            while (state.Regions.Count != AllRegions.Count)
            {
                // All gates adjacent to exactly one of the currently accessible regions
                List<Item> adjacentGates = ItemsToPlace.Where(i =>
                {
                    if (i.type == Item.Type.Gate && i.importance == Item.Importance.Progression)
                    {
                        string[] gate = Regex.Split(i.id, "_");
                        if (state.Gates.Contains(i.id)) return false;

                        if (state.Regions.Contains(gate[1]) ^ state.Regions.Contains(gate[2]))
                        {
                            return true;
                        }
                    }
                    return false;
                }).ToList();

                // Check if we have failed
                if (state.AvailableLocations.Count == 0 || adjacentGates.Count == 0)
                {
                    generationLog.AppendLine($"ERROR: Ran out of " +
                        $"{(adjacentGates.Count == 0 ? "adjacent gates" : "possible locations")}.");

                    generationLog.AppendLine("Failed to connect to:");
                    foreach (string region in AllRegions.Except(state.Regions))
                    {
                        generationLog.AppendLine($"\t{Plugin.RegionNamesMap[region]}");
                    }
                    // TODO: Print full final state on error
                    CurrentStage = GenerationStep.FailedGen;
                    throw new GenerationFailureException();
                }

                Location chosenLocation = state.PopRandomLocation();
                Item chosenGate = adjacentGates[Random.Range(0, adjacentGates.Count)];
                RandomizedGame.Add(chosenLocation, chosenGate);
                state.AddGate(chosenGate.id);
                ItemsToPlace.Remove(chosenGate);
            }
        }

        public class GenerationFailureException : Exception
        {
            public GenerationFailureException() : base() { }

            public GenerationFailureException(string error) : base(error) { }
        }
    }
}
