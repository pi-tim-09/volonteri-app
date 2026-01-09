using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Common;
using WebApp.Data;
using WebApp.DTOs.Admins;
using WebApp.Models;

namespace WebApp.IntegrationTests;

public class AdminsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AdminsApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Helper Methods

    private async Task<Admin> CreateTestAdminInDb(string email = "testadmin@example.com")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var admin = new Admin
        {
            Email = email,
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "Admin",
            PhoneNumber = "123456789",
            Department = "IT",
            Role = UserRole.Admin,
            IsActive = true,
            CanManageUsers = false,
            CanManageOrganizations = false,
            CanManageProjects = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Admins.Add(admin);
        await context.SaveChangesAsync();
        return admin;
    }

    #endregion

    #region GET /api/admins Tests

    [Fact]
    public async Task GetAdmins_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/admins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdmins_ReturnsValidApiResponse()
    {
        // Arrange
        await CreateTestAdminInDb("admin1@example.com");
        await CreateTestAdminInDb("admin2@example.com");

        // Act
        var response = await _client.GetAsync("/api/admins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AdminListDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Admins.Should().NotBeEmpty();
        apiResponse.Data.TotalCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region GET /api/admins/{id} Tests

    [Fact]
    public async Task GetAdmin_WhenExists_Returns200()
    {
        // Arrange
        var admin = await CreateTestAdminInDb();

        // Act
        var response = await _client.GetAsync($"/api/admins/{admin.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AdminDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(admin.Id);
        apiResponse.Data.Email.Should().Be(admin.Email);
        apiResponse.Data.Department.Should().Be("IT");
    }

    [Fact]
    public async Task GetAdmin_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/admins/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("not found");
    }

    #endregion

    #region POST /api/admins Tests

    [Fact]
    public async Task CreateAdmin_WhenValid_Returns201()
    {
        // Arrange
        var request = new CreateAdminRequest
        {
            Email = $"newadmin{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "New",
            LastName = "Admin",
            PhoneNumber = "987654321",
            Department = "HR",
            CanManageUsers = true,
            CanManageOrganizations = false,
            CanManageProjects = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AdminDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Email.Should().Be(request.Email);
        apiResponse.Data.Department.Should().Be("HR");
        apiResponse.Data.CanManageUsers.Should().BeFalse("new admins should have no permissions by default");
        apiResponse.Data.CanManageOrganizations.Should().BeFalse();
        apiResponse.Data.CanManageProjects.Should().BeFalse();
        apiResponse.Data.IsActive.Should().BeTrue();

        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAdmin_WhenInvalid_Returns400()
    {
        // Arrange - missing required fields
        var request = new CreateAdminRequest
        {
            Email = "",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "",
            LastName = "",
            PhoneNumber = "",
            Department = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admins", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/admins/{id} Tests

    [Fact]
    public async Task UpdateAdmin_WhenValid_Returns200()
    {
        // Arrange
        var admin = await CreateTestAdminInDb();
        var request = new UpdateAdminRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Admin",
            PhoneNumber = "999999999",
            Department = "Finance",
            CanManageUsers = true,
            CanManageOrganizations = true,
            CanManageProjects = true,
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/admins/{admin.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();

        // Verify in database - permissions should NOT be updated via this endpoint
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedAdmin = await context.Admins.FindAsync(admin.Id);
        updatedAdmin!.Email.Should().Be("updated@example.com");
        updatedAdmin.Department.Should().Be("Finance");
        updatedAdmin.IsActive.Should().BeFalse();
        updatedAdmin.CanManageUsers.Should().BeFalse("permissions should not change via update");
    }

    [Fact]
    public async Task UpdateAdmin_WhenNotExists_Returns404()
    {
        // Arrange
        var request = new UpdateAdminRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Admin",
            PhoneNumber = "999999999",
            Department = "Finance",
            IsActive = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/admins/99999", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/admins/{id} Tests

    [Fact]
    public async Task DeleteAdmin_WhenNotLastAdmin_Returns200()
    {
        // Arrange - Create 2 admins so we can delete one
        var admin1 = await CreateTestAdminInDb("admin1@example.com");
        var admin2 = await CreateTestAdminInDb("admin2@example.com");

        // Act
        var response = await _client.DeleteAsync($"/api/admins/{admin1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();

        // Verify deleted from database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deletedAdmin = await context.Admins.FindAsync(admin1.Id);
        deletedAdmin.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAdmin_WhenLastAdmin_Returns500()
    {
        // Arrange - Ensure we have exactly one admin
        using (var setupScope = _factory.Services.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Clear all admins first
            setupContext.Admins.RemoveRange(setupContext.Admins);
            await setupContext.SaveChangesAsync();
        }

        // Create only one admin
        var admin = await CreateTestAdminInDb("onlyadmin@example.com");

        // Act
        var response = await _client.DeleteAsync($"/api/admins/{admin.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "deleting the last admin should throw an exception that returns 500");

        // Verify NOT deleted from database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stillExists = await context.Admins.FindAsync(admin.Id);
        stillExists.Should().NotBeNull("last admin cannot be deleted");
    }

    [Fact]
    public async Task DeleteAdmin_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.DeleteAsync("/api/admins/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Permission Management Tests

    [Fact]
    public async Task GrantUserManagementPermission_WhenValid_Returns200()
    {
        // Arrange
        var admin = await CreateTestAdminInDb();

        // Act
        var response = await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/users/grant", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        var updatedAdmin = await context.Admins.FindAsync(admin.Id);
        updatedAdmin!.CanManageUsers.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeUserManagementPermission_WhenValid_Returns200()
    {
        // Arrange
        var admin = await CreateTestAdminInDb();
        
        // Grant first
        await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/users/grant", null);

        // Act - Revoke
        var response = await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/users/revoke", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        var updatedAdmin = await context.Admins.FindAsync(admin.Id);
        updatedAdmin!.CanManageUsers.Should().BeFalse();
    }

    [Fact]
    public async Task GrantOrganizationManagementPermission_WhenValid_Returns200()
    {
        // Arrange
        var admin = await CreateTestAdminInDb();

        // Act
        var response = await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/organizations/grant", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        var updatedAdmin = await context.Admins.FindAsync(admin.Id);
        updatedAdmin!.CanManageOrganizations.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeOrganizationManagementPermission_WhenValid_Returns200()
    {
        // Arrange
        var admin = await CreateTestAdminInDb();
        
        // Grant first
        await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/organizations/grant", null);

        // Act - Revoke
        var response = await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/organizations/revoke", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        var updatedAdmin = await context.Admins.FindAsync(admin.Id);
        updatedAdmin!.CanManageOrganizations.Should().BeFalse();
    }

    [Fact]
    public async Task GrantProjectManagementPermission_WhenValid_Returns200()
    {
        // Arrange
        var admin = await CreateTestAdminInDb();

        // Act
        var response = await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/projects/grant", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        var updatedAdmin = await context.Admins.FindAsync(admin.Id);
        updatedAdmin!.CanManageProjects.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeProjectManagementPermission_WhenValid_Returns200()
    {
        // Arrange
        var admin = await CreateTestAdminInDb();
        
        // Grant first
        await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/projects/grant", null);

        // Act - Revoke
        var response = await _client.PatchAsync($"/api/admins/{admin.Id}/permissions/projects/revoke", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        var updatedAdmin = await context.Admins.FindAsync(admin.Id);
        updatedAdmin!.CanManageProjects.Should().BeFalse();
    }

    [Fact]
    public async Task GrantPermission_WhenAdminNotExists_Returns404()
    {
        // Act
        var response = await _client.PatchAsync("/api/admins/99999/permissions/users/grant", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region End-to-End Workflow Tests

    [Fact]
    public async Task CompleteAdminLifecycle_WorksEndToEnd()
    {
        // 1. Create admin
        var createRequest = new CreateAdminRequest
        {
            Email = $"lifecycle{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Lifecycle",
            LastName = "Admin",
            PhoneNumber = "123456789",
            Department = "IT"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admins", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdAdmin = await createResponse.Content.ReadFromJsonAsync<ApiResponse<AdminDto>>();
        var adminId = createdAdmin!.Data!.Id;

        // 2. Verify admin has no permissions initially
        createdAdmin.Data.CanManageUsers.Should().BeFalse();
        createdAdmin.Data.CanManageOrganizations.Should().BeFalse();
        createdAdmin.Data.CanManageProjects.Should().BeFalse();

        // 3. Grant all permissions
        await _client.PatchAsync($"/api/admins/{adminId}/permissions/users/grant", null);
        await _client.PatchAsync($"/api/admins/{adminId}/permissions/organizations/grant", null);
        await _client.PatchAsync($"/api/admins/{adminId}/permissions/projects/grant", null);

        // 4. Verify permissions granted
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var admin = await context.Admins.FindAsync(adminId);
            admin!.CanManageUsers.Should().BeTrue();
            admin.CanManageOrganizations.Should().BeTrue();
            admin.CanManageProjects.Should().BeTrue();
        }

        // 5. Update admin (permissions should persist)
        var updateRequest = new UpdateAdminRequest
        {
            Email = createRequest.Email,
            FirstName = "Updated",
            LastName = "Admin",
            PhoneNumber = "987654321",
            Department = "HR",
            IsActive = true
        };

        await _client.PutAsJsonAsync($"/api/admins/{adminId}", updateRequest);

        // 6. Verify update didn't affect permissions
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var admin = await context.Admins.FindAsync(adminId);
            admin!.CanManageUsers.Should().BeTrue("permissions should persist through updates");
            admin.Department.Should().Be("HR");
        }

        // 7. Create second admin (so we can delete the first)
        var admin2 = await CreateTestAdminInDb("admin2@example.com");

        // 8. Delete first admin
        var deleteResponse = await _client.DeleteAsync($"/api/admins/{adminId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 9. Verify deletion
        var getResponse = await _client.GetAsync($"/api/admins/{adminId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
