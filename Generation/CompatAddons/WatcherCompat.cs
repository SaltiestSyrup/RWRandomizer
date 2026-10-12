using System.Collections.Generic;
using Watcher;

namespace RainWorldRandomizer.Generation;

using static CustomLogicBuilder;
using WatcherSlugcats = WatcherEnums.SlugcatStatsName;

public class WatcherCompat : LogicAddon
{
    public WatcherCompat()
    {
        Constants.CompatibleSlugcats.Add(WatcherSlugcats.Watcher);

        Constants.SlugcatFoodQuestAccessibility.Add(WatcherSlugcats.Watcher,
        [
            true, true, true, true, false, true, true, false, true, false, false,
            true, false, true, false, false, true, false, true, false, true, true,
            true, true, true, true,
            false, false, false, false, false, false, false, false,
            false, false, false,
            false, false, false,
            false, false, false, false, false, false, false,
            false, false, false, false, false,
            true, true, true, true, true, true, true,
        ]);

        Constants.SlugcatDefaultStartingDen.Add(WatcherSlugcats.Watcher, "WSKB_C15");
        Constants.SlugcatStartingRegion.Add(WatcherSlugcats.Watcher, "WSKB");
    }

    public override void DefineLogic()
    {
        // Purge tutorial
        new List<string> { "SU", "HI", "CC", "SH" }.ForEach(reg =>
            AddBlacklistedRegion(reg, new RulePatch(null),
                SelectionMethod.Whitelist, WatcherSlugcats.Watcher));

        // Set bad warps
        new List<string> { "WSUR", "WDSR", "WHIR", "WGWR" }.ForEach(reg =>
            AddConnection(new ConnectionBlueprint($"BADWARP_{reg}",
                    [VanillaGenerator.START_REG, reg],
                    (AccessRuleConstants.CanDynamicWarp, AccessRule.Impossible())),
                SelectionMethod.Whitelist, WatcherSlugcats.Watcher));

        // WARA_P20 is the chasm room that needs float to cross rightwards
        AddSubregion(new SubregionBlueprint("WARA", "WARAWest",
            ["Flower-WARA_P03", "Token-Boomerang-WARA", "Shelter-WARA_S23"],
            ["Warp-WARA-WARC", "Warp-WARA-WARB", "Warp-WARA-WPTA"],
            [],
            (AccessRule.Empty(), new RippleAccessRule(5))));

        // WMPA_B01 needs max float to climb up
        AddSubregion(new SubregionBlueprint("WMPA", "WMPAWest",
            ["Shelter-WMPA_S01", "Warp-WMPA_A09"],
            ["Warp-WARF-WMPA"],
            ["WMPA_A07"],
            (new RippleAccessRule(9), AccessRule.Empty())));

        // Part of Outer Rim reached with the first warp entry
        AddSubregion(new SubregionBlueprint("WORA", "WORAWest",
            ["Shelter-WORA_S03", "Flower-WORA_AI"],
            [],
            ["WORA_DESERT9", "WORA_START"],
            // Entering the city needs more ripple to unlock a warp destination there
            (AccessRule.Empty(), new RippleAccessRule(5))));
        
        // Egg area needs float
        AddSubregion(new SubregionBlueprint("WORA", "WORAEgg",
            ["Token-Watcher-WORA", "Meet_Ripple_Elder", "Warp-WORA_EGG04"],
            ["Warp-WORA-WORA"],
            [],
            (new RippleAccessRule(5), new RippleAccessRule(5))));
        
        // Prince visits
        AddSubregion(new SubregionBlueprint("WORA", "WORAThrone1",
            ["Flower-WORA_THRONE10"],
            [],
            [],
            (new RippleAccessRule(3), AccessRule.Empty())));
        
        AddSubregion(new SubregionBlueprint("WORA", "WORAThrone2",
            ["Flower-WORA_THRONE05"],
            [],
            [],
            (new RippleAccessRule(5), AccessRule.Empty())));
        
        AddSubregion(new SubregionBlueprint("WORA", "WORAThrone3",
            ["Flower-WORA_THRONE07"],
            [],
            [],
            (new RippleAccessRule(7), AccessRule.Empty())));
        
        AddSubregion(new SubregionBlueprint("WORA", "WORAThrone4",
            ["Flower-WORA_THRONE09", "Shelter-THRONES01"],
            [],
            [],
            (new RippleAccessRule(9), AccessRule.Empty())));
    }
}