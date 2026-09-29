using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MoreSlugcats;

namespace RainWorldRandomizer.Generation;

public static class ItemHelpers
{
    public static (HashSet<string>, List<Item>) MakeGateItems(SlugcatStats.Name slugcat,
        Dictionary<string, RandoRegion> regions)
    {
        HashSet<string> gateNames = [];
        List<Item> gateItems = [];

        foreach (string karmaLock in Plugin.Singleton.rainWorld.progression.karmaLocks)
        {
            string gate = Regex.Split(karmaLock, " : ")[0];
            string[] split = Regex.Split(gate, "_");
            if (split.Length < 3) continue; // Ignore gates that don't follow the pattern "GATE_[R1]_[R2]"
            string[] regionShorts = [split[1], split[2]];

            // Skip if gate already accounted for
            if (gateNames.Contains(gate)) continue;

            regionShorts[0] = Plugin.ProperRegionMap[slugcat][regionShorts[0]];
            regionShorts[1] = Plugin.ProperRegionMap[slugcat][regionShorts[1]];

            if (regionShorts.Any(regionShort =>
                    // If this region does not exist in the timeline
                    // and is not an alias of an existing region, skip the gate
                    !regions.ContainsKey(regionShort)
                    // If this side of the gate is impossible to reach for the current slugcat, skip it
                    || (TokenCachePatcher.GetRoomAccessibility(regionShort)
                            .TryGetValue(gate.ToLowerInvariant(), out List<SlugcatStats.Name> accessibleTo)
                        && !accessibleTo.Contains(slugcat))))
            {
                continue;
            }

            // Create connection
            // Gates defined as always open are given free passage,
            // though there is likely a custom one-way definition
            Connection connection = new(gate,
            [
                regions[regionShorts[0]],
                regions[regionShorts[1]]
            ], Constants.ForceOpenGates.Contains(gate) ? new AccessRule() : new GateAccessRule(gate));
            connection.Create();

            gateNames.Add(gate);

            // Don't create items for gates that are always open
            if (!Constants.ForceOpenGates.Contains(gate))
            {
                gateItems.Add(new Item(gate, Item.Type.Gate, Item.Importance.Progression));
            }
        }

        return (gateNames, gateItems);
    }

    public static List<Item> MakeSpecialItems(SlugcatStats.Name slugcat, OptionStruct options)
    {
        List<Item> items = [];

        if (!ModManager.MSC || slugcat != MoreSlugcatsEnums.SlugcatStatsName.Saint)
        {
            items.Add(new Item("Neuron_Glow", Item.Type.Other, Item.Importance.Progression));
            items.Add(new Item("The_Mark", Item.Type.Other, Item.Importance.Progression));
        }

        switch (slugcat.value)
        {
            case "Red":
                items.Add(new Item("Object-NSHSwarmer", Item.Type.Object, Item.Importance.Progression));
                items.Add(new Item("PearlObject-Red_stomach", Item.Type.Object,
                    Item.Importance.Progression));
                break;
            case "Artificer":
                items.Add(new Item("IdDrone", Item.Type.Other, Item.Importance.Progression));
                break;
            case "Rivulet":
                if (options.useEnergyCell)
                {
                    items.Add(new Item("Object-EnergyCell", Item.Type.Object, Item.Importance.Progression));
                    items.Add(new Item("Longer_Cycles", Item.Type.Other, Item.Importance.Progression));
                    items.Add(new Item("Disconnect_Pebbles", Item.Type.Other, Item.Importance.Filler));
                }

                break;
            case "Spear":
                items.Add(new Item("PearlObject-Spearmasterpearl", Item.Type.Object,
                    Item.Importance.Progression));
                items.Add(new Item("RewriteSpearPearl", Item.Type.Other, Item.Importance.Progression));
                break;
        }

        return items;
    }
}