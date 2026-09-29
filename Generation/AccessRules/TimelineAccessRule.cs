namespace RainWorldRandomizer.Generation;

/// <summary>
/// Determines if a location can ever be reached at a spot in the timeline
/// </summary>
public class TimelineAccessRule : AccessRule
{
    public enum TimelineOperation
    {
        At,
        AtOrBefore,
        AtOrAfter,
    }

    private readonly TimelineOperation operation;
    private readonly SlugcatStats.Timeline timeline;

    public TimelineAccessRule(SlugcatStats.Timeline timeline, TimelineOperation operation)
    {
        this.timeline = timeline;
        this.operation = operation;
        ReqName = timeline.value;
    }

    public override bool IsMet(State state) => IsPossible(state);

    public override bool IsPossible(State state)
    {
        return operation switch
        {
            TimelineOperation.At => state.Timeline == timeline,
            TimelineOperation.AtOrBefore => SlugcatStats.AtOrBeforeTimeline(state.Timeline, timeline),
            TimelineOperation.AtOrAfter => SlugcatStats.AtOrAfterTimeline(state.Timeline, timeline),
            _ => false,
        };
    }

    public override string ToString()
    {
        return operation switch
        {
            TimelineOperation.At => $"Playing at timeline: {ReqName}",
            TimelineOperation.AtOrBefore => $"Playing at or before timeline: {ReqName}",
            TimelineOperation.AtOrAfter => $"Playing at or after timeline: {ReqName}",
            _ => $"Unknown timeline operation: {ReqName}",
        };
    }
}