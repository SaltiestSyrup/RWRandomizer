using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MoreSlugcats;
using RWCustom;

namespace RainWorldRandomizer.Generation;

public static class ItemHelpers
{
    public static (HashSet<string>, List<Item>) MakeGateConnections(SlugcatStats.Name slugcat,
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
            bool forceOpen = Constants.ForceOpenGates.Contains(gate);
            Connection connection = new(gate,
            [
                regions[regionShorts[0]],
                regions[regionShorts[1]]
            ], forceOpen ? AccessRule.Empty() : new GateAccessRule(gate));
            connection.Create();

            gateNames.Add(gate);

            // Don't create items for gates that are always open
            if (!forceOpen)
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

    public static (HashSet<string>, List<Item>) MakeWarpConnections(Dictionary<string, RandoRegion> regions)
    {
        HashSet<string> warpNames = [];
        List<Item> warpItems = [];
        List<WarpConnection> hangingConnections = [];

        // Normal static warps
        foreach (string warp in Custom.rainWorld.regionWarpRooms.SelectMany(kvp => kvp.Value))
        {
            WarpConnection data = WarpConnection.FromStatic(warp.Split(':'));
            if (!regions.ContainsKey(data.startReg) || !regions.ContainsKey(data.destReg))
            {
                continue;
            }

            string warpID = string.Join("-", new[] { data.startReg, data.destReg }.OrderBy(x => x));
            string itemName = $"Warp-{warpID}";
            bool forceOpen = Constants.UnkeyableWarps.Contains(warpID);
            Connection connection;

            // This is a one-way warp, create the connection
            if (data.oneWay)
            {
                connection = new Connection(itemName,
                    [
                        regions[data.startReg],
                        regions[data.destReg]
                    ],
                    (forceOpen ? AccessRule.Empty() : new WarpAccessRule(itemName, data.ripple),
                        AccessRule.Impossible()));
            }
            // We found the second half of a two-way warp pair, create it
            else if (hangingConnections.FirstOrDefault(con => con.MatchPair(data)) is WarpConnection other)
            {
                connection = new Connection(itemName,
                    [
                        regions[data.startReg],
                        regions[data.destReg]
                    ],
                    (forceOpen ? AccessRule.Empty() : new WarpAccessRule(itemName, data.ripple),
                        forceOpen ? AccessRule.Empty() : new WarpAccessRule(itemName, other.ripple)));
                hangingConnections.Remove(other);
            }
            // This is a two-way, but we don't have the other half so save it for now and continue
            else
            {
                hangingConnections.Add(data);
                continue;
            }

            // Finalize the connection if we made one
            connection.Create();
            warpNames.Add(itemName);
            if (!forceOpen)
            {
                warpItems.Add(new Item(itemName, Item.Type.Gate, Item.Importance.Progression));
            }
        }

        // Spinning Top warps
        foreach (string stWarp in Custom.rainWorld.regionSpinningTopRooms.SelectMany(kvp => kvp.Value))
        {
            WarpConnection data = WarpConnection.FromST((stWarp.Split(':')));
            if (!regions.ContainsKey(data.startReg) || !regions.ContainsKey(data.destReg))
            {
                continue;
            }

            string warpID = string.Join("-", new[] { data.startReg, data.destReg }.OrderBy(x => x));
            string itemName = $"Warp-{warpID}";
            bool forceOpen = Constants.UnkeyableWarps.Contains(warpID);

            Connection connection = new(
                itemName,
                [
                    regions[data.startReg],
                    regions[data.destReg]
                ],
                (forceOpen ? AccessRule.Empty() : new WarpAccessRule(itemName, data.ripple),
                    AccessRule.Impossible()));

            // Finalize the connection
            connection.Create();
            warpNames.Add(itemName);
            if (!forceOpen)
            {
                warpItems.Add(new Item(itemName, Item.Type.Gate, Item.Importance.Progression));
            }
        }

        return (warpNames, warpItems);
    }

    private static string DetermineNullWarpDest(string startReg, bool ripple)
    {
        return startReg switch
        { 
            "WARA" => ripple ? "WAUA" : "WRSA",
            "WSSR" => "WORA",
            _ => ripple ? "WRSA" : "NULL"
        };
    }

    private class WarpConnection()
    {
        public string startReg;
        public bool ripple;
        public bool oneWay;
        public string destReg;

        public bool MatchPair(WarpConnection other)
        {
            return startReg.Equals(other.destReg) && destReg.Equals(other.startReg);
        }

        public static WarpConnection FromStatic(string[] split)
        {
            WarpConnection warp = new WarpConnection
            {
                startReg = split[0].Split('_')[0].ToUpperInvariant(),
                ripple = split[1] == "1",
                oneWay = split[2] == "1",
                destReg = split[3].Split('_')[0].ToUpperInvariant()
            };

            if (warp.destReg.Equals("NULL")) warp.destReg = DetermineNullWarpDest(warp.startReg, warp.ripple);

            return warp;
        }

        public static WarpConnection FromST(string[] split)
        {
            WarpConnection warp = new WarpConnection
            {
                startReg = split[0].Split('_')[0].ToUpperInvariant(),
                ripple = false,
                oneWay = true,
                destReg = split[2].Split('_')[0].ToUpperInvariant()
            };
            
            if (warp.destReg.Equals("NULL")) warp.destReg = DetermineNullWarpDest(warp.startReg, warp.ripple);

            return warp;
        }
    }
}