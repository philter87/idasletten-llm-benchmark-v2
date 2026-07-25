using Idasletten.Features.Users.Events;
using Idasletten.Features.Users.Photos;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.EventHandlers;

/// <summary>The spec asks for the picture to be fetched when the user is created.</summary>
public class FetchUserPhotoOnUserCreated(
    AppDbContext db,
    IUserPhotoProvider photos,
    ILogger<FetchUserPhotoOnUserCreated> logger) : INotificationHandler<UserCreated>
{
    public async Task Handle(UserCreated notification, CancellationToken cancellationToken)
    {
        var imageUrl = await photos.GetPhotoUrlAsync(
            notification.Initials, notification.Email, cancellationToken);

        if (string.IsNullOrEmpty(imageUrl))
        {
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.ImageUrl = imageUrl;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Stored profile picture for {Initials}", notification.Initials);
    }
}
