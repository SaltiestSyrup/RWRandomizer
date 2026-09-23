using System;
using System.Collections.Generic;
using System.Linq;
using MoreSlugcats;

namespace RainWorldRandomizer.Generation;

public static class LocationHelpers
{
    public static Location MakeEchoOrSpinningTopLocation(SlugcatStats.Name slugcat, string regionShort)
    {
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
}