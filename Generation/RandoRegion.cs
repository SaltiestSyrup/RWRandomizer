using System;
using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.Generation
{
    // TODO: Regions will have to store the shelters they contain for starting shelter logic
    public class RandoRegion(string ID, HashSet<Location> locations) : IEquatable<RandoRegion>
    {
        public string ID = ID;
        /// <summary> True if this region isn't a normal in-game region ID </summary>
        public bool isSpecial = !Region.GetFullRegionOrder().Contains(ID);
        public bool hasReached = false;
        public bool allLocationsReached = false;

        public HashSet<Location> allLocations = locations;
        public HashSet<Connection> connections = [];

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

        public override bool Equals(object obj)
        {
            if (obj is null || obj is not RandoRegion loc) return false;
            return Equals(loc);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public bool Equals(RandoRegion other)
        {
            return ID.Equals(other.ID);
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
