namespace RainWorldRandomizer.Generation
{
    /// <summary>
    /// Base class for all rules, acts as a wildcard by itself.
    /// </summary>
    public class AccessRule(string requirementName = "")
    {
        public const string IMPOSSIBLE_ID = "IMPOSSIBLE";

        public enum AccessRuleType
        {
            Wildcard,
            Region,
            Karma,
            Gate,
            Creature,
            Object,
            Echo,
            Compound
        }

        /// <summary>
        /// Allows for quick checking of the type of rule this is without using Type checking
        /// </summary>
        public AccessRuleType Type { get; protected set; } = AccessRuleType.Wildcard;

        /// <summary>
        /// Name of the rule, what is searched for within State
        /// </summary>
        public string ReqName { get; protected set; } = requirementName;

        /// <summary>
        /// Returns whether this rule's requirements have been met
        /// </summary>
        /// <param name="state">The current state of the generation process</param>
        public virtual bool IsMet(State state)
        {
            if (ReqName.Equals("")) return true;
            if (ReqName.Equals(IMPOSSIBLE_ID)) return false;
            return state.SpecialProg.Contains(ReqName);
        }

        public virtual bool IsPossible(State state)
        {
            return ReqName != IMPOSSIBLE_ID;
        }

        public override string ToString()
        {
            if (ReqName is "") return "Always met";
            if (ReqName is IMPOSSIBLE_ID) return "Impossible";
            return $"Has item {ReqName}";
        }

        /// <summary>
        /// Create a blank rule with no requirement
        /// </summary>
        public static AccessRule Empty() => new();

        /// <summary>
        /// Create a rule with an impossible condition
        /// </summary>
        public static AccessRule Impossible() => new(IMPOSSIBLE_ID);
    }
}