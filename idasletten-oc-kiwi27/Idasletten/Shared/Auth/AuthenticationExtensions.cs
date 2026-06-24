using System.Net.Http.Headers;
using System.Security.Claims;
using Idasletten.Features.Users;
using Idasletten.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Idasletten.Shared.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddIdaslettenAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{configuration["AzureAd:TenantId"]}/v2.0";
            options.ClientId = configuration["AzureAd:ClientId"]!;
            options.ClientSecret = configuration["AzureAd:ClientSecret"];
            options.CallbackPath = configuration["AzureAd:CallbackPath"] ?? "/signin-oidc";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.Scope.Add("User.Read");
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = async context =>
                {
                    var email = context.Principal?.FindFirstValue(ClaimTypes.Email)
                        ?? context.Principal?.FindFirstValue("preferred_username");
                    var name = context.Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
                    var oid = context.Principal?.FindFirstValue("oid");

                    if (string.IsNullOrWhiteSpace(email)) return;

                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                    var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();

                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        var initials = GenerateInitials(name, email);
                        var userId = await mediator.Send(new CreateUserCommand(initials, name, email));
                        user = await userManager.FindByIdAsync(userId.ToString());
                    }

                    if (user != null)
                    {
                        var imageUrl = await TryGetPhotoUrlAsync(context.TokenEndpointResponse?.AccessToken, email, context.HttpContext.RequestServices);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            user.ImageUrl = imageUrl;
                            await userManager.UpdateAsync(user);
                        }

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
                        };
                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        context.Principal = new ClaimsPrincipal(identity);
                    }
                }
            };
        });

        return services;
    }

    private static string GenerateInitials(string name, string email)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = string.Concat(parts.Take(3).Select(p => char.ToUpperInvariant(p[0])));
            if (initials.Length >= 2) return initials;
        }

        var local = email.Split('@')[0];
        return string.Concat(local.Take(3)).ToUpperInvariant();
    }

    private static async Task<string?> TryGetPhotoUrlAsync(string? accessToken, string email, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(accessToken)) return null;
        try
        {
            var client = services.GetRequiredService<IHttpClientFactory>().CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value");
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var base64 = Convert.ToBase64String(bytes);
                return $"data:image/jpeg;base64,{base64}";
            }
        }
        catch
        {
            // Ignore graph errors
        }
        return null;
    }
}
