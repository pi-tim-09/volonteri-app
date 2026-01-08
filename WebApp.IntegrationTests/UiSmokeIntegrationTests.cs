using System.Net;
using FluentAssertions;

namespace WebApp.IntegrationTests;

public class UiSmokeIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UiSmokeIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_Home_Index_Returns200()
    {
        var resp = await _client.GetAsync("/");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Account_Login_Returns200()
    {
        var resp = await _client.GetAsync("/Account/Login");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
