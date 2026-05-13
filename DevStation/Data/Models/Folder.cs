using System.ComponentModel.DataAnnotations;

namespace DevStation.Data.Models;

public class Folder
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public ICollection<Folder> ChildFolders { get; set; } = [];

    [MaxLength(7)]
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserSnippet> UserSnippets { get; set; } = [];
}
