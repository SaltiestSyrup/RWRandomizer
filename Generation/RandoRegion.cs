using System;
using System.Collections.Generic;

namespace RainWorldRandomizer.Generation
{
    public class RandoRegion(string ID, HashSet<Location> locations, HashSet<Connection> connections)
    {
        public string ID = ID;
        public bool hasReached = false;
        public bool allLocationsReached = false;

        public HashSet<Location> allLocations = locations;
        public HashSet<Connection> connections = connections;
        

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

            RandoRegion output = new(ID, locations, connections);
            output.connections.Add(new($"SUBREG_{this.ID}_{ID}", [this, output], rules));

            return output;
        }
    }
}
