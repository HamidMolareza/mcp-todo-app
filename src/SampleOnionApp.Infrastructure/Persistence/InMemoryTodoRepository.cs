using System.Collections.Concurrent;
using SampleOnionApp.Application.Abstractions.Repositories;
using SampleOnionApp.Domain.Entities;

namespace SampleOnionApp.Infrastructure.Persistence;

public sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly ConcurrentDictionary<Guid, TodoItem> _items = new();

    public Task<IReadOnlyList<TodoItem>> GetAllAsync(string? filter = null, CancellationToken cancellationToken = default)
    {
        var snapshot = _items.Values
            .OrderBy(item => item.CreatedAtUtc)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            snapshot = snapshot.Where(item =>
                item.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (item.Description != null && item.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

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

    public async Task AddRangeAsync(IEnumerable<TodoItem> items, CancellationToken cancellationToken = default) {
        foreach (var todoItem in items) 
            await AddAsync(todoItem, cancellationToken);
    }

    public Task UpdateAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        _items.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var count = _items.Count;
        _items.Clear();
        return Task.FromResult(count);
    }
}
