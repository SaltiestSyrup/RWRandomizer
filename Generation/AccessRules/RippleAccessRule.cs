namespace RainWorldRandomizer.Generation;

public class RippleAccessRule : AccessRule
{
    private readonly int reqAmount;

    public RippleAccessRule(int amount)
    {
        Type = AccessRuleType.Karma;
        ReqName = "Karma";
        reqAmount = amount;
    }

    public override bool IsMet(State state)
    {
        return state.MaxRipple >= reqAmount;
    }

    public override bool IsPossible(State state)
    {
        return reqAmount is > 0 and <= 9;
    }

    public override string ToString()
    {
        return $"Has {reqAmount} Ripple";
    }
}