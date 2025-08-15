using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RainWorldRandomizer.Generation.CustomLogicBuilder;

namespace RainWorldRandomizer.Generation
{
    /// <summary>
    /// Test and demonstration for adding randomizer logic through another class or mod
    /// </summary>
    public static class InvCompat
    {
        /// <summary>
        /// Call this after <see cref="Constants.InitializeConstants"/> and before <see cref="StaticWorld.InitStaticWorld"/>.
        /// Easiest is just to do this in <see cref="RainWorld.PostModsInit"/>
        /// </summary>
        public static void Init()
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

            // Define the default starting room and region, for when start is not randomized
            Constants.SlugcatDefaultStartingDen[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel] = "SH_S09";
            Constants.SlugcatStartingRegion[MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel] = "SH";
        }

        /// <summary>
        /// Call this in a hook to <see cref="CustomLogicBuilder.DefineLogic"/>
        /// </summary>
        public static void DefineLogic()
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
        }
    }
}
