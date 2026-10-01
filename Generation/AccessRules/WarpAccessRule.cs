namespace RainWorldRandomizer.Generation;

public class WarpAccessRule : AccessRule
{
    private bool isRippleWarp;
    
    public WarpAccessRule(string warpName, bool ripple)
    {
        Type = AccessRuleType.Gate;
        ReqName = warpName;
        isRippleWarp = ripple;
    }

    public override bool IsMet(State state)
    {
        return state.Gates.Contains(ReqName) && (!isRippleWarp || state.MaxRipple >= 9);
    }

    public override string ToString()
    {
        return $"Warp {ReqName} is open";
    }
}