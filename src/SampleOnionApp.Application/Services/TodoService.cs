using ErrorOr;
using SampleOnionApp.Application.Abstractions.Repositories;
using SampleOnionApp.Application.Abstractions.Services;
using SampleOnionApp.Application.Models;
using SampleOnionApp.Domain.Entities;

namespace SampleOnionApp.Application.Services;

public sealed class TodoService(ITodoRepository repository) : ITodoService
{
    public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(string? filter = null,CancellationToken cancellationToken = default)
    {
        var items = await repository.GetAllAsync(filter, cancellationToken);
        return items.Select(MapToDto).ToArray();
    }

    public async Task<TodoItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : MapToDto(item);
    }

    public async Task<TodoItemDto> CreateAsync(string title, string? description, CancellationToken cancellationToken = default)
    {
        TodoItem item = TodoItem.Create(title, description);
        await repository.AddAsync(item, cancellationToken);
        return MapToDto(item);
    }

    public async Task<IReadOnlyList<TodoItemDto>> CreateRangeAsync(IEnumerable<TodoItemRequest> todoItems, CancellationToken cancellationToken = default)
    {
        var dbItems = todoItems.Select(item=> TodoItem.Create(item.Title, item.Description)).ToList();
        await repository.AddRangeAsync(dbItems, cancellationToken);
        return dbItems.Select(MapToDto).ToList();
    }

    public async Task<bool> UpdateAsync(Guid id, string title, string? description, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        item.UpdateDetails(title, description);
        await repository.UpdateAsync(item, cancellationToken);
        return true;
    }

    public async Task<bool> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        item.Complete();
        await repository.UpdateAsync(item, cancellationToken);
        return true;
    }

    public async Task<ErrorOr<Success>> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        var item = await repository.GetByIdAsync(id, cancellationToken);
        if (item is null) return Error.NotFound(description: $"Can not find any item with id {id}");
        await repository.DeleteByIdAsync(id, cancellationToken);
        return Result.Success;
    }

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default) =>
        repository.DeleteAllAsync(cancellationToken);

    private static TodoItemDto MapToDto(TodoItem item) =>
        new(
            item.Id,
            item.Title,
            item.Description,
            item.IsCompleted,
            item.CreatedAtUtc,
            item.CompletedAtUtc);
}
