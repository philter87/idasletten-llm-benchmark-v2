using Idasletten.Features.Users.Commands.CreateUser;
using Idasletten.Shared.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Data;

/// <summary>Seeds baseline data both for local development and for the test WebApplicationFactory.</summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IdaslettenDbContext db, ISender sender, TestUserOptions testUserOptions)
    {
        await SeedTestUserAsync(db, sender, testUserOptions);
    }

    private static async Task SeedTestUserAsync(IdaslettenDbContext db, ISender sender, TestUserOptions testUserOptions)
    {
        if (!testUserOptions.IsEnabled)
        {
            return;
        }

        var exists = await db.Users.AnyAsync(u => u.Email == testUserOptions.Email);
        if (exists)
        {
            return;
        }

        await sender.Send(new CreateUserCommand("TST", "Test User", testUserOptions.Email));
    }
}
