using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskManager;

[ApiController, Route("api/users")]
public sealed class UsersController(BoardDb db) : ControllerBase
{
    [HttpGet]
    public Task<List<UserView>> List(string? name, [FromQuery] Page page, CancellationToken ct)
    {
        var users = db.Users.AsQueryable();
        if (name != null) users = users.Where(u => u.Name == name);
        return page.Apply(users.OrderBy(u => u.Id)).Select(u => new UserView(u.Id, u.Name)).ToListAsync(ct);
    }

    [HttpGet("{id:long}")]
    public async Task<UserView> Get(long id, CancellationToken ct) =>
        await db.Users.Where(u => u.Id == id).Select(u => new UserView(u.Id, u.Name)).SingleOrDefaultAsync(ct)
        ?? throw new Missing("User");

    [HttpPost]
    public async Task<ActionResult<UserView>> Create(UserInput input, CancellationToken ct)
    {
        var user = new Member { Name = input.Name.Trim() };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, new UserView(user.Id, user.Name));
    }

    [HttpPut("{id:long}")]
    public async Task<UserView> Rename(long id, UserInput input, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw new Missing("User");
        user.Name = input.Name.Trim();
        await db.SaveChangesAsync(ct);
        return new(id, user.Name);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (await db.Users.Where(u => u.Id == id).ExecuteDeleteAsync(ct) == 0) throw new Missing("User");
        return NoContent();
    }
}
