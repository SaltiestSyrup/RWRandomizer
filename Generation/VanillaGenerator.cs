using Mono.Cecil;
using MoreSlugcats;
using RegionKit.Modules.CustomProjections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RainWorldRandomizer.Generation
{
    public class VanillaGenerator
    {
        private SlugcatStats.Name slugcat;
        private SlugcatStats.Timeline timeline;
        private long generationSeed;

        public enum GenerationStep
        {
            NotStarted,
            InitializingState,
            PlacingProgGates,
            PlacingOtherProg,
            PlacingFillerGates,
            PlacingFiller,
            Complete
        }
        public GenerationStep CurrentStage { get; private set; }

        private State state;
        private List<Item> ItemsToPlace;
        private HashSet<string> AllRegions;
        public HashSet<string> AllGates { get; private set; }
        public HashSet<string> AllPassages { get; private set; }


        public VanillaGenerator(long generationSeed, SlugcatStats.Name slugcat, SlugcatStats.Timeline timeline)
        {
            this.generationSeed = generationSeed;
            this.slugcat = slugcat;
            this.timeline = timeline;
            CurrentStage = GenerationStep.NotStarted;

            AllGates = new HashSet<string>();
            AllPassages = new HashSet<string>();
        }

        public void InitializeState()
        {
            CurrentStage = GenerationStep.InitializingState;
            HashSet<Location> locations = new HashSet<Location>();

            // Load Tokens
            if (Options.UseSandboxTokenChecks)
            {
                try
                {
                    Plugin.Singleton.collectTokenHandler.LoadAvailableTokens(Plugin.Singleton.rainWorld, slugcat);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Failed loading sandbox tokens");
                    Plugin.Log.LogError(e);
                }
            }

            // Regions loop
            bool regionKitEchoes = Options.UseEchoChecks && RegionKitCompatibility.Enabled;
            bool doPearlLocations = Options.UsePearlChecks && (ModManager.MSC || slugcat != SlugcatStats.Name.Yellow);
            bool spearBroadcasts = ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear && Options.UseSMBroadcasts;
            foreach (string region in Region.GetFullRegionOrder(timeline))
            {
                if (ModManager.MSC)
                {
                    // Limit OE access as timeline filtering isn't enough
                    if (region.Equals("OE")
                        && !(slugcat == SlugcatStats.Name.White 
                        || slugcat == SlugcatStats.Name.Yellow
                        || slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand))
                    {
                        continue;
                    }

                    // Limit LC access unless option chosen to allow it
                    if (region.Equals("LC") 
                        && !(Options.ForceOpenMetropolis
                        || slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer))
                    {
                        continue;
                    }
                }
                AllRegions.Add(region);
                RegionAccessRule regionAccessRule = new RegionAccessRule(region);
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
                }

                if (skipThisGate) continue;

                AllGates.Add(gate);
                ItemsToPlace.Add(new Item(gate, Item.Type.Progression));
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
                    ItemsToPlace.Add(new Item(passage, Item.Type.Filler));
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
                ItemsToPlace.Add(new Item("Karma", Item.Type.Progression));
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
                            new RegionAccessRule("SL")));
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
                            new RegionAccessRule("SL")));
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
                ItemsToPlace.Add(new Item("Neuron_Glow", Item.Type.Progression));
                ItemsToPlace.Add(new Item("The_Mark", Item.Type.Progression));
            }

            switch (slugcat.value)
            {
                case "Red":
                    ItemsToPlace.Add(new Item("NSHSwarmer", Item.Type.Progression));
                    ItemsToPlace.Add(new Item("Red_stomach", Item.Type.Progression));
                    break;
                case "Artificer":
                    ItemsToPlace.Add(new Item("IdDrone", Item.Type.Progression));
                    break;
                case "Rivulet":
                    if (Options.UseEnergyCell)
                    {
                        ItemsToPlace.Add(new Item("EnergyCell", Item.Type.Progression));
                        ItemsToPlace.Add(new Item("FP_Disconnected", Item.Type.Progression));
                    }
                    break;
                case "Spear":
                    ItemsToPlace.Add(new Item("Spearmasterpearl", Item.Type.Progression));
                    ItemsToPlace.Add(new Item("Rewrite_Spear_Pearl", Item.Type.Progression));
                    break;
            }

            CollectTokenHandler.ClearRoomAccessibilities();

            state = new State(locations, Options.StartMinimumKarma ? 1 : 5);
        }
    }
}
