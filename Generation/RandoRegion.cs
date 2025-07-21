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
        public HashSet<string> shelters = [];

        /// <summary>
        /// Returns true if there exists at least one connection that could give access to this region
        /// </summary>
        /// <param name="state">The current randomizer state</param>
        public bool IsPossibleToReach(State state)
        {
            foreach (Connection con in connections)
            {
                if (con.TravelPossible(state, con.OtherSide(this))) return true;
            }
            // TODO: Un-hardcode this when generic starting region is added
            if (ID == VanillaGenerator.PASSAGE_REG || ID == VanillaGenerator.SPECIAL_REG) return true;
            return false;
        }

        public override string ToString()
        {
            string[] output =
            [
                ID,
                "\tLocations:",
                .. allLocations.Select(l => $"\t\t{l} => {l.accessRule}"),
                "\tConnections:",
                .. connections.Select(c => $"\t\t{c} => \n\t\t\t{c.requirements[0]}, \n\t\t\t{c.requirements[1]}"),
                "\tShelters:",
                $"\t\t{string.Join(", ", shelters)}"
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

    /// <summary>
    /// Instructions for creating a subregion during generation.
    /// See <see cref="State.DefineSubRegion"/> for more details on subregions
    /// </summary>
    public struct SubregionBlueprint(string baseRegion, string ID, string[] locations, string[] connections, string[] shelters, AccessRule[] rules)
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
        /// The room names of the shelters this should contain
        /// </summary>
        public string[] shelters = shelters;
        /// <summary>
        /// The AccessRules of the connection to the main region
        /// </summary>
        public AccessRule[] rules = rules;
    }
}
