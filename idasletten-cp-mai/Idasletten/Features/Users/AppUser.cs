using Microsoft.AspNetCore.Identity;

namespace Idasletten.Features.Users;

public class AppUser : IdentityUser<Guid>
{
    public string Username { get; set; } = string.Empty; // 3-letter initials, unique
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
