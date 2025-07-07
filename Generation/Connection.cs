using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer.Generation
{
    public class Connection(string ID, RandoRegion[] regions, AccessRule[] rules)
    {
        /// <summary>
        /// ID of this connection. All gate connection IDs are the actual gate ID, ex: GATE_SU_DS
        /// </summary>
        public string ID = ID;
        public RandoRegion[] regions = regions;
        // 0: =>
        // 1: <=
        public AccessRule[] requirements = rules;
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
            if (region == regions[0]) return requirements[0].IsMet(state);
            if (region == regions[1]) return requirements[1].IsMet(state);
            throw new ArgumentException("Given region is not part of this connection", "region");
        }

        /// <summary>
        /// Gets the region that the given region connects to
        /// </summary>
        /// <param name="region">The region to start from</param>
        /// <returns>The other connected region</returns>
        /// <exception cref="ArgumentException">If <paramref name="region"/> is not a part of this connection</exception>
        public RandoRegion OtherSide(RandoRegion region)
        {
            if (region == regions[0]) return regions[1];
            if (region == regions[1]) return regions[0];
            throw new ArgumentException("Given region is not part of this connection", "region");
        }
    }

    public struct ConnectionBlueprint(string ID, string[] regions, AccessRule[] rules)
    {
        public string ID = ID;
        public string[] regions = regions;
        public AccessRule[] rules = rules;
    }
}
