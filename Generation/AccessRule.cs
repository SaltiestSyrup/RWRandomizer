﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        public virtual bool IsPossible(State state)
        {
            return ReqName != IMPOSSIBLE_ID;
        }

        public override string ToString()
        {
            return $"Has item {ReqName}";
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

        public override bool IsPossible(State state)
        {
            return Region.GetFullRegionOrder(state.Timeline).Contains(ReqName);
        }

        public override string ToString()
        {
            return $"Can enter {ReqName}";
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

        public override bool IsPossible(State state)
        {
            return reqAmount > 0 && reqAmount <= 10;
        }

        public override string ToString()
        {
            return $"Has {reqAmount} Karma";
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

        public override string ToString()
        {
            return $"Gate {ReqName} is open";
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

        public override string ToString()
        {
            return $"Can find {ReqName}";
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

        public override string ToString()
        {
            return $"Can find {ReqName}";
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

        public override string ToString()
        {
            return $"Can find Echo {ReqName}";
        }
    }

    /// <summary>
    /// Determines if a location can ever be reached for a given slugcat
    /// </summary>
    public class SlugcatAccessRule : AccessRule
    {
        private SlugcatStats.Name slugcat;

        public SlugcatAccessRule(SlugcatStats.Name slugcat)
        {
            this.slugcat = slugcat;
            ReqName = slugcat.value;
        }

        public override bool IsMet(State state) => true;

        public override bool IsPossible(State state)
        {
            return state.Slugcat == slugcat;
        }

        public override string ToString()
        {
            return $"Playing as {ReqName}";
        }
    }

    /// <summary>
    /// Determines if a location can ever be reached at a spot in the timeline
    /// </summary>
    public class TimelineAccessRule : AccessRule
    {
        public enum TimelineOperation
        {
            At,
            AtOrBefore,
            AtOrAfter,
        }

        private TimelineOperation operation;
        private SlugcatStats.Timeline timeline;

        public TimelineAccessRule(SlugcatStats.Timeline timeline, TimelineOperation operation)
        {
            this.timeline = timeline;
            this.operation = operation;
            ReqName = timeline.value;
        }

        public override bool IsMet(State state) => true;

        public override bool IsPossible(State state)
        {
            switch (operation)
            {
                case TimelineOperation.At:
                    return state.Timeline == timeline;
                case TimelineOperation.AtOrBefore:
                    return SlugcatStats.AtOrBeforeTimeline(state.Timeline, timeline);
                case TimelineOperation.AtOrAfter:
                    return SlugcatStats.AtOrAfterTimeline(state.Timeline, timeline);
                default:
                    return false;
            }
        }

        public override string ToString()
        {
            switch (operation)
            {
                case TimelineOperation.At:
                    return $"Playing at timeline: {ReqName}";
                case TimelineOperation.AtOrBefore:
                    return $"Playing at or before timeline: {ReqName}";
                case TimelineOperation.AtOrAfter:
                    return $"Playing at or after timeline: {ReqName}";
                default:
                    return $"Unknown timeline operation: {ReqName}";
            }
        }
    }

    /// <summary>
    /// Determines if a location can ever be reached under current options.
    /// </summary>
    public class OptionAccessRule : AccessRule
    {
        private PropertyInfo optionProperty;
        private bool inverted;

        /// <summary>
        /// Set location possiblity based on if <paramref name="optionName"/> is enabled.
        /// Will throw an <see cref="ArgumentException"/> if <paramref name="optionName"/> does not exactly match a static <see cref="bool"/> property in <see cref="RandoOptions"/>
        /// </summary>
        /// <param name="inverted">If true, will instead check if option is disabled</param>
        /// <exception cref="ArgumentException"></exception>
        public OptionAccessRule(string optionName, bool inverted = false)
        {
            ReqName = $"Option-{optionName}";
            this.inverted = inverted;

            optionProperty = typeof(RandoOptions).GetProperty(optionName, BindingFlags.Public | BindingFlags.Static, null, typeof(bool), new Type[0], null);

            if (optionProperty == null)
            {
                throw new ArgumentException("Given option does not exist in Options class", "optionName");
            }
        }

        public override bool IsMet(State state) => true;

        public override bool IsPossible(State state)
        {
            // This should always succeed due to check in constructor, but catch exception just in case
            try
            {
                bool optionSet = (bool)optionProperty.GetValue(null);
                return inverted ? !optionSet : optionSet;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"OptionAccessRule tried to fetch invalid Option: {ReqName}");
                Plugin.Log.LogError(e);
                return false;
            }
        }

        public override string ToString()
        {
            return $"{ReqName} is set to {!inverted}";
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

        protected CompoundOperation operation;
        protected AccessRule[] accessRules;
        protected int valAmount;

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

        public override bool IsPossible(State state)
        {
            switch (operation)
            {
                case CompoundOperation.All:
                    return accessRules.All(r => r.IsPossible(state));
                case CompoundOperation.Any:
                    return accessRules.Any(r => r.IsPossible(state));
                case CompoundOperation.AtLeast:
                    int count = accessRules.Sum((r) => { return r.IsPossible(state) ? 1 : 0; });
                    return count >= valAmount;
                default:
                    return false;
            }
        }

        public override string ToString()
        {
            string joinedRules = string.Join(", ", accessRules.Select(r => r.ToString()));
            switch (operation)
            {
                case CompoundOperation.All:
                    return $"ALL of: ({joinedRules})";
                case CompoundOperation.Any:
                    return $"ANY of: ({joinedRules})";
                case CompoundOperation.AtLeast:
                    return $"At least {valAmount} of: ({joinedRules})";
                default:
                    return $"Invalid compound operation containing: ({joinedRules})";
            }
        }
    }

    /// <summary>
    /// Shorthand for a rule allowing any of the given slugcats to be used
    /// </summary>
    public class MultiSlugcatAccessRule : CompoundAccessRule
    {
        public MultiSlugcatAccessRule(SlugcatStats.Name[] slugcats) : base(slugcats.Select((scug) => new SlugcatAccessRule(scug)).ToArray(), CompoundOperation.Any) { }
    }

    public static class AccessRuleConstants
    {
        public static AccessRule[] Lizards;
        public static AccessRule[] Regions;
        public static CompoundAccessRule NeuronAccess;

        public static void InitConstants()
        {
            NeuronAccess = new CompoundAccessRule(new AccessRule[]
            {
                new RegionAccessRule("SS"),
                new RegionAccessRule("SL"),
                new RegionAccessRule("DM"),
                new RegionAccessRule("CL"),
                new RegionAccessRule("RM"),
            }, CompoundAccessRule.CompoundOperation.Any);

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
