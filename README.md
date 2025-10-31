# SampleOnionApp

Sample solution that highlights a lightweight Model Context Protocol (MCP) server built on top of an onion-architecture todo domain. A companion ASP.NET Core minimal API ships alongside it as a convenient way to explore the same application services over HTTP.

## Solution layout

- `SampleOnionApp.Domain` – Aggregate roots and domain logic (`TodoItem` entity).
- `SampleOnionApp.Application` – Use-case services, DTOs, and abstraction contracts (`ITodoService`, `ITodoRepository`, `TodoItemRequest`).
- `SampleOnionApp.Infrastructure` – Infrastructure concerns; includes an in-memory repository and registration helpers.
- `SampleOnionApp.Presentation` – ASP.NET Core minimal API exposing REST endpoints under `/api/todos` (optional companion host).
- `SampleOnionApp.McpServer` – Console host exposing the same functionality over a simple MCP-flavoured JSON-RPC loop (project focus).

The presentation layer depends only on the application layer and infrastructure registrations, while both the web API and the MCP server resolve application services via dependency injection.

## Prerequisites

- .NET SDK 8.0+
- Network access is only required if you intend to add external NuGet packages. The current codebase builds without third-party package restores.

## Running the MCP server

```bash
dotnet run --project src/SampleOnionApp.McpServer
```

The server listens for newline-delimited JSON-RPC 2.0 requests on STDIN and emits responses on STDOUT. A tiny handshake looks like:

```bash
printf '%s\n' \
  '{"jsonrpc":"2.0","id":1,"method":"initialize"}' \
  '{"jsonrpc":"2.0","id":2,"method":"call_tool","params":{"name":"todos_create","arguments":{"title":"Draft docs","description":"Outline MCP support"}}}' \
  '{"jsonrpc":"2.0","id":3,"method":"call_tool","params":{"name":"todos_list"}}' \
  | dotnet run --project src/SampleOnionApp.McpServer
```

Supported methods:

- `initialize`
- `call_tool` with the following tool names:
  - `todos_list`
  - `todos_get`
  - `todos_create`
  - `todos_create_range`
  - `todos_update`
  - `todos_complete`
  - `todos_delete`
  - `todos_delete_all`

> **Note**  
> The implementation keeps the transport intentionally simple for learnability: it accepts newline-separated JSON rather than the full MCP framing headers. Adapt the setup in `src/SampleOnionApp.McpServer/Program.cs` if you need byte-accurate compliance.

### Registering the MCP server with Codex CLI

The helper script `scripts/run-mcp-server.sh` wraps the server launch. To make it available to Codex:

```bash
codex mcp add sample-onion-todos /home/home/Desktop/codex-test/scripts/run-mcp-server.sh
```

After that, `codex mcp list` will show the entry. You can start a Codex session with MCP access by launching Codex and selecting the `sample-onion-todos` server when prompted (or by using the relevant CLI flags once generally available).

## Running the web API (optional companion host)

```bash
dotnet run --project src/SampleOnionApp.Presentation
```

Once running, exercise the endpoints with any HTTP client:

- `GET /api/todos?filter=read` – List all items (optionally filtered by title/description text).
- `GET /api/todos/{id}` – Retrieve a single item.
- `POST /api/todos` – Create an item (`{ "title": "Read spec", "description": "Model Context Protocol" }`).
- `POST /api/todos/bulk` – Create several items in one call (ignores entries with blank titles).
- `PUT /api/todos/{id}` – Update title/description.
- `POST /api/todos/{id}/complete` – Mark as complete.
- `DELETE /api/todos/{id}?confirm=true` – Delete one item (requires an explicit `confirm=true` signal).
- `DELETE /api/todos?confirm=true` – Delete every item and returns the number removed.

## Onion architecture notes

- Domain remains free of infrastructure/application dependencies.
- Application layer exposes contracts and orchestrates domain behaviour.
- Infrastructure implements abstractions and registers them for DI.
- Presentation layers depend only on abstractions, enabling easy reuse between HTTP and MCP hosts.

## Next steps

- Swap the in-memory repository with a persistent data store.
- Harden the MCP transport to honour the official framing protocol (content-length headers, heartbeat, etc.).
- Add unit tests around the application service and JSON-RPC handlers.
