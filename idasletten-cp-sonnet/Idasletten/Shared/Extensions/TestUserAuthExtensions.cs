using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Idasletten.Shared.Extensions;

public static class TestUserAuthExtensions
{
    public static IServiceCollection AddTestUserAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var testPassword = configuration["TestUser__Password"];
        var testEmail = configuration["TestUser__Email"];

        if (!string.IsNullOrEmpty(testPassword) && !string.IsNullOrEmpty(testEmail))
        {
            services.AddSingleton<TestUserConfig>(new TestUserConfig(testEmail, testPassword));
        }

        return services;
    }
}

public record TestUserConfig(string Email, string Password);
