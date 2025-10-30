using SampleOnionApp.Application.Abstractions.Services;
using SampleOnionApp.Application.Extensions;
using SampleOnionApp.Infrastructure.Extensions;
using SampleOnionApp.Presentation.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var todos = app.MapGroup("/api/todos")
    .WithTags("Todos");

todos.MapGet("/", async (ITodoService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetAllAsync(cancellationToken)))
    .WithName("GetTodos")
    .WithOpenApi();

todos.MapGet("/{id:guid}", async (Guid id, ITodoService service, CancellationToken cancellationToken) =>
{
    var todo = await service.GetByIdAsync(id, cancellationToken);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
}).WithName("GetTodoById")
  .WithOpenApi();

todos.MapPost("/", async (CreateTodoRequest request, ITodoService service, CancellationToken cancellationToken) =>
{
    var todo = await service.CreateAsync(request.Title, request.Description, cancellationToken);
    return Results.Created($"/api/todos/{todo.Id}", todo);
}).WithName("CreateTodo")
  .WithOpenApi();

todos.MapPut("/{id:guid}", async (Guid id, UpdateTodoRequest request, ITodoService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateAsync(id, request.Title, request.Description, cancellationToken);
    return updated ? Results.NoContent() : Results.NotFound();
}).WithName("UpdateTodo")
  .WithOpenApi();

todos.MapPost("/{id:guid}/complete", async (Guid id, ITodoService service, CancellationToken cancellationToken) =>
{
    var completed = await service.CompleteAsync(id, cancellationToken);
    return completed ? Results.NoContent() : Results.NotFound();
}).WithName("CompleteTodo")
  .WithOpenApi();

app.Run();
