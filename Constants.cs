using MonoMod.Utils;
using MoreSlugcats;
using System.Collections.Generic;
using Watcher;

namespace RainWorldRandomizer
{
    public static class Constants
    {
        public static readonly List<SlugcatStats.Name> CompatibleSlugcats = [];

        /// <summary>
        /// Describes which food quest items can be eaten by each slugcat. Matches order of <see cref="WinState.GourmandPassageTracker"/>
        /// </summary>
        public static readonly Dictionary<SlugcatStats.Name, bool[]> SlugcatFoodQuestAccessibility = [];

        // -- Cheat sheet for food quest access definitions --
        // SlimeMold, DangleFruit, BatFly, Mushroom, BlackLizard, WaterNut, JellyFish, JetFish, GlowWeed, Salamander, Snail,
        // Hazer, EggBug, LillyPuck, YellowLizard, GrappleWorm, Neuron, Centiwing, DandelionPeach, CyanLizard, GooieDuck, RedCenti

        // SeedCob, Centipede, VultureGrub, SmallNeedleWorm,
        // GreenLizard, BlueLizard, PinkLizard, WhiteLizard, RedLizard, SpitLizard, ZoopLizard, TrainLizard,
        // BigSpider, SpitterSpider, MotherSpider,
        // Vulture, KingVulture, MirosVulture,
        // LanternMouse, CicadaA, Yeek, DropBug, MirosBird, Scavenger, DaddyLongLegs,
        // PoleMimic, TentaclePlant, BigEel, Inspector

        /// <summary>
        /// The fallback starting den for each slugcat. 
        /// Most slugcats start in an intro scene instead of a den, so this defines the closest den to that starting point
        /// </summary>
        public static readonly Dictionary<SlugcatStats.Name, string> SlugcatDefaultStartingDen = [];

        /// <summary>
        /// The normal starting region for each slugcat
        /// </summary>
        public static readonly Dictionary<SlugcatStats.Name, string> SlugcatStartingRegion = [];

        // { GATE_NAME, IS_LEFT_TRAVEL }
        public static readonly Dictionary<string, bool> OneWayGates = new()
        {
            { "GATE_LF_SB", false },
        };

        /// <summary>
        /// These gates must always be open to avoid softlock scenarios
        /// </summary>
        public static readonly HashSet<string> ForceOpenGates =
        [
            "GATE_OE_SU", "GATE_SL_MS"
        ];

        /// <summary>
        /// Dict of readable names for various creatures / items
        /// </summary>
        public static readonly Dictionary<string, string> WikiNames = new()
        {
            { "LizardTemplate", "Lizards" },
            { "GreenLizard", "Green Lizard" },
            { "PinkLizard", "Pink Lizard" },
            { "BlueLizard", "Blue Lizard" },
            { "WhiteLizard", "White Lizard" },
            { "BlackLizard", "Black Lizard" },
            { "YellowLizard", "Yellow Lizard" },
            { "SpitLizard", "Caramel Lizard" },
            { "ZoopLizard", "Strawberry Lizard" },
            { "CyanLizard", "Cyan Lizard" },
            { "RedLizard", "Red Lizard" },
            { "Salamander", "Salamander" },
            { "EelLizard", "Eel Lizard" },
            { "TrainLizard", "Train Lizard" },
            { "Centipede", "Centipede" },
            { "SmallCentipede", "Infant Centipede" },
            { "Centiwing", "Centiwing" },
            { "AquaCenti", "Aquapede" },
            { "RedCentipede", "Red Centipede" },
            { "Spider", "Coalescipede" },
            { "BigSpider", "Big Spider" },
            { "MotherSpider", "Mother Spider" },
            { "SpitterSpider", "Spitter Spider" },
            { "VultureGrub", "Vulture Grub" },
            { "Vulture", "Vulture" },
            { "KingVulture", "King Vulture" },
            { "MirosBird", "Miros Bird" },
            { "MirosVulture", "Miros Vulture" },
            { "DaddyLongLegs", "Daddy Long Legs" },
            { "BrotherLongLegs", "Brother Long Legs" },
            { "TerrorLongLegs", "Mother Long Legs" },
            { "HunterDaddy", "Hunter Long Legs" },
            { "Inspector", "Inspector" },
            { "Leech", "Red Leech" },
            { "SeaLeech", "Blue Leech" },
            { "JungleLeech", "Jungle Leech" },
            { "Scavenger", "Scavenger" },
            { "ScavengerElite", "Elite Scavenger" },
            { "ScavengerKing", "Chieftain Scavenger" },
            { "TempleGuard", "Guardian" },
            { "Fly", "Batfly" },
            { "CicadaB", "Black Squidcada" },
            { "DropBug", "Dropwig" },
            { "EggBug", "Eggbug" },
            { "GarbageWorm", "Garbage Worm" },
            { "TubeWorm", "Grappling Worm" },
            { "Hazer", "Hazer" },
            { "JellyFish", "Jellyfish" },
            { "JetFish", "Jetfish" },
            { "LanternMouse", "Lantern Mouse" },
            { "BigEel", "Leviathan" },
            { "TentaclePlant", "Monster Kelp" },
            { "BigNeedleWorm", "Adult Noodlefly" },
            { "SmallNeedleWorm", "Infant Noodlefly" },
            { "Overseer", "Overseer" },
            { "PoleMimic", "Pole Plant" },
            { "Deer", "Rain Deer" },
            { "Slugcat", "Slugcat" },
            { "Snail", "Snail" },
            { "CicadaA", "White Squidcada" },
            { "FireBug", "Firebug" },
            { "StowawayBug", "Stowaway" },
            { "Yeek", "Yeek" },
            { "BigJelly", "Giant Jellyfish" },
            { "ReliableSpear", "Spear" },
            { "Spear", "Spear" },
            { "ExplosiveSpear", "Explosive Spear" },
            { "ElectricSpear", "Electric Spear" },
            { "FireSpear", "Fire Spear" },
            { "VultureMask", "Vulture Mask" },
            { "SSOracleSwarmer", "Neuron Fly" },
            { "ScavengerBomb", "Grenade" },
            { "SingularityBomb", "Singularity Bomb" },
            { "Rock", "Rock" },
            { "DangleFruit", "Blue Fruit" },
            { "SlimeMold", "Slime Mold" },
            { "GooieDuck", "Gooieduck" },
            { "LillyPuck", "Lilypuck" },
            { "EggBugEgg", "Eggbug Egg" },
            { "NeedleEgg", "Noodlefly Egg" },
            { "FlyLure", "Batnip" },
            { "BubbleGrass", "Bubble Weed" },
            { "SporePlant", "Beehive" },
            { "FirecrackerPlant", "Cherrybomb" },
            { "FlareBomb", "Flashbang" },
            { "PuffBall", "Spore Puff" },
            { "MoonCloak", "Cloak" },
            { "EnergyCell", "Rarefaction Cell" },
            { "JokeRifle", "Joke Rifle" },
            { "Mushroom", "Mushroom" },
            { "WaterNut", "Bubble Fruit" },
            { "GlowWeed", "Glow Weed" },
            { "DandelionPeach", "Dandelion Peach" },
            { "DeadHazer", "Hazer" },
            { "DeadVultureGrub", "VultureGrub" },
            { "SeedCob", "Popcorn Plant" },
        };

        public static void InitializeConstants()
        {
            CompatibleSlugcats.Clear();
            SlugcatFoodQuestAccessibility.Clear();
            SlugcatDefaultStartingDen.Clear();
            SlugcatStartingRegion.Clear();

            CompatibleSlugcats.AddRange(
            [
                SlugcatStats.Name.White,
                SlugcatStats.Name.Yellow,
                SlugcatStats.Name.Red,
            ]);

            SlugcatFoodQuestAccessibility.AddRange(new Dictionary<SlugcatStats.Name, bool[]>
            {
                { SlugcatStats.Name.White,
                [
                    true, true, true, true, false, true, true, false, true, false, false,
                    true, false, true, false, false, true, false, true, false, true, true,
                    true, true, true, true,
                    false, false, false, false, false, false, false, false,
                    false, false, false,
                    false, false, false,
                    false, false, false, false, false, false, false,
                    false, false, false, false,
                ]},
                { SlugcatStats.Name.Yellow,
                [
                    true, true, true, true, false, true, true, false, true, false, false,
                    true, false, true, false, false, true, false, true, false, true, true,
                    true, true, true, true,
                    false, false, false, false, false, false, false, false,
                    false, false, false,
                    false, false, false,
                    false, false, false, false, false, false, false,
                    false, false, false, false
                ]},
                { SlugcatStats.Name.Red,
                [
                    true, true, true, true, true, true, true, true, true, true, true,
                    true, true, true, true, true, true, true, true, true, true, true,
                    true, true, true, true,
                    true, true, true, true, true, true, true, true,
                    true, true, true,
                    true, true, true,
                    true, true, true, true, true, true, true,
                    false, false, false, false,
                ]},
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
                CompatibleSlugcats.AddRange(
                [
                    MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
                    MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                    MoreSlugcatsEnums.SlugcatStatsName.Rivulet,
                    MoreSlugcatsEnums.SlugcatStatsName.Spear,
                    MoreSlugcatsEnums.SlugcatStatsName.Saint
                ]);

                SlugcatFoodQuestAccessibility.AddRange(new Dictionary<SlugcatStats.Name, bool[]>
                {
                    { MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
                    [
                        true, true, true, true, true, true, true, true, true, true, true,
                        true, true, true, true, true, true, true, true, true, true, true,
                        true, true, true, true,
                        true, true, true, true, true, true, true, true,
                        true, true, true,
                        true, true, true,
                        true, true, true, true, true, true, true,
                        false, false, false, false,
                    ]},
                    { MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                    [
                        true, true, true, true, true, true, true, true, false, true, true,
                        true, true, true, true, true, true, true, true, true, true, true,
                        true, true, true, true,
                        true, true, true, true, true, true, true, true,
                        true, true, true,
                        true, true, true,
                        true, true, true, true, true, true, true,
                        false, false, false, false,
                    ]},
                    { MoreSlugcatsEnums.SlugcatStatsName.Spear,
                    [
                        false, false, true, true, true, false, false, true, false, true, true,
                        true, true, false, true, true, true, true, false, true, false, true,
                        true, true, true, true,
                        true, true, true, true, true, true, true, true,
                        true, true, true,
                        true, true, true,
                        true, true, true, true, true, true, true,
                        true, true, true, true,
                    ]},
                    { MoreSlugcatsEnums.SlugcatStatsName.Rivulet,
                    [
                        true, true, true, true, false, true, true, false, true, false, false,
                        true, false, true, false, false, true, false, true, false, true, true,
                        true, true, true, true,
                        false, false, false, false, false, false, false, false,
                        false, false, false,
                        false, false, false,
                        false, false, false, false, false, false, false,
                        false, false, false, false,
                    ]},
                    { MoreSlugcatsEnums.SlugcatStatsName.Saint,
                    [
                        true, true, false, true, false, true, false, false, true, false, false,
                        false, false, true, false, false, true, false, true, false, true, false,
                        false, false, false,
                        false, false, false, false,
                        false, false, false, false, false, false, false, false,
                        false, false, false,
                        false, false, false,
                        false, false, false, false, false, false, false,
                        false, false, false, false,
                    ]}
                });

                SlugcatDefaultStartingDen.AddRange(new Dictionary<SlugcatStats.Name, string>()
                {
                    { MoreSlugcatsEnums.SlugcatStatsName.Gourmand, "SH_S02" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Artificer, "GW_S09" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Rivulet, "DS_S02l" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Spear, "SU_S05" },
                    { MoreSlugcatsEnums.SlugcatStatsName.Saint, "SI_S04" }
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
