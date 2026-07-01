using Idasletten.Data;
using Idasletten.Shared.Entities;
using Microsoft.Identity.Web;

namespace Idasletten.Shared.Auth;

/// <summary>
/// Best-effort fetch of a user's profile photo from Microsoft Graph, run once when a User
/// is first provisioned from an Azure AD sign-in. Silently does nothing if Graph access,
/// consent, or a photo isn't available (e.g. running without real Azure AD credentials).
/// </summary>
public static class GraphProfilePhotoFetcher
{
    private const string PhotoDirectory = "images/users";

    public static async Task TryFetchAndSetAsync(HttpContext httpContext, IdaslettenDbContext db, User user)
    {
        try
        {
            var tokenAcquisition = httpContext.RequestServices.GetService<ITokenAcquisition>();
            if (tokenAcquisition is null)
            {
                return;
            }

            var token = await tokenAcquisition.GetAccessTokenForUserAsync(["https://graph.microsoft.com/User.Read"]);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);
            using var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value");
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var directory = Path.Combine(env.WebRootPath, PhotoDirectory);
            Directory.CreateDirectory(directory);

            var fileName = $"{user.Id}.jpg";
            await using (var fileStream = File.Create(Path.Combine(directory, fileName)))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            user.ImageUrl = $"/{PhotoDirectory}/{fileName}";
            await db.SaveChangesAsync();
        }
        catch
        {
            // Graph photo lookup is best-effort only; never block sign-in on it.
        }
    }
}
