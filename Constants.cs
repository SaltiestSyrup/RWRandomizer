using MoreSlugcats;
using System.Collections.Generic;

namespace RainWorldRandomizer
{
    public static class Constants
    {
        /// <summary>
        /// Describes which food quest items can be eaten by each slugcat. Matches order of <see cref="WinState.GourmandPassageTracker"/>
        /// </summary>
        public static readonly Dictionary<SlugcatStats.Name, bool[]> SlugcatFoodQuestAccessibility = new Dictionary<SlugcatStats.Name, bool[]>
        {
            // SlimeMold, DangleFruit, BatFly, Mushroom, BlackLizard, WaterNut, JellyFish, JetFish, GlowWeed, Salamander, Snail,
            // Hazer, EggBug, LillyPuck, YellowLizard, GrappleWorm, Neuron, Centiwing, DandelionPeach, CyanLizard, GooieDuck, RedCenti

            // Centipede, SmallCentipede, VultureGrub, SmallNeedleWorm,
            // GreenLizard, BlueLizard, PinkLizard, WhiteLizard, RedLizard, SpitLizard, ZoopLizard, TrainLizard,
            // BigSpider, SpitterSpider, MotherSpider,
            // Vulture, KingVulture, MirosVulture,
            // LanternMouse, CicadaA, CicadaB, Yeek, BigNeedleWorm,
            // DropBug, MirosBird, Scavenger, ScavengerElite,
            // DaddyLongLegs, BrotherLongLegs, TerrorLongLegs,
            // PoleMimic, TentaclePlant, BigEel, Inspector
            { SlugcatStats.Name.White, new bool[] { 
                true, true, true, true, false, true, true, false, true, false, false, 
                true, false, true, false, false, true, false, true, false, true, true,
                true, true, true, true,
                false, false, false, false, false, false, false, false,
                false, false, false,
                false, false, false,
                false, false, false, false, false,
                false, false, false, false,
                false, false, false,
                false, false, false, false,
            }},
            { SlugcatStats.Name.Yellow, new bool[] {
                true, true, true, true, false, true, true, false, true, false, false,
                true, false, true, false, false, true, false, true, false, true, true,
                true, true, true, true,
                false, false, false, false, false, false, false, false,
                false, false, false,
                false, false, false,
                false, false, false, false, false,
                false, false, false, false,
                false, false, false,
                false, false, false, false
            }},
            { SlugcatStats.Name.Red, new bool[] {
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true,
                true, true, true, true, true, true, true, true,
                true, true, true,
                true, true, true,
                true, true, true, true, true,
                true, true, true, true,
                true, true, true,
                false, false, false, false,
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Gourmand, new bool[] {
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true,
                true, true, true, true, true, true, true, true,
                true, true, true,
                true, true, true,
                true, true, true, true, true,
                true, true, true, true,
                true, true, true,
                false, false, false, false,
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Artificer, new bool[] {
                true, true, true, true, true, true, true, true, false, true, true,
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true,
                true, true, true, true, true, true, true, true,
                true, true, true,
                true, true, true,
                true, true, true, true, true,
                true, true, true, true,
                true, true, true,
                false, false, false, false,
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Spear, new bool[] {
                false, false, true, true, true, false, false, true, false, true, true,
                true, true, false, true, true, true, true, false, true, false, true,
                true, true, true, true,
                true, true, true, true, true, true, true, true,
                true, true, true,
                true, true, true,
                true, true, true, true, true,
                true, true, true, true,
                true, true, true,
                true, true, true, true,
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Rivulet, new bool[] {
                true, true, true, true, false, true, true, false, true, false, false,
                true, false, true, false, false, true, false, true, false, true, true,
                true, true, true, true,
                false, false, false, false, false, false, false, false,
                false, false, false,
                false, false, false,
                false, false, false, false, false,
                false, false, false, false,
                false, false, false,
                false, false, false, false,
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Saint, new bool[] {
                true, true, false, true, false, true, false, false, true, false, false,
                false, false, true, false, false, true, false, true, false, true, false,
                false, false, false,
                false, false, false, false,
                false, false, false, false, false, false, false, false,
                false, false, false,
                false, false, false,
                false, false, false, false, false,
                false, false, false, false,
                false, false, false,
                false, false, false, false,
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, new bool[] {
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true,
                true, true, true, true, true, true, true, true,
                true, true, true,
                true, true, true,
                true, true, true, true, true,
                true, true, true, true,
                true, true, true,
                false, false, false, false,
            }}
        };

        public static readonly Dictionary<SlugcatStats.Name, string> SlugcatStartingRegion = new Dictionary<SlugcatStats.Name, string>()
        {
            { SlugcatStats.Name.White, "SU" },
            { SlugcatStats.Name.Yellow, "SU" },
            { SlugcatStats.Name.Red, "LF" },
            { MoreSlugcatsEnums.SlugcatStatsName.Gourmand, "SH" },
            { MoreSlugcatsEnums.SlugcatStatsName.Artificer, "GW" },
            { MoreSlugcatsEnums.SlugcatStatsName.Spear, "SU" },
            { MoreSlugcatsEnums.SlugcatStatsName.Rivulet, "DS" },
            { MoreSlugcatsEnums.SlugcatStatsName.Saint, "SI" },
            { MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, "SH" },
        };

        // { GATE_NAME, IS_LEFT_TRAVEL }
        public static readonly Dictionary<string, bool> OneWayGates = new Dictionary<string, bool>()
        {
            { "GATE_OE_SU", false },
            { "GATE_LF_SB", false },
        };

        /// <summary>
        /// These gates must always be open to avoid softlock scenarios
        /// </summary>
        public static readonly HashSet<string> ForceOpenGates = new HashSet<string>()
        {
            "GATE_OE_SU", "GATE_SL_MS"
        };
    }
}
