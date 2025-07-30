using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            return ReqName is not null && state.HasRegion(ReqName);
        }

        public override bool IsPossible(State state)
        {
            return state.AllRegions.Any(r => r.ID == ReqName);
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
        private readonly int reqAmount;

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
            return reqAmount is > 0 and <= 10;
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
        private readonly CreatureTemplate.Type creature;

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

        public override bool IsPossible(State state)
        {
            // Costly calculation, hopefully fine since this shouldn't be checked after init
            return state.AllRegions
                .Any(r =>
                {
                    string regLower = r.ID.ToLowerInvariant();
                    if (!TokenCachePatcher.regionCreatures.TryGetValue(regLower, out List<CreatureTemplate.Type> critList)) return false;
                    int index = critList.IndexOf(creature);
                    if (index < 0) return false;
                    return TokenCachePatcher.regionCreaturesAccessibility[regLower][index].Contains(state.Slugcat);
                });
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
        private readonly AbstractPhysicalObject.AbstractObjectType item;

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

        public override bool IsPossible(State state)
        {
            // Costly calculation, hopefully fine since this shouldn't be checked after init
            return state.AllRegions
                .Any(r =>
                {
                    string regLower = r.ID.ToLowerInvariant();
                    if (!TokenCachePatcher.regionObjects.TryGetValue(regLower, out List<AbstractPhysicalObject.AbstractObjectType> objList)) return false;
                    int index = objList.IndexOf(item);
                    if (index < 0) return false;
                    return TokenCachePatcher.regionObjectsAccessibility[regLower][index].Contains(state.Slugcat);
                });
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
        private readonly SlugcatStats.Name slugcat;
        public bool inverted;

        /// <param name="invert">If true will pass if chosen is not this slugcat</param>
        public SlugcatAccessRule(SlugcatStats.Name slugcat, bool invert = false)
        {
            this.slugcat = slugcat;
            ReqName = slugcat.value;
            inverted = invert;
        }

        public override bool IsMet(State state) => IsPossible(state);

        public override bool IsPossible(State state)
        {
            return inverted ? state.Slugcat != slugcat : state.Slugcat == slugcat;
        }

        public override string ToString()
        {
            return $"{(inverted ? "Not p" : "P")}laying as {ReqName}";
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

        private readonly TimelineOperation operation;
        private readonly SlugcatStats.Timeline timeline;

        public TimelineAccessRule(SlugcatStats.Timeline timeline, TimelineOperation operation)
        {
            this.timeline = timeline;
            this.operation = operation;
            ReqName = timeline.value;
        }

        public override bool IsMet(State state) => IsPossible(state);

        public override bool IsPossible(State state)
        {
            return operation switch
            {
                TimelineOperation.At => state.Timeline == timeline,
                TimelineOperation.AtOrBefore => SlugcatStats.AtOrBeforeTimeline(state.Timeline, timeline),
                TimelineOperation.AtOrAfter => SlugcatStats.AtOrAfterTimeline(state.Timeline, timeline),
                _ => false,
            };
        }

        public override string ToString()
        {
            return operation switch
            {
                TimelineOperation.At => $"Playing at timeline: {ReqName}",
                TimelineOperation.AtOrBefore => $"Playing at or before timeline: {ReqName}",
                TimelineOperation.AtOrAfter => $"Playing at or after timeline: {ReqName}",
                _ => $"Unknown timeline operation: {ReqName}",
            };
        }
    }

    /// <summary>
    /// Determines if a location can ever be reached under current options.
    /// </summary>
    public class OptionAccessRule : AccessRule
    {
        private readonly PropertyInfo optionProperty;
        private readonly bool inverted;

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

            optionProperty = typeof(RandoOptions).GetProperty(optionName, BindingFlags.Public | BindingFlags.Static, null, typeof(bool), [], null);

            if (optionProperty == null)
            {
                throw new ArgumentException("Given option does not exist in Options class", "optionName");
            }
        }

        public override bool IsMet(State state) => IsPossible(state);

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
    /// <param name="rules">Array of rules that this rule will reference</param>
    /// <param name="operation">The type of operation used to determine if given rules are met</param>
    /// <param name="valAmount">Optional value utilized by some operations</param>
    public class CompoundAccessRule(AccessRule[] rules, CompoundAccessRule.CompoundOperation operation, int valAmount = 0) : AccessRule
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

        protected CompoundOperation operation = operation;
        protected AccessRule[] accessRules = rules;
        protected int valAmount = valAmount;

        public override bool IsMet(State state)
        {
            return operation switch
            {
                CompoundOperation.All => accessRules.All(r => r.IsMet(state)),
                CompoundOperation.Any => accessRules.Any(r => r.IsMet(state)),
                CompoundOperation.AtLeast => accessRules.Sum((r) => { return r.IsMet(state) ? 1 : 0; }) >= valAmount,
                _ => false,
            };
        }

        public override bool IsPossible(State state)
        {
            return operation switch
            {
                CompoundOperation.All => accessRules.All(r => r.IsPossible(state)),
                CompoundOperation.Any => accessRules.Any(r => r.IsPossible(state)),
                CompoundOperation.AtLeast => accessRules.Sum((r) => { return r.IsPossible(state) ? 1 : 0; }) >= valAmount,
                _ => false,
            };
        }

        public override string ToString()
        {
            string seperator = operation switch
            {
                CompoundOperation.All => $" AND ",
                CompoundOperation.Any => $" OR ",
                CompoundOperation.AtLeast => $", ",
                _ => $", ",
            };
            string joinedRules = string.Join(seperator, accessRules.Select(r => r.ToString()));
            return operation switch
            {
                CompoundOperation.All => $"({joinedRules})",
                CompoundOperation.Any => $"({joinedRules})",
                CompoundOperation.AtLeast => $"At least {valAmount} of: ({joinedRules})",
                _ => $"Invalid compound operation containing: ({joinedRules})",
            };
        }
    }

    /// <summary>
    /// Shorthand for a rule allowing any of the given slugcats to be used
    /// </summary>
    /// <param name="invert">If true, will instead pass if slugcat is none of those listed</param>
    public class MultiSlugcatAccessRule(SlugcatStats.Name[] slugcats, bool invert = false) 
        : CompoundAccessRule([.. slugcats.Select((scug) => new SlugcatAccessRule(scug, invert))],
            invert ? CompoundOperation.All : CompoundOperation.Any)
    { }

    public static class AccessRuleConstants
    {
        public static List<SlugcatStats.Name> strictCarnivores = [];

        public static AccessRule[] Lizards;
        public static AccessRule[] OutlawCrits;
        public static AccessRule[] HunterFoods;
        public static AccessRule[] MonkFoods;
        public static AccessRule[] Regions;

        /// <summary>
        /// Initialize constant helpers for creating AccessRules. 
        /// Called after <see cref="StaticWorld.InitStaticWorld"/> (Post mod loading)
        /// </summary>
        public static void InitConstants()
        {
            List<AccessRule> lizards = [];
            List<AccessRule> outlaw = [];
            List<AccessRule> hunter =
            [
                new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.JellyFish),
                new CreatureAccessRule(CreatureTemplate.Type.Centipede),
                new CreatureAccessRule(CreatureTemplate.Type.Fly),
                new CreatureAccessRule(CreatureTemplate.Type.VultureGrub),
                new CreatureAccessRule(CreatureTemplate.Type.Hazer),
            ];
            List<AccessRule> monk = 
            [
                new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.DangleFruit),
                new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.WaterNut),
                new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SeedCob),
                new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SlimeMold),
                new ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer),
            ];

            foreach (string name in ExtEnumBase.GetNames(typeof(CreatureTemplate.Type)))
            {
                CreatureTemplate.Type type = new(name);
                CreatureTemplate template = StaticWorld.GetCreatureTemplate(type);
                if (template is null) continue;

                if (template.IsLizard) lizards.Add(new CreatureAccessRule(type));
                // bodySize check filters out large creatures that are unreasonable to kill for Outlaw
                if (template.countsAsAKill > 1 && template.bodySize < 5f) outlaw.Add(new CreatureAccessRule(type));
            }

            strictCarnivores.Add(SlugcatStats.Name.Red);
            if (ModManager.MSC)
            {
                strictCarnivores.AddRange(
                [
                    MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                    MoreSlugcatsEnums.SlugcatStatsName.Spear
                ]);
            }
            if (ModManager.DLCShared)
            {
                monk.AddRange(
                [
                    new ObjectAccessRule(DLCSharedEnums.AbstractObjectType.LillyPuck),
                    new ObjectAccessRule(DLCSharedEnums.AbstractObjectType.GlowWeed),
                    new ObjectAccessRule(DLCSharedEnums.AbstractObjectType.DandelionPeach),
                    new ObjectAccessRule(DLCSharedEnums.AbstractObjectType.GooieDuck),
                ]);
            }

            Lizards = [.. lizards];
            OutlawCrits = [.. outlaw];
            HunterFoods = [.. hunter];
            MonkFoods = [.. monk];

            List<string> regionStrings = Region.GetFullRegionOrder();
            Regions = new AccessRule[regionStrings.Count];
            for (int i = 0; i < Regions.Length; i++)
            {
                Regions[i] = new RegionAccessRule(regionStrings[i]);
            }
        }
    }
}
