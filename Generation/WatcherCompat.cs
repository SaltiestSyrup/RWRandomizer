using System.Collections.Generic;
using Watcher;

namespace RainWorldRandomizer.Generation;
using static RainWorldRandomizer.Generation.CustomLogicBuilder;

public class WatcherCompat : LogicAddon
{
    public override void DefineLogic()
    {
        new List<string> {"SU", "HI", "CC", "SH"}.ForEach(reg => 
            AddBlacklistedRegion(reg, new RulePatch(null), 
                SelectionMethod.Whitelist, WatcherEnums.SlugcatStatsName.Watcher));
        
    }
}