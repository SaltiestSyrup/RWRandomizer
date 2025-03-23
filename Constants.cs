﻿using MoreSlugcats;
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

        public static readonly Dictionary<string, string> WikiNames = new Dictionary<string, string>()
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
            { "CicadaB", "Squidcada" },
            { "DropBug", "Dropwig" },
            { "EggBug", "Eggbug" },
            { "GarbageWorm", "Garbage Worm" },
            { "TubeWorm", "Grappling Worm"},
            { "Hazer", "Hazer" },
            { "JellyFish", "Jellyfish" },
            { "JetFish", "Jetfish" },
            { "LanternMouse", "Lantern Mouse" },
            { "BigEel", "Leviathan" },
            { "TentaclePlant", "Monster Kelp" },
            { "BigNeedleWorm", "Adult Noodlefly" },
            { "SmallNeedleWorm", "Infant Noodlefly" },
            { "Overseer", "Overseer" },
            { "PoleMimic", "Pole Plant"},
            { "Deer", "Rain Deer" },
            { "Slugcat", "Slugcat" },
            { "Snail", "Snail" },
            { "CicadaA", "Squidcada" },
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
    }
}
