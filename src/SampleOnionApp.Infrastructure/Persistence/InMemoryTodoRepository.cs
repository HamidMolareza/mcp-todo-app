using System.Collections.Concurrent;
using SampleOnionApp.Application.Abstractions.Repositories;
using SampleOnionApp.Domain.Entities;

namespace SampleOnionApp.Infrastructure.Persistence;

public sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly ConcurrentDictionary<Guid, TodoItem> _items = new();

    public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = _items.Values
            .OrderBy(item => item.CreatedAtUtc)
            .ToArray();

        return Task.FromResult<IReadOnlyList<TodoItem>>(snapshot);
    }

    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task AddAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }
}
