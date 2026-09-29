using System.Linq;

namespace RainWorldRandomizer.Generation;

/// <summary>
/// Shorthand for a rule allowing any of the given slugcats to be used
/// </summary>
/// <param name="invert">If true, will instead pass if slugcat is none of those listed</param>
public class MultiSlugcatAccessRule(SlugcatStats.Name[] slugcats, bool invert = false)
    : CompoundAccessRule([.. slugcats.Select(scug => new SlugcatAccessRule(scug, invert))],
        invert ? CompoundOperation.All : CompoundOperation.Any);