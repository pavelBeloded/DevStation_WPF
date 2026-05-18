namespace DevStation.Data.Models;

public class SnippetBuilder
{
    private readonly Snippet _snippet = new() { CreatedAt = DateTime.UtcNow };

    public SnippetBuilder WithCreator(int creatorId)       { _snippet.CreatorId = creatorId;    return this; }
    public SnippetBuilder WithTitle(string title)          { _snippet.Title = title;             return this; }
    public SnippetBuilder WithCode(string code)            { _snippet.Code = code;               return this; }
    public SnippetBuilder WithLanguage(string? language)   { _snippet.Language = language;       return this; }
    public SnippetBuilder WithDescription(string? desc)    { _snippet.Description = desc;        return this; }
    public SnippetBuilder WithTags(string? tags)           { _snippet.Tags = tags;               return this; }

    public Snippet Build()
    {
        if (string.IsNullOrWhiteSpace(_snippet.Title))
            throw new InvalidOperationException("Snippet title is required.");
        if (string.IsNullOrWhiteSpace(_snippet.Code))
            throw new InvalidOperationException("Snippet code is required.");
        return _snippet;
    }
}
