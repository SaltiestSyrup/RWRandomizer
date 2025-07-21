using System;
using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.Generation
{
    public class State(SlugcatStats.Name slugcat, SlugcatStats.Timeline timeline, int startKarma)
    {
        /// <summary>
        /// Every possible location.
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

        /// <summary>
        /// Every shelter that exist in the world of this state.
        /// </summary>
        public HashSet<string> AllShelters { get; private set; }

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
            AllShelters = [];

            foreach (RandoRegion region in AllRegions)
            {
                AllConnections.UnionWith(region.connections);
                AllShelters.UnionWith(region.shelters);

                foreach (Location loc in region.allLocations)
                {
                    // If a location with the same name already exists, combine their AccessRules
                    // and consider them the same location. This effectively means when the same
                    // location is collectible in multiple places, it will be deemed accessible
                    // once BOTH conditions are met and one of their regions is reachable.
                    // This mostly applies to sandbox tokens, which normally have no additional
                    // requirements so rules stay the same
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

        /// <summary>
        /// Creates a subregion of a given region, taking select locations and connections as part of itself
        /// </summary>
        /// <param name="baseRegion">The region this subregion is based off of</param>
        /// <param name="newID">The new ID the subregion will have</param>
        /// <param name="locations">The locations from the base region to take. Must be a subset of <paramref name="baseRegion"/>'s locations</param>
        /// <param name="connections">The connections from the base region to take. Must be a subset of <paramref name="baseRegion"/>'s connections</param>
        /// <param name="rules">The <see cref="AccessRule"/>s of the connection from the base region to the new subregion</param>
        /// <returns>The newly created subregion</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if either the <paramref name="locations"/> or <paramref name="connections"/> 
        /// are not a subset of those in <paramref name="baseRegion"/>
        /// </exception>
        public RandoRegion DefineSubRegion(RandoRegion baseRegion, string newID, HashSet<Location> locations,
            HashSet<Connection> connections, HashSet<string> shelters, AccessRule[] rules)
        {
            if (!locations.IsSubsetOf(baseRegion.allLocations)) throw new ArgumentException("Locations must be a subset of region locations", "locations");
            if (!connections.IsSubsetOf(baseRegion.connections)) throw new ArgumentException("Connections must be a subset of region connections", "connections");

            // Remove elements of orig region
            baseRegion.allLocations.ExceptWith(locations);
            baseRegion.connections.ExceptWith(connections);
            baseRegion.shelters.ExceptWith(shelters);

            RandoRegion subRegion = new(newID, locations);
            Connection bridge = new($"SUBREG_{baseRegion.ID}_{newID}", [baseRegion, subRegion], rules);

            // Rebind destination of taken connections
            foreach (Connection con in connections)
            {
                int index = con.regions.IndexOf(baseRegion);
                con.regions[index] = subRegion;
            }

            subRegion.connections = [.. connections];
            bridge.Create();

            subRegion.shelters = shelters;

            AllRegions.Add(subRegion);
            UnreachedRegions.Add(subRegion);
            AllConnections.Add(bridge);

            return subRegion;
        }

        /// <summary>
        /// Directly mark a region as accessible to this state
        /// </summary>
        /// <param name="ID">A region ID to search for</param>
        public void AddRegion(string ID) => AddRegion(AllRegions.First(r => r.ID == ID));

        /// <summary>
        /// Directly mark a region as accessible to this state
        /// </summary>
        /// <param name="ID">A region within this state</param>
        public void AddRegion(RandoRegion region)
        {
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

        /// <summary>
        /// Finds a region within state by ID
        /// </summary>
        /// <param name="ID"></param>
        /// <returns>The found region, or null if not found</returns>
        public RandoRegion RegionFromID(string ID) => AllRegions.FirstOrDefault(r => r.ID == ID);

        /// <summary>
        /// Finds the region that contains the given shelter
        /// </summary>
        /// <param name="shelter"></param>
        /// <returns>The region in this state containing the shelter, null if not found</returns>
        public RandoRegion RegionOfShelter(string shelter) => AllRegions.FirstOrDefault(r => r.shelters.Contains(shelter));

        /// <summary>
        /// Check if a given region ID is accessible to state
        /// </summary>
        /// <param name="regionShort">The ID of a <see cref="RandoRegion"/> in this state</param>
        public bool HasRegion(string regionShort) => AvailableRegions.Any(r => r.ID == regionShort);

        /// <summary>
        /// Check if every region is accessible to state
        /// </summary>
        public bool HasAllRegions() => AllRegions.SetEquals(AvailableRegions);

        /// <summary>
        /// Removes a random accessible location from state and returns it
        /// </summary>
        /// <param name="random">A Random instance to use for RNG</param>
        public Location PopRandomLocation(ref Random random)
        {
            if (AvailableLocations.Count == 0) return null;

            Location chosen = AvailableLocations.ElementAt(random.Next(AvailableLocations.Count));
            AvailableLocations.Remove(chosen);
            return chosen;
        }

        /// <summary>
        /// Purge a location from state altogether, making it no longer valid
        /// </summary>
        /// <param name="loc"></param>
        public void PurgeLocation(Location loc)
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
        /// Purge a region from state altogether, making it no longer valid
        /// </summary>
        /// <param name="region"></param>
        public void PurgeRegion(RandoRegion region)
        {
            foreach (Connection con in region.connections.ToList())
            {
                con.Destroy();
                AllConnections.Remove(con);
            }
            foreach (Location loc in region.allLocations)
            {
                // If this location exists in multiple places, don't purge it
                if (AllRegions.Count(r => r.allLocations.Contains(loc)) > 1) continue;

                AllLocations.Remove(loc);
                UnreachedLocations.Remove(loc);
                AvailableLocations.Remove(loc);
            }

            AllRegions.Remove(region);
            UnreachedRegions.Remove(region);
            AvailableRegions.Remove(region);
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
                        RandoRegion otherSide = connection.OtherSide(region);
                        newRegions.Add(otherSide);
                        otherSide.hasReached = true;
                        AddRegionCreaturesAndObjects(otherSide);
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
            if (region.isSpecial) return; // "Special" regions are ones that don't match to an in-game region

            // We ask the TokenCachePatcher to give us all the creatures and objects in the region we just got.
            // This could become inaccurate with subregions, as when a region is added it cannot consider whether
            // objects may only exist in a subregion we *don't* have yet.
            // Could become a problem with specific subregions but can't be solved for unless we extend cache to track
            // objects in every room and define subregions as the rooms they encompass.
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
    }
}
