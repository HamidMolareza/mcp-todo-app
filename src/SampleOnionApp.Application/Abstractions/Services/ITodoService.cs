using ErrorOr;
using SampleOnionApp.Application.Models;

namespace SampleOnionApp.Application.Abstractions.Services;

public interface ITodoService {
    Task<IReadOnlyList<TodoItemDto>> GetAllAsync(string? filter = null, CancellationToken cancellationToken = default);

    Task<TodoItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TodoItemDto> CreateAsync(string title, string? description, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TodoItemDto>> CreateRangeAsync(IEnumerable<TodoItemRequest> todoItems,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid id, string title, string? description, CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
}