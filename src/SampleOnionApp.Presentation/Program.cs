using SampleOnionApp.Application.Abstractions.Services;
using SampleOnionApp.Application.Extensions;
using SampleOnionApp.Application.Models;
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

todos.MapGet("/", async (ITodoService service, string? filter = null, CancellationToken cancellationToken = default) =>
    Results.Ok(await service.GetAllAsync(filter, cancellationToken)))
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

todos.MapPost("/bulk", async (CreateTodoRangeRequest? request, ITodoService service, CancellationToken cancellationToken) =>
{
    var itemsToCreate = (request?.Items ?? [])
        .Where(item => !string.IsNullOrWhiteSpace(item.Title))
        .Select(item => new TodoItemRequest(item.Title, item.Description))
        .ToArray();

    if (itemsToCreate.Length == 0)
    {
        return Results.BadRequest(new
        {
            error = "no_items_provided",
            message = "Provide at least one todo item with a non-empty title."
        });
    }

    var createdItems = await service.CreateRangeAsync(itemsToCreate, cancellationToken);
    return Results.Ok(createdItems);
}).WithName("CreateTodosBulk")
  .WithOpenApi();

todos.MapDelete("/{id:guid}", async (Guid id, bool confirm, ITodoService service, CancellationToken cancellationToken) =>
{
    if (!confirm)
    {
        return Results.BadRequest(new
        {
            error = "confirmation_required",
            message = "Resend the request with confirm=true to delete this item."
        });
    }

    var result = await service.DeleteByIdAsync(id, cancellationToken);
    if (result.IsError)
    {
        var errors = result.Errors.Select(error => new { error.Code, error.Description });
        return Results.NotFound(new { errors });
    }

    return Results.NoContent();
}).WithName("DeleteTodo")
  .WithOpenApi();

todos.MapDelete("/", async (bool confirm, ITodoService service, CancellationToken cancellationToken) =>
{
    if (!confirm)
    {
        return Results.BadRequest(new
        {
            error = "confirmation_required",
            message = "Resend the request with confirm=true to delete all todo items."
        });
    }

    var deletedCount = await service.DeleteAllAsync(cancellationToken);
    return Results.Ok(new { deleted = deletedCount });
}).WithName("DeleteAllTodos")
  .WithOpenApi();

app.Run();
