
namespace RainWorldRandomizer.Generation
{
    public class Location(string id, Location.Type type, AccessRule accessRule)
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

        public string id = id;
        public bool hasReached;
        public Type type = type;
        public AccessRule accessRule = accessRule;

        public bool CanReach(State state) => accessRule.IsMet(state);

        public override string ToString() => id;
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
