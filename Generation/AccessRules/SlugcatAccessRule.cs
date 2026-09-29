namespace RainWorldRandomizer.Generation;

/// <summary>
/// Determines if a location can ever be reached for a given slugcat
/// </summary>
public class SlugcatAccessRule : AccessRule
{
    private readonly SlugcatStats.Name slugcat;
    public bool inverted;

    /// <param name="invert">If true will pass if chosen is not this slugcat</param>
    public SlugcatAccessRule(SlugcatStats.Name slugcat, bool invert = false)
    {
        this.slugcat = slugcat;
        ReqName = slugcat.value;
        inverted = invert;
    }

    public override bool IsMet(State state) => IsPossible(state);

    public override bool IsPossible(State state)
    {
        return inverted ? state.Slugcat != slugcat : state.Slugcat == slugcat;
    }

    public override string ToString()
    {
        return $"{(inverted ? "Not p" : "P")}laying as {ReqName}";
    }
}