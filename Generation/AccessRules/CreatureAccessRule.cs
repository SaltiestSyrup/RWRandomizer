using System.Collections.Generic;
using System.Linq;

namespace RainWorldRandomizer.Generation;

/// <summary>
/// Determines if a given creature can be found in current state. Useful for Passages and Food Quest
/// </summary>
public class CreatureAccessRule : AccessRule
{
    private readonly CreatureTemplate.Type creature;

    public CreatureAccessRule(CreatureTemplate.Type creature)
    {
        Type = AccessRuleType.Creature;
        ReqName = creature.value;
        this.creature = creature;
    }

    public override bool IsMet(State state)
    {
        return state.Creatures.Contains(creature);
    }

    public override bool IsPossible(State state)
    {
        // Costly calculation, hopefully fine since this shouldn't be checked after init
        return state.AllRegions
            .Any(r =>
            {
                string regLower = r.ID.ToLowerInvariant();
                if (!TokenCachePatcher.regionCreatures.TryGetValue(regLower,
                        out List<CreatureTemplate.Type> critList)) return false;
                int index = critList.IndexOf(creature);
                if (index < 0) return false;
                return TokenCachePatcher.regionCreaturesAccessibility[regLower][index].Contains(state.Slugcat);
            });
    }

    public override string ToString()
    {
        return $"Can find {ReqName}";
    }
}