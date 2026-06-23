using Idasletten.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

/// <summary>
/// Custom WebApplicationFactory. The app already uses an in-memory SQLite database outside of
/// Production, so each factory instance gets its own isolated, migrated and seeded database.
/// </summary>
public class IdaslettenFactory : WebApplicationFactory<Program>
{
    public IServiceScope NewScope() => Services.CreateScope();

    /// <summary>Runs work against a fresh DI scope (DbContext, MediatR, ...).</summary>
    public async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = Services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    public Task<T> Send<T>(IRequest<T> request) =>
        InScope(sp => sp.GetRequiredService<IMediator>().Send(request));

    public Task Query(Func<AppDbContext, Task> work) =>
        InScope<object?>(async sp =>
        {
            await work(sp.GetRequiredService<AppDbContext>());
            return null;
        });
}
