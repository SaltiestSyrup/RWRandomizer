namespace RainWorldRandomizer.Generation;

/// <summary>
/// Used to set a karma requirement, for various flag checks
/// </summary>
public class KarmaAccessRule : AccessRule
{
    private readonly int reqAmount;

    public KarmaAccessRule(int amount)
    {
        Type = AccessRuleType.Karma;
        ReqName = "Karma";
        reqAmount = amount;
    }

    public override bool IsMet(State state)
    {
        return state.MaxKarma >= reqAmount;
    }

    public override bool IsPossible(State state)
    {
        return reqAmount is > 0 and <= 10;
    }

    public override string ToString()
    {
        return $"Has {reqAmount} Karma";
    }
}