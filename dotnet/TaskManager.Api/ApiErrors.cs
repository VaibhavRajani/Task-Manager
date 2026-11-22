using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace TaskManager;

public sealed class Missing(string resource) : Exception($"{resource} was not found.") { }
public sealed class Conflict(string message) : Exception(message) { }

public sealed class ApiErrors(IProblemDetailsService problems, ILogger<ApiErrors> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken token)
    {
        var (status, title) = exception switch
        {
            Missing e => (404, e.Message),
            Conflict e => (409, e.Message),
            DbUpdateConcurrencyException => (409, "The record changed. Reload before retrying."),
            DbUpdateException { InnerException: MySqlException { Number: 1062 or 1452 } }
                => (409, "A related record changed or the assignment already exists."),
            _ => (500, "An unexpected error occurred.")
        };
        if (status == 500)
            logger.LogError("Request {TraceId} failed with {ErrorType}", context.TraceIdentifier, exception.GetType().Name);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new() {
            HttpContext = context, ProblemDetails = new() { Status = status, Title = title }
        });
    }
}
