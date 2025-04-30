using MonoMod.Utils;
using MoreSlugcats;
using System.Collections.Generic;
using Watcher;

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
        };

        public static readonly Dictionary<SlugcatStats.Name, string> SlugcatDefaultStartingDen = new Dictionary<SlugcatStats.Name, string>()
        {
            { SlugcatStats.Name.White, "SU_S01" },
            { SlugcatStats.Name.Yellow, "SU_S01" },
            { SlugcatStats.Name.Red, "LF_S02" },
        };

        public static void InitializeConstants()
        {
            // Add constant entries that require MSC
            if (ModManager.MSC)
            {
                SlugcatFoodQuestAccessibility.AddRange(new Dictionary<SlugcatStats.Name, bool[]>
                {
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
                });

                SlugcatDefaultStartingDen.AddRange(new Dictionary<SlugcatStats.Name, string>()
                {
                    { MoreSlugcatsEnums.SlugcatStatsName.Gourmand, "SH_S02" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Artificer, "GW_S09" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Rivulet, "DS_S02l" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Spear, "SU_S05" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Saint, "SI_S04" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, "SH_S09" },
                });
            }

            // Add Constant entries that require Watcher
            if (ModManager.Watcher)
            {
                SlugcatDefaultStartingDen.Add(WatcherEnums.SlugcatStatsName.Watcher, "HI_WS01");
            }
        }
    }
}
