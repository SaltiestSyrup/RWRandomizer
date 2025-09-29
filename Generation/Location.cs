
using System;

namespace RainWorldRandomizer.Generation
{
    public class Location(string ID, Location.Type type, AccessRule accessRule) : IEquatable<Location>
    {
        // TODO: Location.Type isn't currently well utilized.
        // Either remove it or use it somehow and make it an ExtEnum
        public enum Type
        {
            Unknown,
            Pearl,
            Token,
            Echo,
            Story,
            Food,
            Passage,
            Shelter,
            Flower
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
}
