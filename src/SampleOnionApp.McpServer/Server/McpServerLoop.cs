using System.Text.Json;
using SampleOnionApp.Application.Abstractions.Services;

namespace SampleOnionApp.McpServer.Server;

/// <summary>
/// Minimal JSON-RPC loop that serves Model Context Protocol-style requests for to-do items.
/// </summary>
public sealed class McpServerLoop(ITodoService todoService)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly ITodoService _todoService = todoService;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.Error.WriteLine("SampleOnionApp MCP server started. Awaiting JSON-RPC 2.0 requests...");

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await Console.In.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            RpcRequest? request;

            try
            {
                request = JsonSerializer.Deserialize<RpcRequest>(line, SerializerOptions);
            }
            catch (JsonException jsonEx)
            {
                await WriteOutAsync(new
                {
                    jsonrpc = "2.0",
                    error = new RpcError(-32700, "Parse error", jsonEx.Message)
                });
                continue;
            }

            if (request is null)
            {
                continue;
            }

            RpcResponse? response = await HandleRequestAsync(request, cancellationToken);
            if (response is not null)
            {
                await WriteOutAsync(response);
            }
        }
    }

    private async Task<RpcResponse?> HandleRequestAsync(RpcRequest request, CancellationToken cancellationToken)
    {
        if (request.Id is null || request.Id.Value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        try
        {
            return request.Method switch
            {
                "initialize" => CreateResponse(request, new
                {
                    protocolVersion = "2024-07-03",
                    serverInfo = new { name = "SampleOnionApp.McpServer", version = "1.0.0" },
                    capabilities = new
                    {
                        tools = new
                        {
                            list = new[]
                            {
                                new { name = "list_todos", description = "List all todo items." },
                                new { name = "get_todo", description = "Fetch a single todo item by identifier." },
                                new { name = "create_todo", description = "Create a new todo item." },
                                new { name = "update_todo", description = "Update an existing todo item." },
                                new { name = "complete_todo", description = "Mark a todo item as complete." }
                            }
                        }
                    }
                }),
                "list_todos" => await ListTodosAsync(request, cancellationToken),
                "get_todo" => await GetTodoAsync(request, cancellationToken),
                "create_todo" => await CreateTodoAsync(request, cancellationToken),
                "update_todo" => await UpdateTodoAsync(request, cancellationToken),
                "complete_todo" => await CompleteTodoAsync(request, cancellationToken),
                _ => CreateErrorResponse(request, -32601, $"Unknown method '{request.Method}'.")
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(request, -32000, "Server error", ex.Message);
        }
    }

    private async Task<RpcResponse> ListTodosAsync(RpcRequest request, CancellationToken cancellationToken)
    {
        var todos = await _todoService.GetAllAsync(cancellationToken);
        return CreateResponse(request, new { todos });
    }

    private async Task<RpcResponse> GetTodoAsync(RpcRequest request, CancellationToken cancellationToken)
    {
        var parameters = DeserializeParams<TodoIdParams>(request);
        var todo = await _todoService.GetByIdAsync(parameters.Id, cancellationToken);
        return todo is null
            ? CreateErrorResponse(request, -32004, $"Todo '{parameters.Id}' was not found.")
            : CreateResponse(request, new { todo });
    }

    private async Task<RpcResponse> CreateTodoAsync(RpcRequest request, CancellationToken cancellationToken)
    {
        var parameters = DeserializeParams<CreateTodoParams>(request);
        var todo = await _todoService.CreateAsync(parameters.Title, parameters.Description, cancellationToken);
        return CreateResponse(request, new { todo });
    }

    private async Task<RpcResponse> UpdateTodoAsync(RpcRequest request, CancellationToken cancellationToken)
    {
        var parameters = DeserializeParams<UpdateTodoParams>(request);
        var success = await _todoService.UpdateAsync(parameters.Id, parameters.Title, parameters.Description, cancellationToken);
        if (!success)
        {
            return CreateErrorResponse(request, -32004, $"Todo '{parameters.Id}' was not found.");
        }

        var todo = await _todoService.GetByIdAsync(parameters.Id, cancellationToken);
        return CreateResponse(request, new { todo });
    }

    private async Task<RpcResponse> CompleteTodoAsync(RpcRequest request, CancellationToken cancellationToken)
    {
        var parameters = DeserializeParams<TodoIdParams>(request);
        var success = await _todoService.CompleteAsync(parameters.Id, cancellationToken);
        return success
            ? CreateResponse(request, new { success })
            : CreateErrorResponse(request, -32004, $"Todo '{parameters.Id}' was not found.");
    }

    private static T DeserializeParams<T>(RpcRequest request)
    {
        if (request.Params is null || request.Params.Value.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("Request parameters are required.");
        }

        T? result = request.Params.Value.Deserialize<T>(SerializerOptions);
        return result ?? throw new InvalidOperationException("Unable to parse request parameters.");
    }

    private static RpcResponse CreateResponse(RpcRequest request, object result) =>
        new(request.Id, result, null);

    private static RpcResponse CreateErrorResponse(RpcRequest request, int code, string message, object? data = null) =>
        new(request.Id, null, new RpcError(code, message, data));

    private static Task WriteOutAsync(object payload)
    {
        string serialized = JsonSerializer.Serialize(payload, SerializerOptions);
        Console.Out.WriteLine(serialized);
        return Console.Out.FlushAsync();
    }

    private sealed record RpcRequest(
        string Jsonrpc,
        string Method,
        JsonElement? Params,
        JsonElement? Id);

    private sealed record RpcResponse(
        JsonElement? Id,
        object? Result,
        RpcError? Error)
    {
        public string Jsonrpc { get; } = "2.0";
    }

    private sealed record RpcError(int Code, string Message, object? Data);

    private sealed record CreateTodoParams(string Title, string? Description);

    private sealed record UpdateTodoParams(Guid Id, string Title, string? Description);

    private sealed record TodoIdParams(Guid Id);
}
