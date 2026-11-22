using Microsoft.EntityFrameworkCore;

namespace TaskManager;

public sealed class BoardWorkflow(BoardDb db, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private async Task<WorkItem> FindTask(long id, CancellationToken ct) =>
        await db.Tasks.FindAsync([id], ct) ?? throw new Missing("Task");

    public async Task RequireUser(long id, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id, ct)) throw new Missing("User");
    }

    public async Task RequireTask(long id, CancellationToken ct)
    {
        if (!await db.Tasks.AnyAsync(t => t.Id == id, ct)) throw new Missing("Task");
    }

    private void Record(WorkItem task, string message, long? subjectId = null)
    {
        db.History.Add(new HistoryEntry { Task = task, Event = message, SubjectId = subjectId, AtUtc = Now });
    }

    public async Task<TaskView> Create(CreateTask input, CancellationToken ct)
    {
        var now = Now;
        var task = new WorkItem { Description = input.Description.Trim(), State = input.State,
            CreatedUtc = now, ModifiedUtc = now, Revision = Guid.NewGuid() };
        db.Tasks.Add(task);
        Record(task, "Task created");
        await db.SaveChangesAsync(ct);
        return TaskView.From(task);
    }

    public async Task<TaskView> SetState(long id, Stage state, CancellationToken ct)
    {
        var task = await FindTask(id, ct);
        if (task.State == state) return TaskView.From(task);
        Record(task, $"State changed from {task.State} to {state}");
        task.State = state;
        return await SaveEdit(task, ct);
    }

    public async Task<TaskView> Describe(long id, string description, CancellationToken ct)
    {
        var task = await FindTask(id, ct);
        var value = description.Trim();
        if (task.Description == value) return TaskView.From(task);
        task.Description = value;
        Record(task, "Description updated");
        return await SaveEdit(task, ct);
    }

    private async Task<TaskView> SaveEdit(WorkItem task, CancellationToken ct)
    {
        task.ModifiedUtc = Now;
        task.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        return TaskView.From(task);
    }

    public async Task<CommentView> Comment(long id, AddComment input, CancellationToken ct)
    {
        var task = await FindTask(id, ct);
        await RequireUser(input.UserId, ct);
        var note = new Note { TaskId = id, UserId = input.UserId, Text = input.Comment.Trim() };
        // The history subject needs the database-generated comment ID.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Comments.Add(note);
        await db.SaveChangesAsync(ct);
        Record(task, "Comment added", note.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(note.Id, note.UserId, id, note.Text);
    }

    public async Task DeleteComment(long id, CancellationToken ct)
    {
        var note = await db.Comments.FindAsync([id], ct) ?? throw new Missing("Comment");
        Record(await FindTask(note.TaskId, ct), "Comment deleted", id);
        db.Comments.Remove(note);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AssignmentView> Assign(long taskId, long userId, CancellationToken ct)
    {
        var task = await FindTask(taskId, ct);
        var user = await db.Users.FindAsync([userId], ct) ?? throw new Missing("User");
        if (await db.Assignments.AnyAsync(a => a.TaskId == taskId && a.UserId == userId, ct))
            throw new Conflict("The user is already assigned to this task.");
        db.Assignments.Add(new Assignment { Task = task, User = user });
        Record(task, "User assigned", userId);
        await db.SaveChangesAsync(ct);
        return new(taskId, task.Description, userId, user.Name);
    }

    public async Task Unassign(long taskId, long userId, CancellationToken ct)
    {
        var assignment = await db.Assignments.FindAsync([taskId, userId], ct) ?? throw new Missing("Assignment");
        db.Assignments.Remove(assignment);
        Record(await FindTask(taskId, ct), "User unassigned", userId);
        await db.SaveChangesAsync(ct);
    }
}
