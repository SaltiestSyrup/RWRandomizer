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
    }
}