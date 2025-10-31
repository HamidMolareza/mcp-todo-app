using Microsoft.Extensions.DependencyInjection;
using SampleOnionApp.Application.Abstractions.Repositories;
using SampleOnionApp.Infrastructure.Persistence;

namespace SampleOnionApp.Infrastructure.Extensions;

public static class DependencyInjection {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services) {
        services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
        return services;
    }
}