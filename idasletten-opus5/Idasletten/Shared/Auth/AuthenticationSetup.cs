using System.Security.Claims;
using Idasletten.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Idasletten.Shared.Auth;

public static class AuthenticationSetup
{
    /// <summary>
    /// Cookie authentication for the app itself plus Azure AD (an app registration) as the login
    /// provider. Azure AD is only wired up when it is configured, so the app also runs locally and in
    /// tests where only the test user login exists.
    /// </summary>
    public static IServiceCollection AddIdaslettenAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TestUserOptions>(configuration.GetSection(TestUserOptions.SectionName));

        var azureAd = configuration.GetSection("AzureAd");
        var tenantId = azureAd["TenantId"];
        var clientId = azureAd["ClientId"];
        var azureAdEnabled = !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(clientId);

        var authentication = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = azureAdEnabled
                    ? AuthConstants.AzureAdScheme
                    : CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/login";
                options.AccessDeniedPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Cookie.Name = "idasletten.auth";
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        if (azureAdEnabled)
        {
            authentication.AddOpenIdConnect(AuthConstants.AzureAdScheme, options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                options.ClientId = clientId;
                options.ClientSecret = azureAd["ClientSecret"];
                options.CallbackPath = azureAd["CallbackPath"] ?? "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters.NameClaimType = "name";

                options.Events.OnTokenValidated = LinkLocalUserAsync;
            });
        }

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Everybody who signs in with Azure AD also gets a row in our own user table, so they show up in
    /// the tournaments with the same initials as everybody else.
    /// </summary>
    private static async Task LinkLocalUserAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("preferred_username");
        var name = principal.FindFirstValue("name") ?? principal.FindFirstValue(ClaimTypes.Name);
        var initials = InitialsFrom(email, name);

        var sender = context.HttpContext.RequestServices.GetRequiredService<ISender>();
        var user = await sender.Send(new GetOrCreateUser(initials, name, email));

        identity.AddClaim(new Claim(AuthConstants.AppUserIdClaim, user.Id.ToString()));
        identity.AddClaim(new Claim(AuthConstants.InitialsClaim, user.Initials));
    }

    /// <summary>Initials are the local part of the mail address, or the first letters of the name.</summary>
    public static string InitialsFrom(string? email, string? name)
    {
        var localPart = email?.Split('@').FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(localPart) && localPart.Length <= 5)
        {
            return localPart.ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var letters = name
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part[0])
                .Take(3);

            return new string(letters.ToArray()).ToUpperInvariant();
        }

        return (localPart ?? "UNK")[..Math.Min(3, (localPart ?? "UNK").Length)].ToUpperInvariant();
    }
}
