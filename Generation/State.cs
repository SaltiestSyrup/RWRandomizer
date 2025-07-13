using System;
using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.Generation
{
    public class State(SlugcatStats.Name slugcat, SlugcatStats.Timeline timeline, int startKarma)
    {
        /// <summary>
        /// Every location in this state. Does not change after initialization.
        /// </summary>
        public HashSet<Location> AllLocations { get; private set; }
        /// <summary>
        /// All locations with unsatisfied requirements. Must be empty at end of generation.
        /// </summary>
        public HashSet<Location> UnreachedLocations { get; private set; }
        /// <summary>
        /// All locations that are currently in logic and have not been placed
        /// </summary>
        public HashSet<Location> AvailableLocations { get; private set; }

        /// <summary>
        /// Every region in this state. May be added to during custom rule application.
        /// </summary>
        public HashSet<RandoRegion> AllRegions { get; private set; }
        /// <summary>
        /// All regions not yet accessible. Must be empty at end of generation.
        /// </summary>
        public HashSet<RandoRegion> UnreachedRegions { get; private set; }
        /// <summary>
        /// All regions currently in logic.
        /// </summary>
        public HashSet<RandoRegion> AvailableRegions { get; private set; }

        /// <summary>
        /// Every region connection in this state. Does not change after initialization.
        /// </summary>
        public HashSet<Connection> AllConnections { get; private set; }

        public SlugcatStats.Name Slugcat { get; private set; } = slugcat;
        public SlugcatStats.Timeline Timeline { get; private set; } = timeline;
        public int MaxKarma { get; private set; } = startKarma;
        public HashSet<string> SpecialProg { get; private set; } = [];
        //public HashSet<string> Regions { get; private set; } = [];
        public HashSet<string> Gates { get; private set; } = [];
        public HashSet<CreatureTemplate.Type> Creatures { get; private set; } = [];
        public HashSet<AbstractPhysicalObject.AbstractObjectType> Objects { get; private set; } = [];

        public void DefineLocs(HashSet<RandoRegion> allRegions)
        {
            AllRegions = allRegions;
            AllLocations = [];
            AllConnections = [];

            foreach (RandoRegion region in AllRegions)
            {
                AllConnections.UnionWith(region.connections);

                foreach (Location loc in region.allLocations)
                {
                    // If a location with the same name already exists, combine their AccessRules
                    // and consider them the same location. This effectively means when the same
                    // location is collectible in multiple places, it will be deemed accessible
                    // once BOTH conditions are met.
                    if (AllLocations.Contains(loc))
                    {
                        Location oldLoc = AllLocations.First(l => l.ID == loc.ID);
                        AccessRule mergedRule = new CompoundAccessRule(
                            [oldLoc.accessRule, loc.accessRule],
                            CompoundAccessRule.CompoundOperation.All);

                        loc.accessRule = mergedRule;
                        oldLoc = loc;
                    }
                    else AllLocations.Add(loc);
                }
            }

            UnreachedRegions = [.. AllRegions];
            AvailableRegions = [];
            UnreachedLocations = [.. AllLocations];
            AvailableLocations = [];
        }

        public void DefineSubRegion(RandoRegion baseRegion, string newID, HashSet<Location> locations, HashSet<Connection> connections, AccessRule[] rules)
        {
            if (!locations.IsSubsetOf(baseRegion.allLocations)) throw new ArgumentException("Locations must be a subset of region locations", "locations");
            if (!connections.IsSubsetOf(baseRegion.connections)) throw new ArgumentException("Connections must be a subset of region connections", "connections");

            // Remove elements of orig region
            baseRegion.allLocations = [.. baseRegion.allLocations.Except(locations)];
            baseRegion.connections = [.. baseRegion.connections.Except(connections)];

            RandoRegion subRegion = new(newID, locations);
            Connection bridge = new($"SUBREG_{baseRegion.ID}_{newID}", [baseRegion, subRegion], rules);

            // Rebind destination of taken connections
            foreach (var con in connections)
            {
                int index = con.regions.IndexOf(baseRegion);
                con.regions[index] = subRegion;
            }

            baseRegion.connections.Add(bridge);
            subRegion.connections = [.. connections, bridge];

            AllRegions.Add(subRegion);
            UnreachedRegions.Add(subRegion);
            AllConnections.Add(bridge);
        }

        /// <summary>
        /// Directly add a region to this state
        /// </summary>
        /// <param name="ID"></param>
        public void AddRegion(string ID)
        {
            RandoRegion region = AllRegions.First(r => r.ID == ID);
            UnreachedRegions.Remove(region);
            AvailableRegions.Add(region);
            region.hasReached = true;
            AddRegionCreaturesAndObjects(region);
            RecalculateState();
        }

        /// <summary>
        /// Mark a gate as accessible to this state
        /// </summary>
        public void AddGate(string gateName)
        {
            Gates.Add(gateName);
            //UpdateGate(gateName);
            RecalculateState();
        }

        /// <summary>
        /// Add a special progression item to this state
        /// </summary>
        public void AddOtherProgItem(string itemName)
        {
            if (itemName.Equals("Karma")) MaxKarma++;
            else SpecialProg.Add(itemName);
            RecalculateState();
        }

        public bool HasRegion(string regionShort)
        {
            return AvailableRegions.Any(r => r.ID == regionShort);
        }

        public bool HasAllRegions()
        {
            return AllRegions.SetEquals(AvailableRegions);
        }

        public Location PopRandomLocation(ref Random random)
        {
            if (AvailableLocations.Count == 0) return null;

            Location chosen = AvailableLocations.ElementAt(random.Next(AvailableLocations.Count));
            AvailableLocations.Remove(chosen);
            return chosen;
        }

        public void FindAndRemoveLocation(Location loc)
        {
            AllLocations.Remove(loc);
            UnreachedLocations.Remove(loc);
            AvailableLocations.Remove(loc);

            foreach (RandoRegion region in AllRegions)
            {
                region.allLocations.Remove(loc);
            }
        }

        /// <summary>
        /// Recompute the current State by searching unreached locations for newly reachable ones
        /// </summary>
        private void RecalculateState()
        {
            UpdateRegions();

            HashSet<Location> newLocs = [];
            foreach (RandoRegion region in AvailableRegions)
            {
                // Skip if region checks are fully accessible
                if (!region.allLocationsReached)
                {
                    // Find and set status of all newly reached locations
                    HashSet<Location> newRegionLocs = [.. region.allLocations.Where(r => !r.hasReached && r.CanReach(this))];
                    foreach (Location loc in newRegionLocs) loc.hasReached = true;

                    if (region.allLocations.All(r => r.hasReached)) region.allLocationsReached = true;

                    newLocs.UnionWith(newRegionLocs);
                }
            }

            // Remove any locations that shouldn't be present before adding to available
            newLocs = [.. UnreachedLocations.Intersect(newLocs)];

            UnreachedLocations.ExceptWith(newLocs);
            AvailableLocations.UnionWith(newLocs);
        }

        /// <summary>
        /// Recursively find any new regions from current region connections
        /// </summary>
        private void UpdateRegions()
        {
            List<RandoRegion> newRegions = [];
            foreach (RandoRegion region in AvailableRegions)
            {
                foreach (Connection connection in region.connections)
                {
                    // Ignore this connection if both sides are accessible already
                    if (connection.ConnectedStatus != Connection.ConnectedLevel.OneReached) continue;

                    // Add other region if the directional condition for travel is met
                    if (connection.CanTravel(this, region))
                    {
                        newRegions.Add(connection.OtherSide(region));
                        connection.OtherSide(region).hasReached = true;
                        AddRegionCreaturesAndObjects(region);
                    }
                }
            }
            
            if (newRegions.Count > 0)
            {
                UnreachedRegions.ExceptWith(newRegions);
                AvailableRegions.UnionWith(newRegions);
                UpdateRegions();
            }
        }

        /// <summary>
        /// Add new creatures and objects made accessible by a region
        /// </summary>
        private void AddRegionCreaturesAndObjects(RandoRegion region)
        {
            if (region.isSpecial) return;
            string regionLower = region.ID.ToLowerInvariant();
            for (int i = 0; i < TokenCachePatcher.regionObjects[regionLower].Count; i++)
            {
                if (TokenCachePatcher.regionObjectsAccessibility[regionLower][i].Contains(Slugcat))
                {
                    Objects.Add(TokenCachePatcher.regionObjects[regionLower][i]);
                }
            }
            for (int j = 0; j < TokenCachePatcher.regionCreatures[regionLower].Count; j++)
            {
                if (TokenCachePatcher.regionCreaturesAccessibility[regionLower][j].Contains(Slugcat))
                {
                    Creatures.Add(TokenCachePatcher.regionCreatures[regionLower][j]);
                }
            }
        }

        /// <summary>
        /// Checks if a gate opens up more regions, and does so if true.
        /// </summary>
        /// <param name="gateName">GATE_XX_YY</param>
        /// <returns>Whether a new region was added from this gate</returns>
        //private bool UpdateGate(string gateName)
        //{
        //    string[] gate = Regex.Split(gateName, "_");
        //    string regLeft = Plugin.ProperRegionMap[gate[1]];
        //    string regRight = Plugin.ProperRegionMap[gate[2]];
        //
        //    // One way gate logic
        //    if (Constants.OneWayGates.ContainsKey(gateName))
        //    {
        //        // If the gate only travels right, check for left region access
        //        if (Regions.Contains(regLeft)
        //            && !Constants.OneWayGates[gateName]
        //            && !Regions.Contains(regRight))
        //        {
        //            Regions.Add(regRight);
        //            return true;
        //        }
        //        // If the gate only travels left, check for right region access
        //        if (Regions.Contains(regRight)
        //            && Constants.OneWayGates[gateName]
        //            && !Regions.Contains(regLeft))
        //        {
        //            Regions.Add(regLeft);
        //            return true;
        //        }
        //        return false;
        //    }
        //
        //    if (Regions.Contains(regLeft) ^ Regions.Contains(regRight))
        //    {
        //        Regions.Add(regLeft);
        //        Regions.Add(regRight);
        //        return true;
        //    }
        //
        //    return false;
        //}
    }
}
