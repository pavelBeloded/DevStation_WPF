namespace DevStation.Data.Models.States;

public class RejectedState : ISnippetState
{
    public void Submit(Snippet snippet)
    {
        snippet.Status = SnippetStatus.PendingReview;
        snippet.UpdatedAt = DateTime.UtcNow;
    }

    public void Approve(Snippet snippet) =>
        throw new InvalidOperationException("Cannot approve a rejected snippet. Resubmit it first.");

    public void Reject(Snippet snippet) =>
        throw new InvalidOperationException("Snippet is already rejected.");
}
