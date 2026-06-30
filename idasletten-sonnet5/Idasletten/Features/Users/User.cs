using Microsoft.AspNetCore.Identity;

namespace Idasletten.Features.Users;

public class User : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
