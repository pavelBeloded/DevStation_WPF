using DevStation.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DevStation.Data.DbContext;

public class DevStationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Snippet> Snippets { get; set; }
    public DbSet<UserSnippet> UserSnippets { get; set; }
    public DbSet<Folder> Folders { get; set; }
    public DbSet<SnippetReview> SnippetReviews { get; set; }
    public DbSet<MdnCache> MdnCache { get; set; }

    public DevStationDbContext(DbContextOptions<DevStationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Snippet>()
            .HasOne(s => s.Creator)
            .WithMany(u => u.CreatedSnippets)
            .HasForeignKey(s => s.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserSnippet>()
            .HasKey(us => new { us.UserId, us.SnippetId });

        modelBuilder.Entity<UserSnippet>()
            .HasOne(us => us.User)
            .WithMany(u => u.UserSnippets)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSnippet>()
            .HasOne(us => us.Snippet)
            .WithMany(s => s.UserSnippets)
            .HasForeignKey(us => us.SnippetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSnippet>()
            .HasOne(us => us.Folder)
            .WithMany(f => f.UserSnippets)
            .HasForeignKey(us => us.FolderId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        modelBuilder.Entity<Folder>()
            .HasOne(f => f.ParentFolder)
            .WithMany(f => f.ChildFolders)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Folder>()
            .HasOne(f => f.User)
            .WithMany(u => u.Folders)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SnippetReview>()
            .HasOne(sr => sr.Snippet)
            .WithMany(s => s.Reviews)
            .HasForeignKey(sr => sr.SnippetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SnippetReview>()
            .HasOne(sr => sr.Reviewer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(sr => sr.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MdnCache>()
            .HasIndex(m => m.SearchTerm);
    }
}
