using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SampleOnionApp.Application.Abstractions.Services;
using SampleOnionApp.Application.Extensions;
using SampleOnionApp.Application.Models;
using SampleOnionApp.Infrastructure.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions => {
    // Route logs to stderr so stdout remains reserved for MCP traffic.
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddApplication()
    .AddInfrastructure();

builder.Services
    .AddMcpServer(options => {
        options.ServerInfo = new Implementation {
            Name = "SampleOnionApp Todo MCP Server",
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class TodoTools {
    [McpServerTool(Name = "todos_list")]
    [Description("Get list of todo items")]
    public static async Task<object> GetAllAsync(
        ITodoService todoService,
        [Description("Filter todo items by title and description case insensitive. This is optional parameter.")]
        string? filter = null,
        CancellationToken cancellationToken = default) {
        var items = await todoService.GetAllAsync(filter, cancellationToken);
        return new { items = items.Select(ToResponseModel).ToArray() };
    }

    [McpServerTool(Name = "todos_get")]
    [Description("Get a single todo item by its identifier")]
    public static async Task<object> GetByIdAsync(
        ITodoService todoService,
        [Description("Unique identifier of the todo item")]
        Guid id,
        CancellationToken cancellationToken = default) {
        var item = await todoService.GetByIdAsync(id, cancellationToken);
        return item is null
            ? new { found = false, item = (TodoItemResponse?)null }
            : new { found = true, item = ToResponseModel(item) }!;
    }

    [McpServerTool(Name = "todos_create")]
    [Description("Create a new todo item")]
    public static async Task<object> CreateAsync(
        ITodoService todoService,
        [Description("Short title for the todo item")]
        string title,
        [Description("Optional longer description for the todo item")]
        string? description = null,
        CancellationToken cancellationToken = default) {
        var created = await todoService.CreateAsync(title, description, cancellationToken);
        return new { created = true, item = ToResponseModel(created) };
    }

    [McpServerTool(Name = "todos_create_range")]
    [Description("Create a list of todo items. To create multi todo items, it is recommended to use the batching API.")]
    public static async Task<object> CreateRangeAsync(
        ITodoService todoService,
        [Description("List of todo items to create")]
        TodoItemRequest[]? todoItems = null,
        CancellationToken cancellationToken = default) {
        var itemsToCreate = (todoItems ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => new TodoItemRequest(item.Title, item.Description))
            .ToArray();

        if (itemsToCreate.Length == 0) {
            return new { created = false, reason = "no_items_provided", items = Array.Empty<TodoItemResponse>() };
        }

        var createdItems = await todoService.CreateRangeAsync(itemsToCreate, cancellationToken);
        return new { created = true, items = createdItems.Select(ToResponseModel).ToArray() };
    }

    [McpServerTool(Name = "todos_update")]
    [Description("Update an existing todo item")]
    public static async Task<object> UpdateAsync(
        ITodoService todoService,
        [Description("Unique identifier of the todo item")]
        Guid id,
        [Description("New title for the todo item")]
        string title,
        [Description("Optional new description for the todo item")]
        string? description,
        CancellationToken cancellationToken = default) {
        var updated = await todoService.UpdateAsync(id, title, description, cancellationToken);
        if (!updated) {
            return new { updated = false, reason = "not_found" };
        }

        var item = await todoService.GetByIdAsync(id, cancellationToken);
        return new { updated = true, item = item is null ? null : ToResponseModel(item) };
    }

    [McpServerTool(Name = "todos_complete")]
    [Description("Mark a todo item as completed")]
    public static async Task<object> CompleteAsync(
        ITodoService todoService,
        [Description("Unique identifier of the todo item")]
        Guid id,
        CancellationToken cancellationToken = default) {
        var completed = await todoService.CompleteAsync(id, cancellationToken);
        if (!completed) {
            return new { completed = false, reason = "not_found" };
        }

        var item = await todoService.GetByIdAsync(id, cancellationToken);
        return new { completed = true, item = item is null ? null : ToResponseModel(item) };
    }

    [McpServerTool(Name = "todos_delete")]
    [Description(
        "Delete a todo item by id. This operation requires explicit user confirmation. Show the item to user and ask user to confirm deletion.")]
    public static async Task<object> DeleteByIdAsync(
        ITodoService todoService,
        [Description("Id of item to delete")] Guid itemId,
        [Description(
            "Set to true to confirm deletion of item. Never auto fill it. Always ask user to explicit reconfirm it.")]
        bool confirm = false,
        CancellationToken cancellationToken = default) {
        if (!confirm) {
            return new {
                confirmation_required = true,
                message =
                    "This operation will permanently delete item. Resend the request with confirm=true to proceed.",
            };
        }

        var result = await todoService.DeleteByIdAsync(itemId, cancellationToken);
        return result.Match<object>(
            _ => new { success = true },
            error => new {
                Success = false,
                Errors = error.Select(e => new { error = e.Description, code = e.Code })
            }
        );
    }

    [McpServerTool(Name = "todos_delete_all")]
    [Description(
        "Delete all todo items. This operation requires explicit user confirmation. So ask user to reconfirm it.")]
    public static async Task<object> DeleteAllAsync(
        ITodoService todoService,
        [Description(
            "Set to true to confirm deletion of all todo items. Never auto fill it. Always ask user to explicit reconfirm it.")]
        bool confirm = false,
        CancellationToken cancellationToken = default) {
        if (!confirm) {
            return new {
                confirmation_required = true,
                message =
                    "This operation will permanently delete all todo items. Resend the request with confirm=true to proceed.",
            };
        }

        var deletedCount = await todoService.DeleteAllAsync(cancellationToken);
        return new { deleted = deletedCount };
    }

    private static TodoItemResponse ToResponseModel(TodoItemDto dto) =>
        new(
            dto.Id,
            dto.Title,
            dto.Description,
            dto.IsCompleted,
            dto.CreatedAtUtc,
            dto.CompletedAtUtc);

    private sealed record TodoItemResponse(
        Guid Id,
        string Title,
        string? Description,
        bool IsCompleted,
        DateTime CreatedAtUtc,
        DateTime? CompletedAtUtc);
}