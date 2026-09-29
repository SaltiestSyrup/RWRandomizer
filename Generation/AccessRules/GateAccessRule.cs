namespace RainWorldRandomizer.Generation;

/// <summary>
/// Mainly used for special locations that can only be reached from a certain direction
/// </summary>
public class GateAccessRule : AccessRule
{
    public GateAccessRule(string gateName)
    {
        Type = AccessRuleType.Gate;
        ReqName = gateName;
    }

    public override bool IsMet(State state)
    {
        return state.Gates.Contains(ReqName);
    }

    public override string ToString()
    {
        return $"Gate {ReqName} is open";
    }
}