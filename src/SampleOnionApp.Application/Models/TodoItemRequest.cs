namespace SampleOnionApp.Application.Models;

public sealed record TodoItemRequest(string Title, string? Description);