using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskManager;

[ApiController, Route("api/tasks")]
public sealed class TasksController(BoardDb db, BoardWorkflow workflow) : ControllerBase
{
    [HttpGet]
    public Task<List<TaskView>> List([FromQuery] Page page, CancellationToken ct) =>
        page.Apply(db.Tasks.OrderBy(t => t.Id)).Select(TaskView.Projection).ToListAsync(ct);

    [HttpGet("{id:long}")]
    public async Task<TaskView> Get(long id, CancellationToken ct) =>
        await db.Tasks.Where(t => t.Id == id).Select(TaskView.Projection).SingleOrDefaultAsync(ct)
        ?? throw new Missing("Task");

    [HttpPost]
    public async Task<ActionResult<TaskView>> Create(CreateTask input, CancellationToken ct)
    {
        var task = await workflow.Create(input, ct);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id:long}/state")]
    public Task<TaskView> State(long id, ChangeState input, CancellationToken ct) =>
        workflow.SetState(id, input.State!.Value, ct);

    [HttpPut("{id:long}/description")]
    public Task<TaskView> Describe(long id, ChangeDescription input, CancellationToken ct) =>
        workflow.Describe(id, input.Description, ct);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(ct) == 0) throw new Missing("Task");
        return NoContent();
    }

    [HttpGet("{id:long}/history")]
    public async Task<List<HistoryView>> History(long id, [FromQuery] Page page, CancellationToken ct)
    {
        await workflow.RequireTask(id, ct);
        return await page.Apply(db.History.Where(h => h.TaskId == id).OrderBy(h => h.Id))
            .Select(h => new HistoryView(h.Id, h.TaskId, h.Event, h.SubjectId, h.AtUtc)).ToListAsync(ct);
    }
}
