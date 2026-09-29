using System.Linq;

namespace RainWorldRandomizer.Generation;

/// <summary>
/// The most common rule, used to determine the in game region a location can be found in
/// </summary>
public class RegionAccessRule : AccessRule
{
    public RegionAccessRule(string regionShort = null)
    {
        Type = AccessRuleType.Region;
        ReqName = regionShort;
    }

    public override bool IsMet(State state)
    {
        return ReqName is not null && state.HasRegion(ReqName);
    }

    public override bool IsPossible(State state)
    {
        return state.AllRegions.Any(r => r.ID == ReqName);
    }

    public override string ToString()
    {
        return $"Can enter {ReqName}";
    }
}