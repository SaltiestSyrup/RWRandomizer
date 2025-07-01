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
        public string ID = ID;
        public RandoRegion[] regions = regions;
        // 0: <=
        // 1: =>
        private AccessRule[] requirements = rules;
        public ConnectedLevel connectedLevel;

        public enum ConnectedLevel
        {
            Disconnected, OneReachable, BothReachable
        }

        public Connection(string ID, RandoRegion[] regions, AccessRule rule) : this(ID, regions, [rule, rule]) { }


    }
}
