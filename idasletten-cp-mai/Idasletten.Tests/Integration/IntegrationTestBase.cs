using Idasletten.Tests.Factories;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Idasletten.Tests.Integration;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    static IntegrationTestBase()
    {
        Environment.SetEnvironmentVariable("TestUser__Email", "test@idasletten.local");
        Environment.SetEnvironmentVariable("TestUser__Password", "Test1234!");
    }

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    protected async Task<HttpResponseMessage> LoginAsync()
    {
        // Test-only endpoint that signs in the seeded test user
        return await Client.GetAsync("/test-login");
    }

    protected static string ExtractToken(string html)
    {
        var match = Regex.Match(html, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        return match.Groups[1].Value;
    }
}
