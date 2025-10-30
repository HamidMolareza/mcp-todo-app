using System.ComponentModel;
using System.Linq;
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
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Route logs to stderr so stdout remains reserved for MCP traffic.
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddApplication()
    .AddInfrastructure();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "SampleOnionApp Todo MCP Server",
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class TodoTools
{
    [McpServerTool(Name = "todos_list", UseStructuredContent = true)]
    [Description("Get list of todo items")]
    public static async Task<object> GetAllAsync(
        ITodoService todoService,
        CancellationToken cancellationToken = default)
    {
        var items = await todoService.GetAllAsync(cancellationToken);
        return new
        {
            items = items.Select(ToResponseModel).ToArray()
        };
    }

    [McpServerTool(Name = "todos_get", UseStructuredContent = true)]
    [Description("Get a single todo item by its identifier")]
    public static async Task<object> GetByIdAsync(
        ITodoService todoService,
        [Description("Unique identifier of the todo item")] Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await todoService.GetByIdAsync(id, cancellationToken);
        return item is null
            ? new { found = false, item = (TodoItemResponse?)null }
            : new { found = true, item = ToResponseModel(item) };
    }

    [McpServerTool(Name = "todos_create", UseStructuredContent = true)]
    [Description("Create a new todo item")]
    public static async Task<object> CreateAsync(
        ITodoService todoService,
        [Description("Short title for the todo item")] string title,
        [Description("Optional longer description for the todo item")] string? description,
        CancellationToken cancellationToken = default)
    {
        var created = await todoService.CreateAsync(title, description, cancellationToken);
        return new
        {
            created = true,
            item = ToResponseModel(created)
        };
    }

    [McpServerTool(Name = "todos_update", UseStructuredContent = true)]
    [Description("Update an existing todo item")]
    public static async Task<object> UpdateAsync(
        ITodoService todoService,
        [Description("Unique identifier of the todo item")] Guid id,
        [Description("New title for the todo item")] string title,
        [Description("Optional new description for the todo item")] string? description,
        CancellationToken cancellationToken = default)
    {
        var updated = await todoService.UpdateAsync(id, title, description, cancellationToken);
        if (!updated)
        {
            return new
            {
                updated = false,
                reason = "not_found"
            };
        }

        var item = await todoService.GetByIdAsync(id, cancellationToken);
        return new
        {
            updated = true,
            item = item is null ? null : ToResponseModel(item)
        };
    }

    [McpServerTool(Name = "todos_complete", UseStructuredContent = true)]
    [Description("Mark a todo item as completed")]
    public static async Task<object> CompleteAsync(
        ITodoService todoService,
        [Description("Unique identifier of the todo item")] Guid id,
        CancellationToken cancellationToken = default)
    {
        var completed = await todoService.CompleteAsync(id, cancellationToken);
        if (!completed)
        {
            return new
            {
                completed = false,
                reason = "not_found"
            };
        }

        var item = await todoService.GetByIdAsync(id, cancellationToken);
        return new
        {
            completed = true,
            item = item is null ? null : ToResponseModel(item)
        };
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
