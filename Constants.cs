using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public static class Constants
    {
        /// <summary>
        /// Describes which food quest items can be eaten by each slugcat. Matches order of <see cref="WinState.GourmandPassageTracker"/>
        /// </summary>
        public static readonly Dictionary<SlugcatStats.Name, bool[]> slugcatFoodQuestAccessibility = new Dictionary<SlugcatStats.Name, bool[]>
        {
            // SlimeMold, DangleFruit, BatFly, Mushroom, BlackLizard, WaterNut, JellyFish, JetFish, GlowWeed, Salamander, Snail,
            // Hazer, EggBug, LillyPuck, YellowLizard, GrappleWorm, Neuron, Centiwing, DandelionPeach, CyanLizard, GooieDuck, RedCenti
            { SlugcatStats.Name.White, new bool[] { 
                true, true, true, true, false, true, true, false, true, false, false, 
                true, false, true, false, false, true, false, true, false, true, true
            }},
            { SlugcatStats.Name.Yellow, new bool[] {
                true, true, true, true, false, true, true, false, true, false, false,
                true, false, true, false, false, true, false, true, false, true, true
            }},
            { SlugcatStats.Name.Red, new bool[] {
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true, true, true, true, true, true, true, true
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Gourmand, new bool[] {
                true, true, true, true, true, true, true, true, true, true, true,
                true, true, true, true, true, true, true, true, true, true, true
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Artificer, new bool[] {
                true, true, true, true, true, true, true, true, false, true, true,
                true, true, true, true, true, true, true, true, true, true, true
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Spear, new bool[] {
                false, false, true, true, true, false, false, true, false, true, true,
                true, true, false, true, true, true, true, false, true, false, true
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Rivulet, new bool[] {
                true, true, true, true, false, true, true, false, true, false, false,
                true, false, true, false, false, true, false, true, false, true, true
            }},
            { MoreSlugcatsEnums.SlugcatStatsName.Saint, new bool[] {
                true, true, false, true, false, true, false, false, true, false, false,
                false, false, true, false, false, true, false, true, false, true, false
            }},
        };

    }
}
