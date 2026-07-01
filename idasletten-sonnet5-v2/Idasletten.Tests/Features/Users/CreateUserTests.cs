using Idasletten.Features.Users.Commands.CreateUser;
using Idasletten.Features.Users.Commands.GetOrCreateUserByUsername;
using Idasletten.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features.Users;

public class CreateUserTests(IdaslettenWebApplicationFactory factory) : IClassFixture<IdaslettenWebApplicationFactory>
{
    [Fact]
    public async Task Should_CreateUserWithUniqueUsername_When_UsernameIsAlreadyTaken()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var username = Any.Initials();
        await sender.Send(new CreateUserCommand(username, "First Player"));

        // Act
        var second = await sender.Send(new CreateUserCommand(username, "Second Player"));

        // Assert
        Assert.NotEqual(username, second.UserName);
        Assert.StartsWith(username, second.UserName);
    }

    [Fact]
    public async Task Should_ReturnExistingUser_When_GetOrCreateFindsAMatchingUsername()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var username = Any.Initials();
        var created = await sender.Send(new CreateUserCommand(username, "Original Name"));

        // Act
        var found = await sender.Send(new GetOrCreateUserByUsernameCommand(username, "Ignored Name"));

        // Assert
        Assert.Equal(created.Id, found.Id);
        Assert.Equal("Original Name", found.Name);
    }
}
