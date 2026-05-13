using System.ComponentModel.DataAnnotations;

namespace DevStation.Data.Models;

public class MdnCache
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string SearchTerm { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Summary { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; }

    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
