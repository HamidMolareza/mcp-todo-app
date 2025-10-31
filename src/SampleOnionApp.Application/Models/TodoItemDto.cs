namespace SampleOnionApp.Application.Models;

public sealed record TodoItemDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);