using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskManager;

[ApiController, Route("api")]
public sealed class AssignmentsController(BoardDb db, BoardWorkflow workflow) : ControllerBase
{
    [HttpPost("tasks/{taskId:long}/assignees/{userId:long}")]
    public async Task<ActionResult<AssignmentView>> Assign(long taskId, long userId, CancellationToken ct)
    {
        var assignment = await workflow.Assign(taskId, userId, ct);
        return StatusCode(StatusCodes.Status201Created, assignment);
    }

    [HttpDelete("tasks/{taskId:long}/assignees/{userId:long}")]
    public async Task<IActionResult> Unassign(long taskId, long userId, CancellationToken ct)
    {
        await workflow.Unassign(taskId, userId, ct);
        return NoContent();
    }

    [HttpGet("assignments")]
    public Task<List<AssignmentView>> List([FromQuery] Page page, CancellationToken ct) =>
        Read(db.Assignments, page, ct);

    [HttpGet("tasks/{id:long}/assignments")]
    public async Task<List<AssignmentView>> ByTask(long id, [FromQuery] Page page, CancellationToken ct)
    {
        await workflow.RequireTask(id, ct);
        return await Read(db.Assignments.Where(a => a.TaskId == id), page, ct);
    }

    [HttpGet("users/{id:long}/assignments")]
    public async Task<List<AssignmentView>> ByUser(long id, [FromQuery] Page page, CancellationToken ct)
    {
        await workflow.RequireUser(id, ct);
        return await Read(db.Assignments.Where(a => a.UserId == id), page, ct);
    }

    private static Task<List<AssignmentView>> Read(IQueryable<Assignment> assignments, Page page, CancellationToken ct) =>
        page.Apply(assignments.OrderBy(a => a.TaskId).ThenBy(a => a.UserId))
            .Select(a => new AssignmentView(a.TaskId, a.Task.Description, a.UserId, a.User.Name)).ToListAsync(ct);

    [HttpGet("assignments/comments")]
    public Task<List<CommentView>> AssignedComments([FromQuery] Page page, CancellationToken ct)
    {
        var comments = db.Comments.Where(n => db.Assignments.Any(a => a.UserId == n.UserId && a.TaskId == n.TaskId));
        return page.Apply(comments.OrderBy(n => n.Id)).Select(CommentView.Projection).ToListAsync(ct);
    }
}
