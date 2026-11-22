using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace TaskManager;

public sealed record CreateTask([Required, StringLength(1000)] string Description, Stage State = Stage.TODO);
public sealed record ChangeState([Required] Stage? State);
public sealed record ChangeDescription([Required, StringLength(1000)] string Description);
public sealed record UserInput([Required, StringLength(120)] string Name);
public sealed record AddComment([Range(1, long.MaxValue)] long UserId, [Required, StringLength(4000)] string Comment);

public sealed class Page
{
    [Range(0, int.MaxValue)] public int Offset { get; set; }
    [Range(1, 100)] public int Limit { get; set; } = 50;
    public IQueryable<T> Apply<T>(IOrderedQueryable<T> query) => query.Skip(Offset).Take(Limit);
}

public sealed record TaskView(long Id, string Description, Stage State, DateTime CreatedUtc, DateTime ModifiedUtc)
{
    public static readonly Expression<Func<WorkItem, TaskView>> Projection =
        t => new TaskView(t.Id, t.Description, t.State, t.CreatedUtc, t.ModifiedUtc);
    public static readonly Func<WorkItem, TaskView> From = Projection.Compile();
}
public sealed record UserView(long Id, string Name);
public sealed record CommentView(long Id, long UserId, long TaskId, string Comment)
{
    public static readonly Expression<Func<Note, CommentView>> Projection =
        n => new CommentView(n.Id, n.UserId, n.TaskId, n.Text);
}
public sealed record AssignmentView(long TaskId, string Description, long UserId, string Name);
public sealed record HistoryView(long Id, long TaskId, string Event, long? SubjectId, DateTime AtUtc);
