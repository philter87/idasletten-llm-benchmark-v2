using Azure.Identity;
using Idasletten.Features.Users.Photos;
using Idasletten.Shared.Auth;
using Idasletten.Shared.Data;
using Idasletten.Shared.Messaging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

namespace Idasletten.Shared.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdasletten(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdaslettenDatabase(configuration);
        services.AddIdaslettenAuthentication(configuration);
        services.AddIdaslettenMessaging();
        services.AddIdaslettenGraph(configuration);
        services.AddIdaslettenForwardedHeaders();

        return services;
    }

    public static IServiceCollection AddIdaslettenDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InMemoryDatabaseKeepAlive>();

        // The connection string is read when the context is resolved, not now: a test host adds its
        // own configuration after this runs, and it has to win.
        services.AddDbContext<AppDbContext>((provider, options) => options.UseSqlite(
            InMemoryDatabase.ResolveConnectionString(provider.GetRequiredService<IConfiguration>()),
            // The match queries include several collections - one query per collection is faster.
            sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        return services;
    }

    public static IServiceCollection AddIdaslettenMessaging(this IServiceCollection services)
    {
        // Picks up every handler in the assembly, including the open generic DomainEventLogger that
        // writes an audit line for all domain events.
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<AppDbContext>());

        return services;
    }

    /// <summary>
    /// Profile pictures come from the Microsoft Graph API when an app registration with a secret is
    /// configured. Without it - locally and in tests - nobody gets a picture.
    /// </summary>
    public static IServiceCollection AddIdaslettenGraph(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        var azureAd = configuration.GetSection("AzureAd");
        var tenantId = azureAd["TenantId"];
        var clientId = azureAd["ClientId"];
        var clientSecret = azureAd["ClientSecret"];

        var canCallGraph = !string.IsNullOrWhiteSpace(tenantId)
                           && !string.IsNullOrWhiteSpace(clientId)
                           && !string.IsNullOrWhiteSpace(clientSecret);

        if (!canCallGraph)
        {
            services.AddSingleton<IUserPhotoProvider, NoUserPhotoProvider>();
            return services;
        }

        services.AddSingleton(_ => new GraphServiceClient(
            new ClientSecretCredential(tenantId, clientId, clientSecret),
            ["https://graph.microsoft.com/.default"]));

        services.AddScoped<IUserPhotoProvider, GraphUserPhotoProvider>();

        return services;
    }

    /// <summary>
    /// Fly.io terminates TLS in its own proxy. Trusting the forwarded headers from any network is what
    /// makes ASP.NET Core generate https redirect URIs for Azure AD instead of http.
    /// </summary>
    public static IServiceCollection AddIdaslettenForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                       | ForwardedHeaders.XForwardedProto
                                       | ForwardedHeaders.XForwardedHost;
            // KnownIPNetworks is what KnownNetworks was called before .NET 9.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}
