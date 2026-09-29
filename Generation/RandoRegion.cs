using System;
using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.Generation
{
    public class RandoRegion(string id, HashSet<Location> locations) : IEquatable<RandoRegion>
    {
        /// <summary> Constant storing the ID for the Passage region </summary>
        public const string PASSAGE_REG = "Passages";

        /// <summary> Constant storing the ID for the Food Quest region </summary>
        public const string FOODQUEST_REG = "FoodQuest";

        /// <summary> Constant storing the ID for the Special region </summary>
        public const string SPECIAL_REG = "Special";
        
        public readonly string ID = id;
        /// <summary> True if this region isn't a normal in-game region ID </summary>
        public bool isSpecial = !Region.GetFullRegionOrder().Contains(id);
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
            return connections.Any(con => con.TravelPossible(state, con.OtherSide(this)));
        }

        public override string ToString()
        {
            string[] output =
            [
                ID,
                "\tLocations:",
                .. allLocations.Select(l => $"\t\t{l} => {l.accessRule}"),
                "\tConnections:",
                .. connections.Select(c => $"\t\t{c} => \n\t\t\t{c.requirements.Item1}, \n\t\t\t{c.requirements.Item2}"),
                "\tShelters:",
                $"\t\t{string.Join(", ", shelters)}"
            ];
            return string.Join("\n", output);
        }

        public override bool Equals(object obj)
        {
            return obj is RandoRegion loc && Equals(loc);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public bool Equals(RandoRegion other)
        {
            return other != null && ID.Equals(other.ID);
        }
    }

    /// <summary>
    /// Instructions for creating a subregion during generation.
    /// See <see cref="State.DefineSubRegion"/> for more details on subregions
    /// </summary>
    public struct SubregionBlueprint(string baseRegion, string id, string[] locations, string[] connections, string[] shelters, (AccessRule, AccessRule) rules)
    {
        /// <summary>
        /// ID of this subregion
        /// </summary>
        public string ID = id;
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
        public (AccessRule, AccessRule) rules = rules;
    }
}
