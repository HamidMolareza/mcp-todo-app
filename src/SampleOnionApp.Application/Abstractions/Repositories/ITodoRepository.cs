using SampleOnionApp.Domain.Entities;

namespace SampleOnionApp.Application.Abstractions.Repositories;

public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TodoItem> items, CancellationToken cancellationToken = default);

    Task UpdateAsync(TodoItem item, CancellationToken cancellationToken = default);

    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
}
