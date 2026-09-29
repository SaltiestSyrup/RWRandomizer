using System;
using System.Reflection;

namespace RainWorldRandomizer.Generation;

/// <summary>
/// Determines if a location can ever be reached under current options.
/// </summary>
public class OptionAccessRule : AccessRule
{
    private readonly FieldInfo optionField;
    private readonly bool inverted;

    /// <summary>
    /// Set location possibility based on if <paramref name="optionName"/> is enabled.
    /// </summary>
    /// <param name="optionName">The exact name of a field in <see cref="OptionStruct"/></param>
    /// <param name="inverted">If true, will instead check if option is disabled</param>
    /// <exception cref="ArgumentException">if <paramref name="optionName"/> is not a
    /// <see cref="bool"/> property in <see cref="OptionStruct"/></exception>
    public OptionAccessRule(string optionName, bool inverted = false)
    {
        ReqName = $"Option-{optionName}";
        this.inverted = inverted;

        optionField = typeof(OptionStruct).GetField(optionName);

        if (optionField == null || optionField.FieldType != typeof(bool))
        {
            throw new ArgumentException(
                "Given option does not exist in Options class or does not refer to a boolean", optionName);
        }
    }

    public override bool IsMet(State state) => IsPossible(state);

    public override bool IsPossible(State state)
    {
        // This should always succeed due to check in constructor, but catch exception just in case
        try
        {
            bool optionSet = (bool)optionField.GetValue(state.options);
            return inverted ? !optionSet : optionSet;
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"OptionAccessRule tried to fetch invalid Option: {ReqName}");
            Plugin.Log.LogError(e);
            return false;
        }
    }

    public override string ToString()
    {
        return $"{ReqName} is set to {!inverted}";
    }
}