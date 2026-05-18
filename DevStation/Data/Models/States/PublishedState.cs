namespace DevStation.Data.Models.States;

public class PublishedState : ISnippetState
{
    public void Submit(Snippet snippet) =>
        throw new InvalidOperationException("Snippet is already published.");

    public void Approve(Snippet snippet) =>
        throw new InvalidOperationException("Snippet is already published.");

    public void Reject(Snippet snippet)
    {
        snippet.Status = SnippetStatus.Rejected;
        snippet.IsPublic = false;
        snippet.UpdatedAt = DateTime.UtcNow;
    }
}
