using Microsoft.AspNetCore.HttpOverrides;

namespace Idasletten.Shared.Auth;

public static class ForwardedHeadersExtensions
{
    public static IApplicationBuilder UseIdaslettenForwardedHeaders(this IApplicationBuilder app)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        return app.UseForwardedHeaders(options);
    }
}
