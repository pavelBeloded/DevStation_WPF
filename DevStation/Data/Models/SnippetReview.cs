using System.ComponentModel.DataAnnotations;

namespace DevStation.Data.Models;

public class SnippetReview
{
    public int Id { get; set; }

    public int SnippetId { get; set; }
    public Snippet Snippet { get; set; } = null!;

    public int ReviewerId { get; set; }
    public User Reviewer { get; set; } = null!;

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    [MaxLength(1000)]
    public string? ReviewComment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
