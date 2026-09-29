namespace RainWorldRandomizer.Generation;

/// <summary>
/// Shorthand rule for Echo accessibility. Directly translates into a Region rule representing where the Echo can be found
/// </summary>
public class EchoAccessRule : RegionAccessRule
{
    public EchoAccessRule(GhostWorldPresence.GhostID echoID)
    {
        Type = AccessRuleType.Echo;
        ReqName = echoID.value;
    }

    public override string ToString()
    {
        return $"Can find Echo {ReqName}";
    }
}