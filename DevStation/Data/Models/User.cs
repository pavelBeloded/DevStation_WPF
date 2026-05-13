using System.ComponentModel.DataAnnotations;

namespace DevStation.Data.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    [MaxLength(100)]
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Snippet> CreatedSnippets { get; set; } = [];
    public ICollection<UserSnippet> UserSnippets { get; set; } = [];
    public ICollection<Folder> Folders { get; set; } = [];
    public ICollection<SnippetReview> Reviews { get; set; } = [];
}
