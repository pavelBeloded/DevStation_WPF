using System.ComponentModel.DataAnnotations;

namespace DevStation.Data.Models;

public class Snippet
{
    public int Id { get; set; }

    public int CreatorId { get; set; }
    public User Creator { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Language { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public int ViewCount { get; set; } = 0;

    public SnippetStatus Status { get; set; } = SnippetStatus.Draft;
    public bool IsPublic { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserSnippet> UserSnippets { get; set; } = [];
    public ICollection<SnippetReview> Reviews { get; set; } = [];
}
