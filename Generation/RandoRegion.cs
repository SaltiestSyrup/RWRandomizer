using System;
using System.Collections.Generic;

namespace RainWorldRandomizer.Generation
{
    // TODO: Regions will have to store the shelters they contain for starting shelter logic
    public class RandoRegion(string ID, HashSet<Location> locations)
    {
        public string ID = ID;
        /// <summary> True if this region isn't a normal in-game region ID </summary>
        public bool isSpecial = !Region.GetFullRegionOrder().Contains(ID);
        public bool hasReached = false;
        public bool allLocationsReached = false;

        public HashSet<Location> allLocations = locations;
        public HashSet<Connection> connections = [];

        public RandoRegion NewSubregion(string ID, HashSet<Location> locations, HashSet<Connection> connections, AccessRule rule)
        {
            return NewSubregion(ID, locations, connections, [rule, rule]);
        }

        // Pull locations and entrances out of this region and create a new one.
        // A new entrance should be created to connect this and the new region together
        public RandoRegion NewSubregion(string ID, HashSet<Location> locations, HashSet<Connection> connections, AccessRule[] rules)
        {
            if (!locations.IsSubsetOf(allLocations)) throw new ArgumentException("Locations must be a subset of region locations", "locations");
            if (!connections.IsSubsetOf(this.connections)) throw new ArgumentException("Connections must be a subset of region connections", "connections");

            allLocations.ExceptWith(locations);
            this.connections.ExceptWith(connections);

            RandoRegion output = new(ID, locations);
            output.connections = [.. connections, new($"SUBREG_{this.ID}_{ID}", [this, output], rules)];

        public override string ToString()
        {
            string[] output = 
            [
                ID,
                "\tLocations:",
                .. allLocations.Select(l => $"\t\t{l} => {l.accessRule}"),
                "\tConnections:",
                .. connections.Select(c => $"\t\t{c} => \n\t\t\t{c.requirements[0]}, \n\t\t\t{c.requirements[1]}")
            ];
            return string.Join("\n", output);
        }
    }

    public struct SubregionBlueprint(string baseRegion, string ID, string[] locations, string[] connections, AccessRule[] rules)
    {
        /// <summary>
        /// ID of this subregion
        /// </summary>
        public string ID = ID;
        /// <summary>
        /// The region this subregion is a part of
        /// </summary>
        public string baseRegion = baseRegion;
        /// <summary>
        /// The location IDs this should contain
        /// </summary>
        public string[] locations = locations;
        /// <summary>
        /// The connection IDs this should contain
        /// </summary>
        public string[] connections = connections;
        /// <summary>
        /// The AccessRules of the connection to the main region
        /// </summary>
        public AccessRule[] rules = rules;
    }
}
