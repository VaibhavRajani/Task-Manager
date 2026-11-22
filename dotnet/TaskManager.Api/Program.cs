using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TaskManager;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("TaskManager")
    ?? throw new InvalidOperationException("Set ConnectionStrings__TaskManager to the local development database.");
builder.Services.AddDbContext<BoardDb>(options => options.UseMySQL(connection));
builder.Services.AddScoped<BoardWorkflow>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<Stage>(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiErrors>();
builder.Services.AddOpenApi();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.Run();
