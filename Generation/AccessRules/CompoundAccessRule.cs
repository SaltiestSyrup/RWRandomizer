using System.Linq;

namespace RainWorldRandomizer.Generation;

/// <summary>
/// Rule used for any location with requirements more complex than a single state check. 
/// Most notably applies to Passages and Story locations.
/// These can be chained together to create arbitrarily complex rules for any situation.
/// </summary>
/// <param name="rules">Array of rules that this rule will reference</param>
/// <param name="operation">The type of operation used to determine if given rules are met</param>
/// <param name="valAmount">Optional value utilized by some operations</param>
public class CompoundAccessRule(
    AccessRule[] rules,
    CompoundAccessRule.CompoundOperation operation,
    int valAmount = 0) : AccessRule
{
    public enum CompoundOperation
    {
        /// <summary>
        /// All the rules must be satisfied
        /// </summary>
        All,

        /// <summary>
        /// At least one of the rules must be satisfied
        /// </summary>
        Any,

        /// <summary>
        /// At least <see cref="valAmount"/> of the rules must be satisfied
        /// </summary>
        AtLeast
    }

    protected CompoundOperation operation = operation;
    protected AccessRule[] accessRules = rules;
    protected int valAmount = valAmount;

    public override bool IsMet(State state)
    {
        return operation switch
        {
            CompoundOperation.All => accessRules.All(r => r.IsMet(state)),
            CompoundOperation.Any => accessRules.Any(r => r.IsMet(state)),
            CompoundOperation.AtLeast => accessRules.Sum(r => r.IsMet(state) ? 1 : 0) >= valAmount,
            _ => false,
        };
    }

    public override bool IsPossible(State state)
    {
        return operation switch
        {
            CompoundOperation.All => accessRules.All(r => r.IsPossible(state)),
            CompoundOperation.Any => accessRules.Any(r => r.IsPossible(state)),
            CompoundOperation.AtLeast => accessRules.Sum(r => r.IsPossible(state) ? 1 : 0) >=
                                         valAmount,
            _ => false,
        };
    }

    public override string ToString()
    {
        string separator = operation switch
        {
            CompoundOperation.All => " AND ",
            CompoundOperation.Any => " OR ",
            CompoundOperation.AtLeast or _ => ", ",
        };
        string joinedRules = string.Join(separator, accessRules.Select(r => r.ToString()));
        return operation switch
        {
            CompoundOperation.All => $"({joinedRules})",
            CompoundOperation.Any => $"({joinedRules})",
            CompoundOperation.AtLeast => $"At least {valAmount} of: ({joinedRules})",
            _ => $"Invalid compound operation containing: ({joinedRules})",
        };
    }
}