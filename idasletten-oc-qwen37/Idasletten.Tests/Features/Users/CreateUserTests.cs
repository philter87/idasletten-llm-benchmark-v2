using Idasletten.Features.Users.Commands;
using Idasletten.Tests.Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests.Features.Users;

public class CreateUserTests
{
    [Fact]
    public async Task Should_CreateUser_When_UsernameIsUnique()
    {
        var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var username = Any.Username();
        var command = new CreateUserCommand(username, Any.Name(), Any.Email(), null);

        var userId = await mediator.Send(command);

        Assert.NotEqual(Guid.Empty, userId);
    }

    [Fact]
    public async Task Should_ReturnExistingUserId_When_UsernameAlreadyExists()
    {
        var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var username = Any.Username();
        var command1 = new CreateUserCommand(username, Any.Name(), Any.Email(), null);
        var userId1 = await mediator.Send(command1);

        var command2 = new CreateUserCommand(username, Any.Name(), Any.Email(), null);
        var userId2 = await mediator.Send(command2);

        Assert.Equal(userId1, userId2);
    }
}
