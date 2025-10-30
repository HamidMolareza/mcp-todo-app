using Microsoft.Extensions.DependencyInjection;
using SampleOnionApp.Application.Abstractions.Services;
using SampleOnionApp.Application.Services;

namespace SampleOnionApp.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITodoService, TodoService>();
        return services;
    }
}
