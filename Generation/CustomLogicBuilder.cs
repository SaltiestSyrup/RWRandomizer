using MoreSlugcats;
using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.Generation
{
    public static class CustomLogicBuilder
    {
        private static Dictionary<SlugcatStats.Name, LogicPackage> slugcatLogicPackages = [];

        public enum SelectionMethod
        {
            Whitelist,
            Blacklist
        }

        public enum OverlapMethod
        {
            /// <summary>Completely replace old rule with this one</summary>
            Overwrite,
            /// <summary>Combine old rule and this one with an AND condition</summary>
            AndPrevious,
            /// <summary>Combine old rule and this one with an OR condition</summary>
            OrPrevious
        }

        /// <summary>
        /// Add a <see cref="RulePatch"/> on top of an existing rule
        /// </summary>
        /// <param name="origRule">The originally defined rule</param>
        /// <param name="newRule">The new rule to apply on top</param>
        /// <returns>The result of the rule combination</returns>
        public static AccessRule CombineRules(AccessRule origRule, RulePatch newRule)
        {
            // Null rules tell combine to not change anything
            if (newRule.rule is null) return origRule;
            return newRule.overlapMethod switch
            {
                OverlapMethod.AndPrevious => new CompoundAccessRule([origRule, newRule.rule], CompoundAccessRule.CompoundOperation.All),
                OverlapMethod.OrPrevious => new CompoundAccessRule([origRule, newRule.rule], CompoundAccessRule.CompoundOperation.Any),
                _ => newRule.rule,
            };
        }

        /// <summary>
        /// Define slugcats as having custom rules. If a slugcat is not initialized, NO predefined rules will be applied to their generation.
        /// </summary>
        public static void InitNewSlugcats(params SlugcatStats.Name[] slugcats)
        {
            foreach (var slugcat in slugcats)
            {
                if (!slugcatLogicPackages.ContainsKey(slugcat)) slugcatLogicPackages[slugcat] = new();
            }
        }

        public static void ClearDefinedLogic() => slugcatLogicPackages.Clear();

        /// <summary>
        /// Get all the custom logic defined for a slugcat
        /// </summary>
        public static LogicPackage GetLogicForSlugcat(SlugcatStats.Name slugcat)
        {
            if (slugcatLogicPackages.TryGetValue(slugcat, out LogicPackage package)) return package;
            return null;
        }

        /// <summary>
        /// Modify the access rule for a single location
        /// </summary>
        /// <param name="selectionMethod">Whether defined slugcats should be treated as a whitelist or a blacklist</param>
        /// <param name="slugcats">Slugcats this will apply to. If <paramref name="selectionMethod"/> is Blacklist, will apply to every slugcat except those listed.
        /// If none are listed, will apply to all slugcats</param>
        public static void AddLocationRule(string locationID, RulePatch newRule, SelectionMethod selectionMethod = SelectionMethod.Blacklist, params SlugcatStats.Name[] slugcats)
        {
            foreach (var package in slugcatLogicPackages)
            {
                switch (selectionMethod)
                {
                    case SelectionMethod.Whitelist:
                        if (!slugcats.Contains(package.Key)) continue;
                        break;
                    case SelectionMethod.Blacklist:
                        if (slugcats.Contains(package.Key)) continue;
                        break;
                }

                if (package.Value.locationRules.ContainsKey(locationID))
                {
                    package.Value.locationRules[locationID] += newRule;
                }
                else
                {
                    package.Value.locationRules[locationID] = newRule;
                }
            }
        }

        /// <summary>
        /// Modify the access rules of a connection
        /// </summary>
        /// <param name="selectionMethod">Whether defined slugcats should be treated as a whitelist or a blacklist</param>
        /// <param name="slugcats">Slugcats this will apply to. If <paramref name="selectionMethod"/> is Blacklist, will apply to every slugcat except those listed.
        /// If none are listed, will apply to all slugcats</param>
        public static void AddConnectionRule(string connectionID, RulePatch newLeftRule, RulePatch newRightRule,
            SelectionMethod selectionMethod = SelectionMethod.Blacklist, params SlugcatStats.Name[] slugcats)
        {
            foreach (var package in slugcatLogicPackages)
            {
                switch (selectionMethod)
                {
                    case SelectionMethod.Whitelist:
                        if (!slugcats.Contains(package.Key)) continue;
                        break;
                    case SelectionMethod.Blacklist:
                        if (slugcats.Contains(package.Key)) continue;
                        break;
                }

                if (package.Value.connectionRules.ContainsKey(connectionID))
                {
                    package.Value.connectionRules[connectionID] =
                        (package.Value.connectionRules[connectionID].Item1 + newLeftRule,
                        package.Value.connectionRules[connectionID].Item2 + newRightRule);
                }
                else
                {
                    package.Value.connectionRules[connectionID] = (newLeftRule, newRightRule);
                }
            }
        }

        /// <summary>
        /// Create a new subregion
        /// </summary>
        /// <param name="selectionMethod">Whether defined slugcats should be treated as a whitelist or a blacklist</param>
        /// <param name="slugcats">Slugcats this will apply to. If <paramref name="selectionMethod"/> is Blacklist, will apply to every slugcat except those listed.
        /// If none are listed, will apply to all slugcats</param>
        public static void AddSubregion(SubregionBlueprint subregion, SelectionMethod selectionMethod = SelectionMethod.Blacklist, params SlugcatStats.Name[] slugcats)
        {
            foreach (var package in slugcatLogicPackages)
            {
                switch (selectionMethod)
                {
                    case SelectionMethod.Whitelist:
                        if (!slugcats.Contains(package.Key)) continue;
                        break;
                    case SelectionMethod.Blacklist:
                        if (slugcats.Contains(package.Key)) continue;
                        break;
                }

                package.Value.newSubregions.Add(subregion);
            }
        }

        /// <summary>
        /// Create a new connnection
        /// </summary>
        /// <param name="selectionMethod">Whether defined slugcats should be treated as a whitelist or a blacklist</param>
        /// <param name="slugcats">Slugcats this will apply to. If <paramref name="selectionMethod"/> is Blacklist, will apply to every slugcat except those listed.
        /// If none are listed, will apply to all slugcats</param>
        public static void AddConnection(ConnectionBlueprint connection, SelectionMethod selectionMethod = SelectionMethod.Blacklist, params SlugcatStats.Name[] slugcats)
        {
            foreach (var package in slugcatLogicPackages)
            {
                switch (selectionMethod)
                {
                    case SelectionMethod.Whitelist:
                        if (!slugcats.Contains(package.Key)) continue;
                        break;
                    case SelectionMethod.Blacklist:
                        if (slugcats.Contains(package.Key)) continue;
                        break;
                }

                package.Value.newConnections.Add(connection);
            }
        }

        /// <summary>
        /// Blacklist a region from being a starting region
        /// </summary>
        /// <param name="regionID">ID of the affected region. Can be the region acronym for generated regions, 
        /// or the given ID of custom subregions</param>
        /// <param name="selectionMethod">Whether defined slugcats should be treated as a whitelist or a blacklist</param>
        /// <param name="slugcats">Slugcats this will apply to. If <paramref name="selectionMethod"/> is Blacklist, will apply to every slugcat except those listed.
        /// If none are listed, will apply to all slugcats</param>
        public static void AddBlacklistedStart(string regionID, SelectionMethod selectionMethod = SelectionMethod.Blacklist, params SlugcatStats.Name[] slugcats)
        {
            foreach (var package in slugcatLogicPackages)
            {
                switch (selectionMethod)
                {
                    case SelectionMethod.Whitelist:
                        if (!slugcats.Contains(package.Key)) continue;
                        break;
                    case SelectionMethod.Blacklist:
                        if (slugcats.Contains(package.Key)) continue;
                        break;
                }

                package.Value.blacklistedStarts.Add(regionID);
            }
        }

        public class LogicPackage
        {
            public Dictionary<string, RulePatch> locationRules = [];
            public Dictionary<string, (RulePatch, RulePatch)> connectionRules = [];
            public List<SubregionBlueprint> newSubregions = [];
            public List<ConnectionBlueprint> newConnections = [];
            public List<string> blacklistedStarts = [];
        }

        /// <summary>
        /// Defines a custom rule to be applied on top of an existing rule. In the case of multiple custom rules for the same location or connection,
        /// The rule that was added later will be applied to the previous rule patch instead. See the + operator for more details.
        /// </summary>
        /// <param name="rule">The rule that will be applied on top of an existing rule. Leave as null to make this patch do nothing</param>
        /// <param name="overlapMethod">How the rule will be added to the original</param>
        public struct RulePatch(AccessRule rule, OverlapMethod overlapMethod = OverlapMethod.Overwrite)
        {
            public AccessRule rule = rule;
            public OverlapMethod overlapMethod = overlapMethod;

            /// <summary>
            /// When adding RulePatches, the left argument's rule is modified by the right argument patch, 
            /// and the new RulePatch has the overlap method of the right argument. 
            /// This new patch is what is later applied to the generated rule.
            /// This can cause some strange behaviors, so it is best to avoid having multiple chaining patches present if possible.
            /// </summary>
            public static RulePatch operator +(RulePatch left, RulePatch right)
            {
                return new(CombineRules(left.rule, right), right.overlapMethod);
            }
        }

        // ---- Define logic functions

        public static void DefineLogic()
        {
            if (ModManager.MSC) DefineLogicMSC();
            else DefineLogicNoDLC();
        }

        private static void DefineLogicNoDLC()
        {
            // Cannot climb SB Ravine
            AddSubregion(new SubregionBlueprint("SB", "SBRavine",
                    ["Echo-SB", "Pearl-SB_ravine", "Broadcast-Chatlog_SB0", "Shelter-SB_S09"],
                    ["GATE_LF_SB"],
                    ["SB_S09"],
                    (new(AccessRule.IMPOSSIBLE_ID), new())));

            // The Exterior is split in half at UW_D06 pre-MSC, as there are no grapple worms for crossing
            // You *could* bring a grapple worm from Chimney but that's too out of the way to be in logic
            AddSubregion(new SubregionBlueprint("UW", "UWWall",
                    ["Pearl-UW", "Echo-UW", "Token-L-UW", "Token-YellowLizard", "Shelter-UW_S01",
                        "Shelter-UW_S03", "Shelter-UW_S04"],
                    ["GATE_SS_UW", "GATE_CC_UW"],
                    ["UW_S01", "UW_S03", "UW_S04"],
                    (new(), new(AccessRule.IMPOSSIBLE_ID))),
                SelectionMethod.Whitelist,
                SlugcatStats.Name.White, SlugcatStats.Name.Yellow);
        }

        private static void DefineLogicMSC()
        {
            // --- Locations ---

            // Token cache fails to filter this pearl to only Past GW
            AddLocationRule("Pearl-MS",
                new RulePatch(new TimelineAccessRule(SlugcatStats.Timeline.Artificer, TimelineAccessRule.TimelineOperation.AtOrBefore)));

            // Artificer can't reach underwater GW token
            AddLocationRule("Token-BrotherLongLegs",
                new RulePatch(new(AccessRule.IMPOSSIBLE_ID)),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Artificer);

            // Waterfront Safari token is in a very silly location for Spearmaster
            AddLocationRule("Token-S-LM",
                new RulePatch(new(AccessRule.IMPOSSIBLE_ID)),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Spear);

            // --- Subregions ---

            // Cannot climb SB Ravine
            AddSubregion(new SubregionBlueprint("SB", "SBRavine",
                    ["Echo-SB", "Pearl-SB_ravine", "Broadcast-Chatlog_SB0", "Shelter-SB_S09"],
                    ["GATE_LF_SB"],
                    ["SB_S09"],
                    (new(AccessRule.IMPOSSIBLE_ID), new())));

            // The Exterior is split in half at UW_C02 during Rivulet's time, they have a hard time crossing it
            AddSubregion(new SubregionBlueprint("UW", "UWWall",
                    ["Pearl-UW", "Echo-UW", "Token-S-UW", "Token-L-UW", "Token-YellowLizard",
                        "Broadcast-Chatlog_Broadcast0", "Shelter-UW_S01", "Shelter-UW_S03",
                        "Shelter-UW_S04", "DevToken-UW_H01", "DevToken_UW_F01"],
                    ["GATE_SS_UW", "GATE_CC_UW", "GATE_UW_LC"],
                    ["UW_S01", "UW_S03", "UW_S04"],
                    (new TimelineAccessRule(SlugcatStats.Timeline.Yellow, TimelineAccessRule.TimelineOperation.AtOrBefore), new())));

            // Only Saint is considered able to climb up into Outskirts filtration
            AddSubregion(new SubregionBlueprint("SU", "SU_Filt",
                    ["Pearl-SU_filt", "Shelter-SU_S05", "DevToken-SU_CAVE01", "DevToken-SU_PMPSTATION01"],
                    ["GATE_OE_SU"],
                    ["SU_S05"],
                    (new(AccessRule.IMPOSSIBLE_ID), new())),
                SelectionMethod.Blacklist,
                MoreSlugcatsEnums.SlugcatStatsName.Saint);

            // The Precipice is completely disconnected from Shoreline
            AddSubregion(new SubregionBlueprint("SL", "SLPrecipice",
                    ["Shelter-SL_S13", "DevToken-SL_BRIDGE01"],
                    ["GATE_UW_SL"],
                    ["SL_S13"],
                    (new(AccessRule.IMPOSSIBLE_ID), new(AccessRule.IMPOSSIBLE_ID))));

            SubregionBlueprint bitterAerie = new("MS", "MSBitterAerie",
                ["Token-S-MS", "Token-MirosVulture", "Echo-MS", "Shelter-MS_S07", "Shelter-MS_S10",
                    "Shelter-MS_BITTERSHELTER", "DevToken-MS_SEWERBRIDGE", "DevToken-MS_X02", "DevToken-MS_BITTEREDGE"],
                ["GATE_SL_MS"],
                ["MS_S07", "MS_S10", "MS_BITTERSHELTER"],
                (new(AccessRule.IMPOSSIBLE_ID), new(AccessRule.IMPOSSIBLE_ID)));

            // For most, Bitter Aerie is unreachable
            AddSubregion(bitterAerie,
                SelectionMethod.Blacklist,
                MoreSlugcatsEnums.SlugcatStatsName.Rivulet, MoreSlugcatsEnums.SlugcatStatsName.Saint);

            // For Saint, Bitter Aerie is free
            bitterAerie.rules = (new(), new());
            AddSubregion(bitterAerie,
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Saint);

            // For Rivulet, it's a bit more complicated.
            // If randomized energy cell, require the randomized item. Else, require access to The Rot
            bitterAerie.rules = (new(AccessRule.IMPOSSIBLE_ID),
                new CompoundAccessRule(
                [
                    new CompoundAccessRule(
                    [
                        new OptionAccessRule("UseEnergyCell"),
                        new AccessRule("Object-EnergyCell")
                    ], CompoundAccessRule.CompoundOperation.All),
                    new CompoundAccessRule(
                    [
                        new OptionAccessRule("UseEnergyCell", true),
                        new RegionAccessRule("RM")
                    ], CompoundAccessRule.CompoundOperation.All),
                ], CompoundAccessRule.CompoundOperation.Any));
            AddSubregion(bitterAerie,
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Rivulet);

            // Only Saint can climb up above LttM
            AddSubregion(new SubregionBlueprint("SL", "SLAboveLttM",
                    ["Echo-SL", "Shelter-SL_STOP", "DevToken-SL_ROOF04", "DevToken-SL_TEMPLE", "DevToken-SL_ROOF03"],
                    ["GATE_SL_MS"],
                    ["SL_STOP"],
                    (new(AccessRule.IMPOSSIBLE_ID), new())),
                SelectionMethod.Blacklist,
                MoreSlugcatsEnums.SlugcatStatsName.Saint);

            // Artificer cannot traverse Sump Tunnel
            AddSubregion(new SubregionBlueprint("VS", "VSSumpTunnel",
                    ["Shelter-VS_S02"],
                    ["GATE_SL_VS"],
                    ["VS_S02"],
                    (new(AccessRule.IMPOSSIBLE_ID), new(AccessRule.IMPOSSIBLE_ID))),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Artificer);

            // --- Connections ---

            // Gourmand needs the mark to enter OE
            AddConnectionRule("GATE_SB_OE",
                new RulePatch(null),
                new RulePatch(new("The_Mark"), OverlapMethod.AndPrevious),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Gourmand);

            // Outskirts filtration gate has no key and is one way
            AddConnectionRule("GATE_OE_SU",
                new RulePatch(new(), OverlapMethod.Overwrite),
                new RulePatch(new(AccessRule.IMPOSSIBLE_ID)));

            // Metropolis can be accessed if the option is enabled
            AddConnectionRule("GATE_UW_LC",
                new RulePatch(null),
                new RulePatch(new OptionAccessRule("ForceOpenMetropolis"), OverlapMethod.AndPrevious),
                SelectionMethod.Blacklist,
                MoreSlugcatsEnums.SlugcatStatsName.Artificer);

            // Artificer needs the Mark and ID drone to enter Metropolis
            AddConnectionRule("GATE_UW_LC",
                new RulePatch(null),
                new RulePatch(new CompoundAccessRule(
                    [
                        new("The_Mark"),
                        new("IdDrone")
                    ], CompoundAccessRule.CompoundOperation.All),
                    OverlapMethod.AndPrevious),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Artificer);

            // Submerged can be accessed if the option is enabled
            AddConnectionRule("GATE_MS_SL",
                new RulePatch(null),
                new RulePatch(new OptionAccessRule("ForceOpenSubmerged"), OverlapMethod.AndPrevious),
                SelectionMethod.Blacklist,
                MoreSlugcatsEnums.SlugcatStatsName.Rivulet);

            // Bitter Aerie to above LttM has no key and is one way
            AddConnectionRule("GATE_SL_MS",
                new RulePatch(new(AccessRule.IMPOSSIBLE_ID)),
                new RulePatch(new()));

            // Artificer cannot traverse Sump Tunnel
            AddConnectionRule("GATE_SL_VS",
                new RulePatch(new(AccessRule.IMPOSSIBLE_ID)),
                new RulePatch(new(AccessRule.IMPOSSIBLE_ID)),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Artificer);

            // --- New Connections ---

            // Create a connection to Rubicon, which has no gate to it
            AddConnection(new ConnectionBlueprint("FALL_SB_HR",
                    ["SB", "HR"],
                    (new KarmaAccessRule(10), new(AccessRule.IMPOSSIBLE_ID))),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Saint);

            // --- Blacklisted Starts ---

            // Arty shouldn't start inside Metropolis
            AddBlacklistedStart("LC", SelectionMethod.Whitelist, MoreSlugcatsEnums.SlugcatStatsName.Artificer);

            // Any start in OE is instant ending access
            AddBlacklistedStart("OE");

            InvCompat.DefineLogic();
        }
    }
}
