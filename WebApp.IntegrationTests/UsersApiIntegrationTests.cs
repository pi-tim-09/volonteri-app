using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebApp.Common;
using WebApp.DTOs.Users;
using WebApp.Models;

namespace WebApp.IntegrationTests;

public class UsersApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_Returns200()
    {
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_WhenValid_Returns201()
    {
        var password = "Password123!";

        var req = new CreateUserRequest
        {
            Email = $"itest{Guid.NewGuid():N}@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "Integration",
            LastName = "Test",
            PhoneNumber = "123",
            Role = UserRole.Volunteer
        };

        var response = await _client.PostAsJsonAsync("/api/users", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetUsers_Response_IsParsableApiResponse()
    {
        var response = await _client.GetAsync("/api/users?pageNumber=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var api = await response.Content.ReadFromJsonAsync<ApiResponse<UserListDto>>();
        api.Should().NotBeNull();
    }
}
