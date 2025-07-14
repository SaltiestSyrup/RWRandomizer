using System;

namespace RainWorldRandomizer.Generation
{
    public class Connection(string ID, RandoRegion[] regions, AccessRule[] rules) : IEquatable<Connection>
    {
        /// <summary>
        /// ID of this connection. All gate connection IDs are the actual gate ID, ex: GATE_SU_DS
        /// </summary>
        public string ID = ID;
        /// <summary>
        /// Regions this connects together. Assumed to be of length 2
        /// </summary>
        public RandoRegion[] regions = regions;
        // 0: Travelling from left to right ( => ) 
        // 1: Travelling from right to left ( <= )
        /// <summary>
        /// Requirements for travelling this connection.
        /// Index 0 is left to right, and index 1 is right to left
        /// </summary>
        public AccessRule[] requirements = rules;
        /// <summary>
        /// How many of <see cref="regions"/> have been marked accessible
        /// </summary>
        public ConnectedLevel ConnectedStatus
        {
            get
            {
                if (regions[0].hasReached && regions[1].hasReached) return ConnectedLevel.BothReached;
                if (regions[0].hasReached || regions[1].hasReached) return ConnectedLevel.OneReached;
                return ConnectedLevel.Disconnected;
            }
        }

        public enum ConnectedLevel
        {
            Disconnected, OneReached, BothReached
        }

        /// <summary>
        /// Shortcut to create a connection with just one rule, repeated for both directions
        /// </summary>
        public Connection(string ID, RandoRegion[] regions, AccessRule rule) : this(ID, regions, [rule, rule]) { }

        /// <summary>
        /// Whether this connection is currently traversable from a given region
        /// </summary>
        /// <param name="state">The current randomizer state</param>
        /// <param name="region">The region to start from</param>
        /// <returns>True if state can access the other connected region</returns>
        /// <exception cref="ArgumentException">If <paramref name="region"/> is not a part of this connection</exception>
        public bool CanTravel(State state, RandoRegion region)
        {
            if (region.ID == regions[0].ID) return requirements[0].IsMet(state);
            if (region.ID == regions[1].ID) return requirements[1].IsMet(state);
            throw new ArgumentException($"Given region ({region.ID}) is not part of this connection ({ID})", "region");
        }

        /// <summary>
        /// Gets the region that the given region connects to
        /// </summary>
        /// <param name="region">The region to start from</param>
        /// <returns>The other connected region</returns>
        /// <exception cref="ArgumentException">If <paramref name="region"/> is not a part of this connection</exception>
        public RandoRegion OtherSide(RandoRegion region)
        {
            if (region.ID == regions[0].ID) return regions[1];
            if (region.ID == regions[1].ID) return regions[0];
            throw new ArgumentException($"Given region ({region.ID}) is not part of this connection ({ID})", "region");
        }

        public override string ToString()
        {
            return $"{ID} connects {regions[0].ID} and {regions[1].ID}";
        }

        public override bool Equals(object obj)
        {
            if (obj is null || obj is not Connection loc) return false;
            return Equals(loc);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public bool Equals(Connection other)
        {
            return ID.Equals(other.ID);
        }
    }

    /// <summary>
    /// Instructions for creating a custom connection during generation.
    /// See <see cref="Connection"/> for more details on connections
    /// </summary>
    public struct ConnectionBlueprint(string ID, string[] regions, AccessRule[] rules)
    {
        public string ID = ID;
        public string[] regions = regions;
        public AccessRule[] rules = rules;
    }
}
