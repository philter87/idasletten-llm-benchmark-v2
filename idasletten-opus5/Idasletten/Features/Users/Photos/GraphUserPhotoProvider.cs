using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Idasletten.Features.Users.Photos;

/// <summary>
/// Fetches profile pictures from the Microsoft Graph API with application permissions, so pictures of
/// everyone in the organisation (Mjolner) can be read - also for people who never signed in here.
/// The organisation directory is read once and cached; the photo itself is stored as a data URI on the
/// user, which keeps rendering free of extra round trips and works even if Graph is down later.
/// </summary>
public class GraphUserPhotoProvider(
    GraphServiceClient graph,
    IMemoryCache cache,
    ILogger<GraphUserPhotoProvider> logger) : IUserPhotoProvider
{
    private const string DirectoryCacheKey = "graph:organisation-users";
    private static readonly TimeSpan DirectoryCacheTime = TimeSpan.FromHours(12);
    private const int MaxPhotoBytes = 512 * 1024;

    public async Task<string?> GetPhotoUrlAsync(
        string initials, string? email, CancellationToken cancellationToken)
    {
        try
        {
            var directory = await GetOrganisationUsersAsync(cancellationToken);
            var match = Match(directory, initials, email);
            if (match?.Id is null)
            {
                logger.LogInformation("No Graph user found for {Initials}", initials);
                return null;
            }

            using var photo = await graph.Users[match.Id].Photo.Content
                .GetAsync(cancellationToken: cancellationToken);
            if (photo is null)
            {
                return null;
            }

            using var buffer = new MemoryStream();
            await photo.CopyToAsync(buffer, cancellationToken);
            if (buffer.Length is 0 or > MaxPhotoBytes)
            {
                return null;
            }

            return "data:image/jpeg;base64," + Convert.ToBase64String(buffer.ToArray());
        }
        catch (Exception exception)
        {
            // A missing picture must never stop a user from being created.
            logger.LogWarning(exception, "Could not fetch Graph photo for {Initials}", initials);
            return null;
        }
    }

    private async Task<List<Microsoft.Graph.Models.User>> GetOrganisationUsersAsync(
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(DirectoryCacheKey, out List<Microsoft.Graph.Models.User>? cached) && cached is not null)
        {
            return cached;
        }

        var page = await graph.Users.GetAsync(request =>
        {
            request.QueryParameters.Select = ["id", "displayName", "mail", "userPrincipalName"];
            request.QueryParameters.Top = 999;
        }, cancellationToken);

        var users = new List<Microsoft.Graph.Models.User>();
        if (page is not null)
        {
            var iterator = Microsoft.Graph.PageIterator<Microsoft.Graph.Models.User, UserCollectionResponse>
                .CreatePageIterator(graph, page, user =>
                {
                    users.Add(user);
                    return true;
                });

            await iterator.IterateAsync(cancellationToken);
        }

        cache.Set(DirectoryCacheKey, users, DirectoryCacheTime);
        return users;
    }

    /// <summary>Matches on mail first, then on the initials part of the user principal name.</summary>
    private static Microsoft.Graph.Models.User? Match(
        IEnumerable<Microsoft.Graph.Models.User> users, string initials, string? email)
    {
        var candidates = users.ToList();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var byEmail = candidates.FirstOrDefault(u =>
                string.Equals(u.Mail, email, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.UserPrincipalName, email, StringComparison.OrdinalIgnoreCase));

            if (byEmail is not null)
            {
                return byEmail;
            }
        }

        return candidates.FirstOrDefault(u =>
            LocalPart(u.Mail ?? u.UserPrincipalName).Equals(initials, StringComparison.OrdinalIgnoreCase));
    }

    private static string LocalPart(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        var at = address.IndexOf('@');
        return at < 0 ? address : address[..at];
    }
}
