using SampleOnionApp.Application.Models;

namespace SampleOnionApp.Application.Abstractions.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TodoItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TodoItemDto> CreateAsync(string title, string? description, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid id, string title, string? description, CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
}
