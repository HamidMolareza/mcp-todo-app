namespace SampleOnionApp.Domain.Entities;

/// <summary>
/// Aggregate root representing a simple to-do task.
/// </summary>
public sealed class TodoItem {
    private TodoItem(Guid id, string title, string? description, DateTime createdAtUtc) {
        Id = id;
        Title = title;
        Description = description;
        CreatedAtUtc = createdAtUtc;
        IsCompleted = false;
    }

    public Guid Id { get; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public bool IsCompleted { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public static TodoItem Create(string title, string? description = null) {
        if (string.IsNullOrWhiteSpace(title)) {
            throw new ArgumentException("Title must be provided.", nameof(title));
        }

        return new TodoItem(Guid.NewGuid(), title.Trim(), description?.Trim(), DateTime.UtcNow);
    }

    public void UpdateDetails(string title, string? description) {
        if (string.IsNullOrWhiteSpace(title)) {
            throw new ArgumentException("Title must be provided.", nameof(title));
        }

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void Complete() {
        if (IsCompleted) {
            return;
        }

        IsCompleted = true;
        CompletedAtUtc = DateTime.UtcNow;
    }
}