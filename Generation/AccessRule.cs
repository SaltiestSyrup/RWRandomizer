using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer.Generation
{
    /// <summary>
    /// Base class for all rules, acts as a wildcard by itself.
    /// </summary>
    public class AccessRule
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
        public AccessRuleType Type { get; protected set; }
        /// <summary>
        /// Name of the rule, what is searched for within State
        /// </summary>
        public string ReqName { get; protected set; }

        public AccessRule(string requirementName = "")
        {
            Type = AccessRuleType.Wildcard;
            this.ReqName = requirementName;
        }

        /// <summary>
        /// Returns whether this rule's requirements have been met
        /// </summary>
        /// <param name="state">The current state of the generation process</param>
        public virtual bool IsMet(State state)
        {
            if (ReqName.Equals("")) return true;
            return state.SpecialProg.Contains(ReqName);
        }
    }

    /// <summary>
    /// The most common rule, used to determine the in game region a location can be found in
    /// </summary>
    public class RegionAccessRule : AccessRule
    {
        public RegionAccessRule(string regionShort = null)
        {
            Type = AccessRuleType.Region;
            ReqName = regionShort;
        }

        public override bool IsMet(State state)
        {
            return ReqName != null && state.Regions.Contains(ReqName);
        }
    }

    /// <summary>
    /// Used to set a karma requirement, for various flag checks
    /// </summary>
    public class KarmaAccessRule : AccessRule
    {
        private int reqAmount;

        public KarmaAccessRule(int amount)
        {
            Type = AccessRuleType.Karma;
            ReqName = "Karma";
            reqAmount = amount;
        }

        public override bool IsMet(State state)
        {
            return state.MaxKarma >= reqAmount;
        }
    }

    /// <summary>
    /// Mainly used for special locations that can only be reached from a certain direction
    /// </summary>
    public class GateAccessRule : AccessRule
    {
        public GateAccessRule(string gateName)
        {
            Type = AccessRuleType.Gate;
            ReqName = gateName;
        }

        public override bool IsMet(State state)
        {
            return state.Gates.Contains(ReqName);
        }
    }

    /// <summary>
    /// Determines if a given creature can be found in current state. Useful for Passages and Food Quest
    /// </summary>
    public class CreatureAccessRule : AccessRule
    {
        private CreatureTemplate.Type creature;

        public CreatureAccessRule(CreatureTemplate.Type creature)
        {
            Type = AccessRuleType.Creature;
            ReqName = creature.value;
            this.creature = creature;
        }

        public override bool IsMet(State state)
        {
            return state.Creatures.Contains(creature);
        }
    }

    /// <summary>
    /// Determines if a given PlacedObject can be found in the current state. Useful for Passages and Food Quest
    /// </summary>
    public class ObjectAccessRule : AccessRule
    {
        private AbstractPhysicalObject.AbstractObjectType item;

        public ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType item)
        {
            Type = AccessRuleType.Object;
            ReqName = item.value;
            this.item = item;
        }

        public override bool IsMet(State state)
        {
            return state.Objects.Contains(item);
        }
    }

    /// <summary>
    /// Shorthand rule for Echo accessibility. Directly translates into a Region rule representing where the Echo can be found
    /// </summary>
    public class EchoAccessRule : RegionAccessRule
    {
        public EchoAccessRule(GhostWorldPresence.GhostID echoID)
        {
            Type = AccessRuleType.Echo;
            ReqName = echoID.value;
        }
    }

    /// <summary>
    /// Rule used for any location with requirements more complex than a single state check. 
    /// Most notably applies to Passages and Story locations.
    /// These can be chained together to create arbitrarily complex rules for any situation.
    /// </summary>
    public class CompoundAccessRule : AccessRule
    {
        public enum CompoundOperation
        {
            /// <summary>
            /// All of the rules must be satisfied
            /// </summary>
            All,
            /// <summary>
            /// At least one of the rules must be satisfied
            /// </summary>
            Any, 
            /// <summary>
            /// At least <see cref="valAmount"/> of the rules must be satisfied
            /// </summary>
            AtLeast
        }

        private CompoundOperation operation;
        private AccessRule[] accessRules;
        private int valAmount;

        /// <param name="rules">Array of rules that this rule will reference</param>
        /// <param name="operation">The type of operation used to determine if given rules are met</param>
        /// <param name="valAmount">Optional value utilized by some operations</param>
        public CompoundAccessRule(AccessRule[] rules, CompoundOperation operation, int valAmount = 0)
        {
            accessRules = rules;
            this.operation = operation;
            this.valAmount = valAmount;
        }

        public override bool IsMet(State state)
        {
            switch (operation)
            {
                case CompoundOperation.All:
                    return accessRules.All(r => r.IsMet(state));
                case CompoundOperation.Any:
                    return accessRules.Any(r => r.IsMet(state));
                case CompoundOperation.AtLeast:
                    int count = accessRules.Sum((r) => { return r.IsMet(state) ? 1 : 0; });
                    return count >= valAmount;
                default:
                    return false;
            }
        }
    }

    public static class AccessRuleConstants
    {
        public static AccessRule[] Lizards;
        public static AccessRule[] Regions;

        public static void InitConstants()
        {
            List<AccessRule> lizards = new List<AccessRule>
            {
                new CreatureAccessRule(CreatureTemplate.Type.BlueLizard),
                new CreatureAccessRule(CreatureTemplate.Type.PinkLizard),
                new CreatureAccessRule(CreatureTemplate.Type.GreenLizard),
                new CreatureAccessRule(CreatureTemplate.Type.YellowLizard),
                new CreatureAccessRule(CreatureTemplate.Type.BlackLizard),
                new CreatureAccessRule(CreatureTemplate.Type.WhiteLizard)
            };
            if (ModManager.DLCShared)
            {
                lizards.Add(new CreatureAccessRule(CreatureTemplate.Type.RedLizard));
                lizards.Add(new CreatureAccessRule(CreatureTemplate.Type.CyanLizard));
                lizards.Add(new CreatureAccessRule(DLCSharedEnums.CreatureTemplateType.ZoopLizard));
                lizards.Add(new CreatureAccessRule(DLCSharedEnums.CreatureTemplateType.SpitLizard));
            }
            Lizards = lizards.ToArray();

            List<string> regionStrings = Region.GetFullRegionOrder();
            Regions = new AccessRule[regionStrings.Count];
            for(int i = 0; i < Regions.Length; i++)
            {
                Regions[i] = new RegionAccessRule(regionStrings[i]);
            }
        }
    }
}
