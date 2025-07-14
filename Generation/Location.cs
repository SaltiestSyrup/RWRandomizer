
using System;

namespace RainWorldRandomizer.Generation
{
    public class Location(string ID, Location.Type type, AccessRule accessRule) : IEquatable<Location>
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

        public string ID = ID;
        public bool hasReached;
        public Type type = type;
        public AccessRule accessRule = accessRule;

        public bool CanReach(State state) => accessRule.IsMet(state);

        public override string ToString() => ID;

        public override bool Equals(object obj)
        {
            if (obj is null || obj is not Location loc) return false;
            return Equals(loc);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public bool Equals(Location other)
        {
            return ID.Equals(other.ID);
        }
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
