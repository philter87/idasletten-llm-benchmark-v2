using Idasletten.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

public class UserPageTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UserPageTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_ShowUserStats_When_VisitingUserPage()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Username == "THO");

        // Act
        var html = await client.GetStringAsync($"/users/{user.Id}");

        // Assert
        Assert.Contains("THO", html);
        Assert.Contains("Tournaments", html);
        Assert.Contains("Valkyrior Open", html);
    }
}
