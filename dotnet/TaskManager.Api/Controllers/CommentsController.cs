using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskManager;

[ApiController, Route("api")]
public sealed class CommentsController(BoardDb db, BoardWorkflow workflow) : ControllerBase
{
    [HttpPost("tasks/{id:long}/comments")]
    public async Task<ActionResult<CommentView>> Create(long id, AddComment input, CancellationToken ct)
    {
        var comment = await workflow.Comment(id, input, ct);
        return CreatedAtAction(nameof(Get), new { id = comment.Id }, comment);
    }

    [HttpGet("comments/{id:long}")]
    public async Task<CommentView> Get(long id, CancellationToken ct) =>
        await db.Comments.Where(n => n.Id == id).Select(CommentView.Projection).SingleOrDefaultAsync(ct)
        ?? throw new Missing("Comment");

    [HttpDelete("comments/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await workflow.DeleteComment(id, ct);
        return NoContent();
    }

    [HttpGet("tasks/{id:long}/comments")]
    public async Task<List<CommentView>> ByTask(long id, [FromQuery] Page page, CancellationToken ct)
    {
        await workflow.RequireTask(id, ct);
        return await page.Apply(db.Comments.Where(n => n.TaskId == id).OrderBy(n => n.Id))
            .Select(CommentView.Projection).ToListAsync(ct);
    }

    [HttpDelete("tasks/{id:long}/comments")]
    public async Task<IActionResult> DeleteByTask(long id, CancellationToken ct)
    {
        await workflow.RequireTask(id, ct);
        await db.Comments.Where(n => n.TaskId == id).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpGet("users/{id:long}/comments")]
    public async Task<List<CommentView>> ByUser(long id, [FromQuery] Page page, CancellationToken ct)
    {
        await workflow.RequireUser(id, ct);
        return await page.Apply(db.Comments.Where(n => n.UserId == id).OrderBy(n => n.Id))
            .Select(CommentView.Projection).ToListAsync(ct);
    }

    [HttpDelete("users/{id:long}/comments")]
    public async Task<IActionResult> DeleteByUser(long id, CancellationToken ct)
    {
        await workflow.RequireUser(id, ct);
        await db.Comments.Where(n => n.UserId == id).ExecuteDeleteAsync(ct);
        return NoContent();
    }
}
