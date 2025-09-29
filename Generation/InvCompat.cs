using MoreSlugcats;
using static RainWorldRandomizer.Generation.CustomLogicBuilder;

namespace RainWorldRandomizer.Generation
{
    /// <summary>
    /// Test and demonstration for adding randomizer logic through another class or mod
    /// </summary>
    public class InvCompat : LogicAddon
    {
        public InvCompat()
        {
            // Most importantly, mark the slugcat as playable
            Constants.CompatibleSlugcats.Add(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);

            // Add flags for what food quest foods are edible. You can read what foods these match to in the Constants class
            Constants.SlugcatFoodQuestAccessibility[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel] =
            [
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true,
                true, true, true, true, true, true, true, true,
                true, true, true,
                true, true, true,
                true, true, true, true, true, true, true,
                false, false, false, false,
            ];

            // Define a default starting shelter and region, for when start is not randomized
            Constants.SlugcatDefaultStartingDen[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel] = "SH_S09";
            Constants.SlugcatStartingRegion[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel] = "SH";
        }

        // Logic is written after the base randomizer and other mods behind in load order write their logic.
        // If rules are written that have been defined previously, the rule defined here will apply to the resulting
        // rule from applying previous logic definitions
        public override void DefineLogic()
        {
            // Inv can't reach under-cheese GW tokens
            AddLocationRule("Token-BrotherLongLegs",
                new RulePatch(new(AccessRule.IMPOSSIBLE_ID)),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
            AddLocationRule("Token-RedLizard",
                new RulePatch(new(AccessRule.IMPOSSIBLE_ID)),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);

            // Travelling up the wall isn't in logic for Inv. Also covers the one-way path to Pebbles' access shaft
            AddSubregion(new SubregionBlueprint("UWWall", "UWInvWall",
                    ["Pearl-UW", "Echo-UW", "Shelter-UW_S03"],
                    ["GATE_SS_UW", "GATE_UW_LC"],
                    ["UW_S03"],
                    (new(AccessRule.IMPOSSIBLE_ID), new())),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);

            // Blacklist The Exterior altogether if allow setting not checked
            AddBlacklistedRegion("UW",
                new RulePatch(new OptionAccessRule("AllowExteriorForInv", true)),
                SelectionMethod.Whitelist,
                MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
        }
    }
}
