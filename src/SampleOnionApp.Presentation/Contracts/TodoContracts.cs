namespace SampleOnionApp.Presentation.Contracts;

public sealed record CreateTodoRequest(string Title, string? Description);

public sealed record UpdateTodoRequest(string Title, string? Description);
