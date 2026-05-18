namespace DevStation.Data.Models.States;

public class DraftState : ISnippetState
{
    public void Submit(Snippet snippet)
    {
        snippet.Status = SnippetStatus.PendingReview;
        snippet.UpdatedAt = DateTime.UtcNow;
    }

    public void Approve(Snippet snippet) =>
        throw new InvalidOperationException("Cannot approve a draft. Submit it for review first.");

    public void Reject(Snippet snippet) =>
        throw new InvalidOperationException("Cannot reject a draft. Submit it for review first.");
}
