using System.Collections.Generic;
using System.Linq;
using MoreSlugcats;
using Watcher;

namespace RainWorldRandomizer.Generation;

public static class LocationHelpers
{
    public static Location MakeEchoOrSpinningTopLocation(SlugcatStats.Name slugcat, string regionShort)
    {
        if (slugcat.IsWatcher())
        {
            return RWCustom.Custom.rainWorld.regionSpinningTopRooms.ContainsKey(regionShort)
                ? new Location($"SpinningTop-{regionShort}", Location.Type.Echo, AccessRule.Empty())
                : null;
        }

        if ((RegionKitCompatibility.Enabled && RegionKitCompatibility.RegionHasEcho(regionShort, slugcat))
            || World.CheckForRegionGhost(slugcat, regionShort))
        {
            return new Location($"Echo-{regionShort}", Location.Type.Echo, AccessRule.Empty());
        }

        return null;
    }

    /// <summary>
    /// Create locations for all the pearls a slugcat can find in a region
    /// </summary>
    /// <param name="slugcat"></param>
    /// <param name="regionShort">The acronym of the region to search (UPPERCASE)</param>
    /// <returns></returns>
    public static HashSet<Location> MakePearlLocations(SlugcatStats.Name slugcat, string regionShort)
    {
        string regionLower = regionShort.ToLowerInvariant();
        if (!Plugin.Singleton.rainWorld.regionDataPearls.ContainsKey(regionLower)) return [];

        HashSet<Location> locs = [];

        for (int i = 0; i < Plugin.Singleton.rainWorld.regionDataPearls[regionLower].Count; i++)
        {
            if (Plugin.Singleton.rainWorld.regionDataPearlsAccessibility[regionLower][i].Contains(slugcat)
                && Plugin.Singleton.rainWorld.regionDataPearls[regionLower][i].value != "")
            {
                locs.Add(new Location(
                    $"Pearl-{Plugin.Singleton.rainWorld.regionDataPearls[regionLower][i].value}-{regionShort}",
                    Location.Type.Pearl, AccessRule.Empty()));
            }
        }

        return locs;
    }

    /// <summary>
    /// Create locations for all the tokens a slugcat can find in a region (does not include broadcasts or dev tokens)
    /// </summary>
    /// <param name="slugcat"></param>
    /// <param name="regionShort">The acronym of the region to search (UPPERCASE)</param>
    /// <returns></returns>
    public static HashSet<Location> MakeTokenLocations(SlugcatStats.Name slugcat, string regionShort)
    {
        if (!CollectTokenHandler.AvailableTokens[slugcat].TryGetValue(regionShort, out string[] tokens)) return [];

        return tokens.Select(token =>
        {
            string name = $"Token-{token}";
            if (token.Split('-').Length == 1) name += $"-{regionShort}";
            return new Location(name, Location.Type.Token, AccessRule.Empty());
        }).ToHashSet();
    }

    /// <summary>
    /// Create locations for all the broadcast tokens that can be found in a region
    /// </summary>
    /// <param name="slugcat"></param>
    /// <param name="regionShort">The acronym of the region to search (UPPERCASE)</param>
    /// <returns></returns>
    public static HashSet<Location> MakeBroadcastLocations(SlugcatStats.Name slugcat, string regionShort)
    {
        string regionLower = regionShort.ToLowerInvariant();
        if (!Plugin.Singleton.rainWorld.regionGreyTokens.TryGetValue(regionLower,
                out List<ChatlogData.ChatlogID> chatLogs))
        {
            return [];
        }

        return chatLogs
            .Select(token => new Location(
                $"Broadcast-{token.value}-{regionShort}",
                Location.Type.Token,
                AccessRule.Empty()))
            .ToHashSet();
    }

    /// <summary>
    /// Create locations for all the dev tokens a slugcat can find in a region
    /// </summary>
    /// <param name="slugcat"></param>
    /// <param name="regionShort">The acronym of the region to search (UPPERCASE)</param>
    /// <returns></returns>
    public static HashSet<Location> MakeDevTokenLocations(SlugcatStats.Name slugcat, string regionShort)
    {
        string regionLower = regionShort.ToLowerInvariant();
        if (!TokenCachePatcher.regionDevTokens.TryGetValue(regionLower, out List<string> tokens)) return [];

        HashSet<Location> locs = [];

        for (int i = 0; i < tokens.Count; i++)
        {
            if (TokenCachePatcher.regionDevTokensAccessibility[regionLower][i].Contains(slugcat))
            {
                locs.Add(new Location($"DevToken-{tokens[i]}", Location.Type.Token, AccessRule.Empty()));
            }
        }

        return locs;
    }

    /// <summary>
    /// Create locations for all the karma flowers a slugcat can find in a region
    /// </summary>
    /// <param name="slugcat"></param>
    /// <param name="regionShort">The acronym of the region to search (UPPERCASE)</param>
    /// <returns></returns>
    public static HashSet<Location> MakeKarmaFlowerLocations(SlugcatStats.Name slugcat, string regionShort)
    {
        string regionLower = regionShort.ToLowerInvariant();
        if (!TokenCachePatcher.regionKarmaFlowers.TryGetValue(regionLower, out List<string> flowers)) return [];

        HashSet<Location> locs = [];

        for (int i = 0; i < flowers.Count; i++)
        {
            if (TokenCachePatcher.regionKarmaFlowersAccessibility[regionLower][i].Contains(slugcat))
            {
                locs.Add(new Location($"Flower-{flowers[i]}", Location.Type.Flower, AccessRule.Empty()));
            }
        }

        return locs;
    }

    /// <summary>
    /// Get a set of all the shelters in a region, optionally also making them into locations
    /// </summary>
    /// <param name="timeline"></param>
    /// <param name="regionShort">The acronym of the region to search (UPPERCASE)</param>
    /// <param name="makeLocations">Whether the locations part of the return tuple should be populated</param>
    /// <returns></returns>
    public static (HashSet<string>, HashSet<Location>) MakeShelters(SlugcatStats.Timeline timeline, string regionShort,
        bool makeLocations)
    {
        string regionLower = regionShort.ToLowerInvariant();
        if (!TokenCachePatcher.regionShelters.TryGetValue(regionLower, out List<string> shelters)) return ([], []);

        HashSet<string> shelt = [];
        HashSet<Location> locs = [];

        for (int i = 0; i < shelters.Count; i++)
        {
            if (TokenCachePatcher.regionSheltersAccessibility[regionLower][i].Contains(timeline))
            {
                shelt.Add(shelters[i]);
                // Create Shelter locations
                if (makeLocations)
                {
                    locs.Add(new Location($"Shelter-{shelters[i]}", Location.Type.Shelter, AccessRule.Empty()));
                }
            }
        }

        return (shelt, locs);
    }

    /// <summary>
    /// Create locations for all possible food quest items a slugcat can eat
    /// </summary>
    /// <param name="slugcat"></param>
    /// <param name="includePassage">Whether to include the Gourmand passage as a location</param>
    /// <returns></returns>
    public static HashSet<Location> MakeFoodQuest(SlugcatStats.Name slugcat, bool includePassage)
    {
        List<AccessRule> allGourmRules = [];
        HashSet<Location> foodQuestLocs = [];
        for (int i = 0; i < WinState.GourmandPassageTracker.Length; i++)
        {
            // Skip if slugcat cannot consume this
            if (!Constants.SlugcatFoodQuestAccessibility[slugcat][i]) continue;

            WinState.GourmandTrackerData data = WinState.GourmandPassageTracker[i];
            // Creature food
            if (data.type == AbstractPhysicalObject.AbstractObjectType.Creature)
            {
                List<CreatureAccessRule> rules = data.crits
                    .Select(type => new CreatureAccessRule(type))
                    .ToList();

                AccessRule rule;
                if (rules.Count > 1)
                    rule = new CompoundAccessRule([.. rules], CompoundAccessRule.CompoundOperation.Any);
                else rule = rules[0];

                allGourmRules.Add(rule);
                foodQuestLocs.Add(new Location($"FoodQuest-{data.crits[0].value}", Location.Type.Food, rule));
            }
            // Item food
            else
            {
                AccessRule rule = new ObjectAccessRule(data.type);
                allGourmRules.Add(rule);
                foodQuestLocs.Add(new Location($"FoodQuest-{data.type.value}", Location.Type.Food, rule));
            }
        }

        if (includePassage)
        {
            Location gourmPassage = new("Passage-Gourmand", Location.Type.Passage,
                new CompoundAccessRule([.. allGourmRules], CompoundAccessRule.CompoundOperation.All));
            foodQuestLocs.Add(gourmPassage);
        }

        return foodQuestLocs;
    }

    /// <summary>
    /// Create all the special locations a slugcat can get
    /// </summary>
    /// <param name="slugcat"></param>
    /// <returns></returns>
    public static Dictionary<string, HashSet<Location>> MakeSpecial(SlugcatStats.Name slugcat)
    {
        Dictionary<string, HashSet<Location>> output = [];

        output[RandoRegion.SPECIAL_REG] =
        [
            new Location("Eat_Neuron", Location.Type.Story,
                new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer))
        ];

        switch (slugcat.value)
        {
            // Normal Iterator goals
            case "White":
            case "Yellow":
            case "Gourmand":
            case "Sofanthiel":
                output["SL"] = [new Location("Meet_LttM", Location.Type.Story, new AccessRule("The_Mark"))];
                output["SS"] = [new Location("Meet_FP", Location.Type.Story, AccessRule.Empty())];
                break;
            // Spear finds LttM in LM
            case "Spear":
                output["LM"] = [new Location("Meet_LttM_Spear", Location.Type.Story, AccessRule.Empty())];
                output["SS"] = [new Location("Meet_FP", Location.Type.Story, AccessRule.Empty())];
                break;
            // Hunter Saves LttM, which is a separate check
            case "Red":
                output["SL"] =
                [
                    new Location("Save_LttM", Location.Type.Story, new AccessRule("Object-NSHSwarmer")),
                    new Location("Meet_LttM", Location.Type.Story, new AccessRule("The_Mark"))
                ];
                output["SS"] = [new Location("Meet_FP", Location.Type.Story, AccessRule.Empty())];
                break;
            // Artificer cannot meet LttM
            case "Artificer":
                output["SS"] = [new Location("Meet_FP", Location.Type.Story, AccessRule.Empty())];
                break;
            // Rivulet does a murder in RM, separate check
            case "Rivulet":
                output["SL"] = [new Location("Meet_LttM", Location.Type.Story, new AccessRule("The_Mark"))];
                output["RM"] =
                [
                    new Location("Kill_FP", Location.Type.Story,
                        new OptionAccessRule(nameof(OptionStruct.useEnergyCell)))
                ];
                break;
            // Saint has 2 separate checks for ascending
            case "Saint":
                output["SL"] = [new Location("Ascend_LttM", Location.Type.Story, new KarmaAccessRule(10))];
                output["CL"] = [new Location("Ascend_FP", Location.Type.Story, new KarmaAccessRule(10))];
                break;
            case "Watcher":
                output["WORA"] =
                [
                    new Location("Prince-1", Location.Type.Story, new RippleAccessRule(3)),
                    new Location("Prince-2", Location.Type.Story, new RippleAccessRule(5)),
                    new Location("Prince-3", Location.Type.Story, new RippleAccessRule(7)),
                    new Location("Prince-4", Location.Type.Story, new RippleAccessRule(9)),
                    new Location("Meet_Ripple_Elder", Location.Type.Story, AccessRule.Empty())
                ];

                AccessRule weaverRule = new CompoundAccessRule(
                    [..AccessRuleConstants.Regions, AccessRuleConstants.CanDynamicWarp],
                    CompoundAccessRule.CompoundOperation.All);

                output["WRSA"] =
                [
                    new Location("Weaver-1", Location.Type.Story, weaverRule),
                    new Location("Weaver-2", Location.Type.Story, weaverRule),
                    new Location("Weaver-3", Location.Type.Story, weaverRule),
                    new Location("Weaver-4", Location.Type.Story, weaverRule),
                ];
                break;
        }

        return output;
    }

    /// <summary>
    /// Creates all the rules for a slugcat's passage locations
    /// </summary>
    /// <param name="slugcat"></param>
    /// <returns></returns>
    public static Dictionary<string, AccessRule> CreatePassageRules(SlugcatStats.Name slugcat)
    {
        Dictionary<string, AccessRule> passageRules = [];

        bool motherUnlocked = ModManager.MSC &&
                              (Plugin.Singleton.rainWorld.progression.miscProgressionData.beaten_Gourmand_Full ||
                               MoreSlugcats.MoreSlugcats.chtUnlockSlugpups.Value);
        bool canFindSlugpups = slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Red ||
                               (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand);

        foreach (string passage in ExtEnumBase.GetNames(typeof(WinState.EndgameID)))
        {
            AccessRule accessRule = new();
            AccessRule survivorRule = new KarmaAccessRule(5);

            // Skip over impossible passages
            switch (passage)
            {
                case "Survivor":
                    // Watcher gets Survivor for free
                    accessRule = slugcat.IsWatcher() ? AccessRule.Empty() : survivorRule;
                    break;
                case "Monk":
                case "Saint":
                    // Much simpler to exclude from logic than to figure out what's reasonable
                    if (slugcat == SlugcatStats.Name.Red) continue;
                    if (ModManager.MSC
                        && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear
                        || slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer) continue;
                    accessRule = new CompoundAccessRule(
                        [
                            survivorRule,
                            new CompoundAccessRule(AccessRuleConstants.MonkFoods,
                                CompoundAccessRule.CompoundOperation.AtLeast, 3)
                        ],
                        CompoundAccessRule.CompoundOperation.All);
                    break;
                case "Hunter":
                    if (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint) continue;
                    // Hunter passage for carnivores is easy pretty much anywhere,
                    // check for a single food object to ensure we aren't in SS or some similar region
                    int foodCount = AccessRuleConstants.StrictCarnivores.Contains(slugcat) ? 1 : 3;
                    accessRule = new CompoundAccessRule(
                        [
                            survivorRule,
                            new CompoundAccessRule(AccessRuleConstants.HunterFoods,
                                CompoundAccessRule.CompoundOperation.AtLeast, foodCount)
                        ],
                        CompoundAccessRule.CompoundOperation.All);
                    break;
                case "Outlaw":
                    if (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint) continue;
                    // Outlaw creatures aren't filtered exceptionally well,
                    // so the requirements are higher to compensate
                    accessRule = new CompoundAccessRule(
                        [
                            survivorRule,
                            new CompoundAccessRule(AccessRuleConstants.OutlawCrits,
                                CompoundAccessRule.CompoundOperation.AtLeast, 8)
                        ],
                        CompoundAccessRule.CompoundOperation.All);
                    break;
                case "Chieftain":
                    if (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer) continue;
                    accessRule = new CreatureAccessRule(CreatureTemplate.Type.Scavenger);
                    break;
                case "Traveller":
                    if (slugcat.IsWatcher()) continue;
                    accessRule = new CompoundAccessRule(
                        [
                            .. SlugcatStats.SlugcatStoryRegions(slugcat)
                                .Select(r => new RegionAccessRule(r))
                        ],
                        CompoundAccessRule.CompoundOperation.All);
                    break;
                case "DragonSlayer":
                    if (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint) continue;
                    accessRule = new CompoundAccessRule(AccessRuleConstants.Lizards,
                        CompoundAccessRule.CompoundOperation.AtLeast, 6);
                    break;
                case "Friend":
                    accessRule = new CompoundAccessRule(AccessRuleConstants.Lizards,
                        CompoundAccessRule.CompoundOperation.Any);
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

                    List<AccessRule> rules =
                    [
                        survivorRule,
                        new AccessRule("The_Mark"),
                        new CompoundAccessRule(AccessRuleConstants.Regions,
                            CompoundAccessRule.CompoundOperation.AtLeast, 4)
                    ];
                    // These slugcats need to meet LttM first
                    if (slugcat == SlugcatStats.Name.White
                        || slugcat == SlugcatStats.Name.Yellow
                        || (ModManager.MSC && slugcat ==
                            MoreSlugcatsEnums.SlugcatStatsName.Gourmand))
                    {
                        rules.Add(new RegionAccessRule("SL"));
                    }

                    accessRule = new CompoundAccessRule([.. rules],
                        CompoundAccessRule.CompoundOperation.All);

                    break;
                case "Martyr":
                    accessRule = new CompoundAccessRule(AccessRuleConstants.Regions,
                        CompoundAccessRule.CompoundOperation.AtLeast, 5);
                    break;
                case "Nomad":
                    if (slugcat.IsWatcher()) continue;
                    accessRule = new CompoundAccessRule(AccessRuleConstants.Regions,
                        CompoundAccessRule.CompoundOperation.AtLeast, 4);
                    break;
                case "Pilgrim":
                    if (slugcat.IsWatcher()) continue;
                    accessRule = new CompoundAccessRule(
                        [
                            .. SlugcatStats.SlugcatStoryRegions(slugcat)
                                .Where(r => World.CheckForRegionGhost(slugcat, r))
                                .Select(r => new RegionAccessRule(r))
                        ],
                        CompoundAccessRule.CompoundOperation.All);
                    break;
                case "Mother":
                    if (motherUnlocked && canFindSlugpups)
                    {
                        // TODO: Add better check for pup regions if we find another use for property file parsing to justify it
                        // Surely there's a pup spawnable region within a group of 5.
                        // The correct way to do this is by reading pup spawn chances from region properties files,
                        // but it does not feel worth parsing those every OnModsInit just for this one rule
                        accessRule = new CompoundAccessRule(AccessRuleConstants.Regions,
                            CompoundAccessRule.CompoundOperation.AtLeast, 5);
                    }
                    else continue;

                    break;
                case "Gourmand":
                    // Gourmand is handled in Food Quest
                    continue;
            }

            passageRules[passage] = accessRule;
        }

        return passageRules;
    }

    private static bool IsWatcher(this SlugcatStats.Name slugcat)
    {
        return ModManager.Watcher && slugcat == WatcherEnums.SlugcatStatsName.Watcher;
    }
}