using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.Generation;

/// <summary>
/// Determines if a given PlacedObject can be found in the current state. Useful for Passages and Food Quest
/// </summary>
public class ObjectAccessRule : AccessRule
{
    private readonly AbstractPhysicalObject.AbstractObjectType item;

    public ObjectAccessRule(AbstractPhysicalObject.AbstractObjectType item)
    {
        Type = AccessRuleType.Object;
        ReqName = item.value;
        this.item = item;
    }

    public override bool IsMet(State state)
    {
        return state.Objects.Contains(item);
    }

    public override bool IsPossible(State state)
    {
        // Costly calculation, hopefully fine since this shouldn't be checked after init
        return state.AllRegions
            .Any(r =>
            {
                string regLower = r.ID.ToLowerInvariant();
                if (!TokenCachePatcher.regionObjects.TryGetValue(regLower,
                        out List<AbstractPhysicalObject.AbstractObjectType> objList)) return false;
                int index = objList.IndexOf(item);
                if (index < 0) return false;
                return TokenCachePatcher.regionObjectsAccessibility[regLower][index].Contains(state.Slugcat);
            });
    }

    public override string ToString()
    {
        return $"Can find {ReqName}";
    }
}