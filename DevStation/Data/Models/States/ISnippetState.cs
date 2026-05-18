namespace DevStation.Data.Models.States;

public interface ISnippetState
{
    void Submit(Snippet snippet);
    void Approve(Snippet snippet);
    void Reject(Snippet snippet);
}
