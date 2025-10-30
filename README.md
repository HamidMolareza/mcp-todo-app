# SampleOnionApp

Sample solution demonstrating an ASP.NET Core API built with an onion architecture, accompanied by a lightweight Model Context Protocol (MCP) compatible JSON-RPC server that shares the same application core.

## Solution layout

- `SampleOnionApp.Domain` – Aggregate roots and domain logic (`TodoItem` entity).
- `SampleOnionApp.Application` – Use-case services, DTOs, and abstraction contracts (`ITodoService`, `ITodoRepository`).
- `SampleOnionApp.Infrastructure` – Infrastructure concerns; includes an in-memory repository and registration helpers.
- `SampleOnionApp.Presentation` – ASP.NET Core minimal API exposing REST endpoints under `/api/todos`.
- `SampleOnionApp.McpServer` – Console host exposing the same functionality over a simple MCP flavoured JSON-RPC loop.

The presentation layer depends only on the application layer and infrastructure registrations, while both the web API and the MCP server resolve application services via dependency injection.

## Prerequisites

- .NET SDK 8.0+
- Network access is only required if you intend to add external NuGet packages. The current codebase builds without third-party package restores.

## Running the web API

```bash
dotnet run --project src/SampleOnionApp.Presentation
```

Once running, exercise the endpoints with any HTTP client:

- `GET /api/todos` – List all items.
- `GET /api/todos/{id}` – Retrieve a single item.
- `POST /api/todos` – Create an item (`{ "title": "Read spec", "description": "Model Context Protocol" }`).
- `PUT /api/todos/{id}` – Update title/description.
- `POST /api/todos/{id}/complete` – Mark as complete.

## Running the MCP server

```bash
dotnet run --project src/SampleOnionApp.McpServer
```

The server listens for newline-delimited JSON-RPC 2.0 requests on STDIN and emits responses on STDOUT. A tiny handshake looks like:

```bash
printf '%s\n' \
  '{"jsonrpc":"2.0","id":1,"method":"initialize"}' \
  '{"jsonrpc":"2.0","id":2,"method":"create_todo","params":{"title":"Draft docs","description":"Outline MCP support"}}' \
  '{"jsonrpc":"2.0","id":3,"method":"list_todos"}' \
  | dotnet run --project src/SampleOnionApp.McpServer
```

Supported methods:

- `initialize`
- `list_todos`
- `get_todo` (`{ "id": "<guid>" }`)
- `create_todo`
- `update_todo`
- `complete_todo`

> **Note**  
> The implementation keeps the transport intentionally simple for learnability: it accepts newline-separated JSON rather than the full MCP framing headers. Adapt the loop in `SampleOnionApp.McpServer/Server/McpServerLoop.cs` if you need byte-accurate compliance.

### Registering the MCP server with Codex CLI

The helper script `scripts/run-mcp-server.sh` wraps the server launch. To make it available to Codex:

```bash
codex mcp add sample-onion-todos /home/home/Desktop/codex-test/scripts/run-mcp-server.sh
```

After that, `codex mcp list` will show the entry. You can start a Codex session with MCP access by launching Codex and selecting the `sample-onion-todos` server when prompted (or by using the relevant CLI flags once generally available).

## Onion architecture notes

- Domain remains free of infrastructure/application dependencies.
- Application layer exposes contracts and orchestrates domain behaviour.
- Infrastructure implements abstractions and registers them for DI.
- Presentation layers depend only on abstractions, enabling easy reuse between HTTP and MCP hosts.

## Next steps

- Swap the in-memory repository with a persistent data store.
- Harden the MCP transport to honour the official framing protocol (content-length headers, heartbeat, etc.).
- Add unit tests around the application service and JSON-RPC handlers.
