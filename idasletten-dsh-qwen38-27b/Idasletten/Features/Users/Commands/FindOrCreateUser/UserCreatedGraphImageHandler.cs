using Idasletten.Data;
using Idasletten.Features.Users.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Commands.FindOrCreateUser;

/// <summary>
/// Fetches the user's avatar from the Azure Graph API when a user is created
/// (only when Graph is configured). Failures are non-fatal: the user simply
/// keeps no ImageUrl.
/// </summary>
public sealed class UserCreatedGraphImageHandler : INotificationHandler<UserCreated>
{
    private readonly AppDbContext _db;
    private readonly IGraphAvatarService _graph;

    public UserCreatedGraphImageHandler(AppDbContext db, IGraphAvatarService graph)
    {
        _db = db;
        _graph = graph;
    }

    public async Task Handle(UserCreated notification, CancellationToken cancellationToken)
    {
        if (!_graph.IsConfigured) return;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);
        if (user is null || string.IsNullOrEmpty(user.Email)) return;
        try
        {
            var url = await _graph.GetAvatarUrlAsync(user.Email, cancellationToken);
            if (!string.IsNullOrEmpty(url))
            {
                user.ImageUrl = url;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch
        {
            // Graph is best-effort; a missing avatar must never break user creation.
        }
    }
}
