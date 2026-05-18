namespace DevStation.Data.Models.States;

public class PendingReviewState : ISnippetState
{
    public void Submit(Snippet snippet) =>
        throw new InvalidOperationException("Snippet is already submitted for review.");

    public void Approve(Snippet snippet)
    {
        snippet.Status = SnippetStatus.Published;
        snippet.IsPublic = true;
        snippet.UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(Snippet snippet)
    {
        snippet.Status = SnippetStatus.Rejected;
        snippet.UpdatedAt = DateTime.UtcNow;
    }
}
