using Microsoft.Extensions.DependencyInjection;
using Idasletten.Features.Users.Commands.CreateUser;
using Idasletten.Features.Users.Commands.GetOrCreateUserByUsername;
using Idasletten.Shared.Data;
using Idasletten.Tests.TestData;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Tests.Features.Users;

public class UserFeatureTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Should_CreateUser_When_UsernameIsNew()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var username = Any.Username();

        // Act
        var userId = await sender.Send(new CreateUserCommand(username, "Some Name"));

        // Assert
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        Assert.Equal(username, user.UserName);
        Assert.Equal("Some Name", user.Name);
    }

    [Fact]
    public async Task Should_ReturnExistingUserId_When_UsernameAlreadyExists()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var username = Any.Username();
        var firstId = await sender.Send(new CreateUserCommand(username, "First"));

        // Act
        var secondId = await sender.Send(new GetOrCreateUserByUsernameCommand(username));

        // Assert
        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task Should_CreateNewUser_When_UsernameDoesNotExist()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
        var username = Any.Username();

        // Act
        var userId = await sender.Send(new GetOrCreateUserByUsernameCommand(username));

        // Assert
        Assert.True(await db.Users.AnyAsync(u => u.Id == userId && u.NormalizedUserName == username.ToUpperInvariant()));
    }
}
