using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer.Generation
{
    public class Location
    {
        public enum Type
        {
            Unknown,
            Pearl,
            Token, 
            Echo, 
            Story, 
            Food, 
            Passage, 
            Shelter
        }

        public string id;
        public Type type;
        protected AccessRule accessRule;

        public Location(string id, Type type, AccessRule accessRule)
        {
            this.id = id;
            this.type = type;
            this.accessRule = accessRule;
        }

        public bool CanReach(State state) => accessRule.IsMet(state);
    }

    //public class PearlLocation : Location
    //{
    //    readonly string pearlID;
    //    readonly string region;

    //    public PearlLocation(Type type, string pearlID, string region) : base(type)
    //    {
    //        this.pearlID = pearlID;
    //        this.region = region;
    //        this.accessRule = new AccessRule(AccessRule.AccessRuleType.Region, region);
    //    }

    //    public override bool CanReach(State state)
    //    {
    //        return state.Regions.Contains(region);
    //    }
    //}
}
