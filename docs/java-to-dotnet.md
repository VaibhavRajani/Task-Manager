# From Spring Boot to ASP.NET Core

Start in [BoardWorkflow.cs](../dotnet/TaskManager.Api/Services/BoardWorkflow.cs) beside [TaskService.java](../src/main/java/edu/syr/project/trelloclone/data/service/TaskService.java). Trace a state edit from HTTP input to the history row before studying the read queries.

| Spring concept and original source | C# implementation | Difference to explain |
|---|---|---|
| Java model classes in `data/models` | Entity classes in `Persistence/BoardDb.cs`, records in `Contracts.cs` | EF tracks mutable entities. Records give DTOs value equality and an independent JSON shape. |
| Spring REST controllers, `@RestController` | `[ApiController]` plus `ControllerBase` in `Controllers/` | ASP.NET Core infers body binding and returns validation errors before the action runs. |
| `@GetMapping` and class-level `@RequestMapping` | `[HttpGet]` and `[Route]` | Route templates compose. Numeric constraints select matching routes. |
| Spring field injection and `@Service` | Primary constructor injection and `AddScoped<BoardWorkflow>()` in `Program.cs` | Registration and lifetime are explicit. A request shares one context. |
| Spring Data JPA and Hibernate | EF Core and Oracle MySQL provider | Both map relational entities and track changes. Their generated schemas differ. |
| `JpaRepository` and derived finder methods | `BoardDb`, `DbSet<T>`, LINQ | A context tracks multiple sets together. The assignment reports project joins into named records. |
| Java Streams | LINQ over `IQueryable<T>` | Stream operations commonly process objects in memory. The EF query provider translates supported expressions before materialization. |
| Java CompletableFuture | C# Task and async/await | The Java services here are synchronous. ASP.NET awaits provider I/O and passes the request cancellation token. |
| `application.properties` | ASP.NET configuration in `Program.cs` | Standard providers include optional appsettings.json and environment variables. This project supplies the connection string externally. |
| Controller-specific status checks and generic service exceptions | `Missing`, `Conflict`, `ApiErrors` | The global exception handler emits ProblemDetails. Unexpected errors expose a generic title. |
| JUnit context-load test | xUnit in `WorkflowTests.cs` | Behavioral tests exercise data changes, history and SQL projections using SQLite. |

## Follow a collaboration workflow

1. [CommentService.java](../src/main/java/edu/syr/project/trelloclone/data/service/CommentService.java) resolves a user and task, saves a comment and logs history. [CommentsController.cs](../dotnet/TaskManager.Api/Controllers/CommentsController.cs) receives an AddComment record and calls BoardWorkflow.Comment. The explicit transaction spans two saves because the history subject needs the generated comment ID.
2. [UserTaskRepository.java](../src/main/java/edu/syr/project/trelloclone/data/repository/UserTaskRepository.java) uses native joins to read assignments. [AssignmentsController.cs](../dotnet/TaskManager.Api/Controllers/AssignmentsController.cs) projects task descriptions and user names from navigation properties. The assignment-comment query uses an existence check on both IDs.
3. Java's UserController saves a new user directly. [UsersController.cs](../dotnet/TaskManager.Api/Controllers/UsersController.cs) likewise handles simple persistence. History-producing writes live in the shared workflow so callers cannot forget the accompanying event.

## Thirty seconds

“My original Task Manager used Spring Boot and MySQL. I added an alternative ASP.NET Core backend to practice implementing the same behavior in C#. A user can change a task, assign a teammate, or leave a comment. The most interesting part was keeping a change and its history consistent while moving from Spring repositories to EF Core.”

## Two-minute walkthrough

“Take a state update. The client sends a PUT with a state such as DONE. ASP.NET Core binds that body to a small record and validates it. TasksController passes the ID, state and cancellation token to a scoped BoardWorkflow. That service loads the tracked task, compares the old and requested states, and returns immediately for a repeated value. For a real change it records the transition, updates the timestamp and revision, and calls SaveChangesAsync. EF writes the task and its history together. The revision lets EF detect when another request edited the row after it was read.

“Comment creation has a different constraint. History refers to the comment ID, and MySQL generates that ID. The service opens an explicit transaction, saves the comment to obtain its ID, then saves the history entry and commits. An existing user can comment even before assignment, as in the Java service. The assignment-comment report is narrower because it checks the user and task pair.

“For reads, the API projects directly into DTOs. An assignment list can return a user's name and task description without loading an object graph or looping over related rows. The .NET implementation has its own schema because the key strategy and constraints differ from the original. The xUnit tests are written against SQLite. Running them and checking the migration and concurrency behavior on MySQL are the next validation steps.”

## Ten engineering-manager questions

1. **Why use one scoped context?** It tracks related changes for one request and commits them together. It must not be shared across threads.
2. **Why separate DTOs?** Clients can supply a description or state without setting server-controlled IDs, timestamps or revision tokens.
3. **Can a task move from DONE to TODO?** Yes. Java defines three state values and no transition matrix. C# preserves reopening and suppresses redundant events.
4. **How are duplicate assignments handled?** A pre-check gives a clear conflict. The composite key enforces uniqueness even if two requests pass the check.
5. **Why two saves for a comment?** The first obtains its generated key. An explicit transaction keeps the second history write atomic with the first.
6. **How do you avoid N+1 reads?** The assignment projection selects related names and descriptions inside the query. Materialization happens after filtering, ordering and pagination.
7. **What happens if two state updates overlap?** EF includes the original revision in the update condition. A stale write raises a concurrency exception and yields 409. There is no client-side ETag for edits based on an earlier GET.
8. **What survives a deletion?** Deleting a task cascades to its history, comments and assignments. Individual comment deletion records its ID in task history. Bulk comment deletion skips per-comment events.
9. **Why avoid a generic repository?** DbSet already supports queries and DbContext coordinates writes. BoardWorkflow adds value where multiple business changes belong together.
10. **What can SQLite establish?** The tests exercise workflow logic, joins and relational deletes with that engine. MySQL collation, migration SQL, provider errors and locking still require separate verification.

## Three tradeoffs and three limits

A separate schema isolates development but requires its own migrations. DTO projections keep responses small but add contract definitions. Ordered offset pagination is straightforward but reads can shift between pages during concurrent inserts or deletes.

The API has no authentication, the activity feed disappears when its task is deleted, and provider-specific execution has not been validated.
