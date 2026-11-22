using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TaskManager;
using Xunit;

namespace TaskManager.Tests;

public sealed class WorkflowTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");
    private readonly BoardDb db;
    private readonly BoardWorkflow workflow;
    private readonly CancellationToken ct = CancellationToken.None;

    public WorkflowTests()
    {
        connection.Open();
        db = new BoardDb(new DbContextOptionsBuilder<BoardDb>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        workflow = new(db, TimeProvider.System);
    }

    [Fact]
    public async Task Changes_survive_a_fresh_read_and_history_cascades_on_delete()
    {
        var controller = new TasksController(db, workflow);
        var created = await controller.Create(new(" Release notes "), ct);
        var result = Assert.IsType<CreatedAtActionResult>(created.Result);
        var task = Assert.IsType<TaskView>(result.Value);
        Assert.Equal(201, result.StatusCode);
        await workflow.SetState(task.Id, Stage.DONE, ct);
        await workflow.SetState(task.Id, Stage.TODO, ct);
        await workflow.SetState(task.Id, Stage.TODO, ct);
        await workflow.Describe(task.Id, "Publish notes", ct);
        db.ChangeTracker.Clear();
        var current = await controller.Get(task.Id, ct);
        Assert.Equal("Publish notes", current.Description);
        Assert.Equal(Stage.TODO, current.State);
        Assert.Equal(4, (await controller.History(task.Id, new(), ct)).Count);
        Assert.IsType<NoContentResult>(await controller.Delete(task.Id, ct));
        await Assert.ThrowsAsync<Missing>(() => controller.Get(task.Id, ct));
        Assert.Empty(await db.History.ToListAsync());
    }

    [Fact]
    public async Task Assignment_controls_report_membership_while_any_existing_user_can_comment()
    {
        var task = await workflow.Create(new("Review API"), ct);
        var member = new Member { Name = "Reviewer" };
        db.Users.Add(member);
        await db.SaveChangesAsync(ct);
        var comment = await workflow.Comment(task.Id, new(member.Id, "Ready"), ct);
        var reports = new AssignmentsController(db, workflow);
        Assert.Empty(await reports.AssignedComments(new(), ct));
        await workflow.Assign(task.Id, member.Id, ct);
        await Assert.ThrowsAsync<Conflict>(() => workflow.Assign(task.Id, member.Id, ct));
        Assert.Equal(comment, Assert.Single(await reports.AssignedComments(new(), ct)));
        await workflow.Unassign(task.Id, member.Id, ct);
        Assert.Empty(await reports.AssignedComments(new(), ct));
        await workflow.DeleteComment(comment.Id, ct);
        Assert.Empty(await db.Comments.ToListAsync());
        Assert.Equal(2, await db.History.CountAsync(h => h.Event.StartsWith("Comment") && h.SubjectId == comment.Id));
        await Assert.ThrowsAsync<Missing>(() => workflow.Comment(task.Id, new(member.Id + 1, "Missing"), ct));
    }

    [Fact]
    public void Pagination_rejects_out_of_range_limits()
    {
        var page = new Page { Limit = 101 };
        Assert.False(Validator.TryValidateObject(page, new ValidationContext(page), new List<ValidationResult>(), true));
    }

    public void Dispose()
    {
        db.Dispose();
        connection.Dispose();
    }
}
