namespace DevStation.Data.Models.States;

public static class SnippetStateFactory
{
    public static ISnippetState GetState(SnippetStatus status) => status switch
    {
        SnippetStatus.Draft         => new DraftState(),
        SnippetStatus.PendingReview => new PendingReviewState(),
        SnippetStatus.Published     => new PublishedState(),
        SnippetStatus.Rejected      => new RejectedState(),
        _ => throw new ArgumentOutOfRangeException(nameof(status), $"Unknown status: {status}")
    };
}
