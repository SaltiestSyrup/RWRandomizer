using MonoMod.Utils;
using MoreSlugcats;
using System.Collections.Generic;
using Watcher;

namespace RainWorldRandomizer
{
    public static class Constants
    {
        public static readonly List<SlugcatStats.Name> CompatibleSlugcats = new List<SlugcatStats.Name>();

        /// <summary>
        /// Describes which food quest items can be eaten by each slugcat. Matches order of <see cref="WinState.GourmandPassageTracker"/>
        /// </summary>
        public static readonly Dictionary<SlugcatStats.Name, bool[]> SlugcatFoodQuestAccessibility = new Dictionary<SlugcatStats.Name, bool[]>();
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

        /// <summary>
        /// The fallback starting den for each slugcat. 
        /// Most slugcats start in an intro scene instead of a den, so this defines the closest den to that starting point
        /// </summary>
        public static readonly Dictionary<SlugcatStats.Name, string> SlugcatDefaultStartingDen = new Dictionary<SlugcatStats.Name, string>();

        /// <summary>
        /// The normal starting region for each slugcat
        /// </summary>
        public static readonly Dictionary<SlugcatStats.Name, string> SlugcatStartingRegion = new Dictionary<SlugcatStats.Name, string>();

        // { GATE_NAME, IS_LEFT_TRAVEL }
        public static readonly Dictionary<string, bool> OneWayGates = new Dictionary<string, bool>()
        {
            //{ "GATE_OE_SU", false }, This doesn't matter because it should always be unlocked
            { "GATE_LF_SB", false },
        };

        /// <summary>
        /// These gates must always be open to avoid softlock scenarios
        /// </summary>
        public static readonly HashSet<string> ForceOpenGates = new HashSet<string>()
        {
            "GATE_OE_SU", "GATE_SL_MS"
        };

        public static void InitializeConstants()
        {
            CompatibleSlugcats.Clear();
            SlugcatFoodQuestAccessibility.Clear();
            SlugcatDefaultStartingDen.Clear();
            SlugcatStartingRegion.Clear();

            CompatibleSlugcats.AddRange(new List<SlugcatStats.Name>()
            {
                SlugcatStats.Name.White,
                SlugcatStats.Name.Yellow,
                SlugcatStats.Name.Red,
            });

            SlugcatFoodQuestAccessibility.AddRange(new Dictionary<SlugcatStats.Name, bool[]>
            {
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
            });

            SlugcatDefaultStartingDen.AddRange(new Dictionary<SlugcatStats.Name, string>
            {
                { SlugcatStats.Name.White, "SU_S01" },
                { SlugcatStats.Name.Yellow, "SU_S01" },
                { SlugcatStats.Name.Red, "LF_S02" },
            });

            // Add constant entries that require MSC
            if (ModManager.MSC)
            {
                CompatibleSlugcats.AddRange(new List<SlugcatStats.Name>
                {
                    MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
                    MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                    MoreSlugcatsEnums.SlugcatStatsName.Rivulet,
                    MoreSlugcatsEnums.SlugcatStatsName.Spear,
                    MoreSlugcatsEnums.SlugcatStatsName.Saint
                });

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

            // Infer starting regions from starting dens
            foreach (var pair in SlugcatDefaultStartingDen)
            {
                SlugcatStartingRegion.Add(pair.Key, pair.Value.Split('_')[0]);
            }
        }
    }
}
