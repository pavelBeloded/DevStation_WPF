namespace DevStation.Data.Models;

public class UserSnippet
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SnippetId { get; set; }
    public Snippet Snippet { get; set; } = null!;

    public int? FolderId { get; set; }
    public Folder? Folder { get; set; }

    public bool IsFavorite { get; set; } = false;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
