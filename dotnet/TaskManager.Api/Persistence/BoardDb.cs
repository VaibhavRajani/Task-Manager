using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TaskManager;

public enum Stage { TODO, DOING, DONE }

public sealed class WorkItem
{
    public long Id { get; set; }
    [MaxLength(1000)] public required string Description { get; set; }
    public Stage State { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; }
}

public sealed class Member
{
    public long Id { get; set; }
    [MaxLength(120)] public required string Name { get; set; }
}

public sealed class Note
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public long UserId { get; set; }
    [MaxLength(4000)] public required string Text { get; set; }
}

public sealed class Assignment
{
    public long TaskId { get; set; }
    public long UserId { get; set; }
    public WorkItem Task { get; set; } = null!;
    public Member User { get; set; } = null!;
}

public sealed class HistoryEntry
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public WorkItem Task { get; set; } = null!;
    public required string Event { get; set; }
    public long? SubjectId { get; set; }
    public DateTime AtUtc { get; set; }
}

public sealed class BoardDb(DbContextOptions<BoardDb> options) : DbContext(options)
{
    public DbSet<WorkItem> Tasks => Set<WorkItem>();
    public DbSet<Member> Users => Set<Member>();
    public DbSet<Note> Comments => Set<Note>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<HistoryEntry> History => Set<HistoryEntry>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<WorkItem>().Property(t => t.State).HasConversion<string>().HasMaxLength(8);
        model.Entity<Member>().HasIndex(u => u.Name);
        model.Entity<Assignment>().HasKey(a => new { a.TaskId, a.UserId });
        model.Entity<Assignment>().HasOne(a => a.Task).WithMany().HasForeignKey(a => a.TaskId);
        model.Entity<Assignment>().HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId);
        model.Entity<Note>().HasOne<WorkItem>().WithMany().HasForeignKey(n => n.TaskId);
        model.Entity<Note>().HasOne<Member>().WithMany().HasForeignKey(n => n.UserId);
        model.Entity<HistoryEntry>().HasOne(h => h.Task).WithMany().HasForeignKey(h => h.TaskId);
    }
}
