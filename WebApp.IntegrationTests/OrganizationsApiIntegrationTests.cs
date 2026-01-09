using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Common;
using WebApp.Data;
using WebApp.DTOs.Organizations;
using WebApp.Models;

namespace WebApp.IntegrationTests;

public class OrganizationsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public OrganizationsApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

   

    private async Task<Organization> CreateTestOrganizationInDb(string email = "testorg@example.com")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var organization = new Organization
        {
            Email = email,
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "Organization",
            PhoneNumber = "123456789",
            OrganizationName = "Test Org",
            Description = "Test Description",
            Address = "123 Test St",
            City = "Test City",
            Role = UserRole.Organization,
            IsActive = true,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        return organization;
    }

    private async Task<Project> CreateTestProjectForOrganization(int organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var project = new Project
        {
            Title = "Test Project",
            Description = "Test project description",
            Location = "Test Location",
            City = "Test City",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(20),
            ApplicationDeadline = DateTime.UtcNow.AddDays(5),
            MaxVolunteers = 10,
            CurrentVolunteers = 0,
            RequiredSkills = new List<string> { "Skill1" },
            Categories = new List<string> { "Category1" },
            Status = ProjectStatus.Draft,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

   

   

    [Fact]
    public async Task GetOrganizations_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrganizations_ReturnsValidApiResponse()
    {
        // Arrange
        await CreateTestOrganizationInDb("org1@example.com");
        await CreateTestOrganizationInDb("org2@example.com");

        // Act
        var response = await _client.GetAsync("/api/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationListDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Organizations.Should().NotBeEmpty();
        apiResponse.Data.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOrganizations_IncludesOrganizationDetails()
    {
        // Arrange
        var testOrg = await CreateTestOrganizationInDb("detailed@example.com");

        // Act
        var response = await _client.GetAsync("/api/organizations");
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationListDto>>();

        // Assert
        var organization = apiResponse!.Data!.Organizations.Should().Contain(o => o.Email == "detailed@example.com").Subject;
        organization.OrganizationName.Should().Be("Test Org");
        organization.City.Should().Be("Test City");
        organization.IsVerified.Should().BeFalse();
        organization.IsActive.Should().BeTrue();
    }

   

    

    [Fact]
    public async Task GetOrganization_WhenExists_Returns200()
    {
        // Arrange
        var organization = await CreateTestOrganizationInDb();

        // Act
        var response = await _client.GetAsync($"/api/organizations/{organization.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(organization.Id);
        apiResponse.Data.Email.Should().Be(organization.Email);
    }

    [Fact]
    public async Task GetOrganization_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/organizations/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("not found");
    }

  

    

    [Fact]
    public async Task CreateOrganization_WhenValid_Returns201()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Email = $"neworg{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "New",
            LastName = "Organization",
            PhoneNumber = "987654321",
            OrganizationName = "New Test Organization",
            Description = "New organization description",
            Address = "456 New St",
            City = "New City"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Email.Should().Be(request.Email);
        apiResponse.Data.OrganizationName.Should().Be(request.OrganizationName);
        apiResponse.Data.IsVerified.Should().BeFalse("new organizations should start unverified");
        apiResponse.Data.IsActive.Should().BeTrue("new organizations should be active");

        response.Headers.Location.Should().NotBeNull("created response should include Location header");
    }

    [Fact]
    public async Task CreateOrganization_WhenInvalid_Returns400()
    {
        // Arrange - missing required fields
        var request = new CreateOrganizationRequest
        {
            Email = "", 
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "",
            LastName = "",
            PhoneNumber = "",
            OrganizationName = "",
            City = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrganization_HashesPasswordCorrectly()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var request = new CreateOrganizationRequest
        {
            Email = $"secureorg{Guid.NewGuid():N}@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "Secure",
            LastName = "Organization",
            PhoneNumber = "123456789",
            OrganizationName = "Secure Org",
            Description = "Secure organization description",
            Address = "123 Secure St",
            City = "Secure City"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);
        
        // Assert - First verify the request was successful
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();

        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var createdOrg = await context.Organizations.FindAsync(apiResponse.Data!.Id);
        
        createdOrg.Should().NotBeNull();
        createdOrg!.PasswordHash.Should().NotBe(password, "password should be hashed");
        createdOrg.PasswordHash.Should().NotBeNullOrEmpty();
    }

    

    

    [Fact]
    public async Task UpdateOrganization_WhenValid_Returns200()
    {
        // Arrange
        var organization = await CreateTestOrganizationInDb();
        var request = new UpdateOrganizationRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Organization",
            PhoneNumber = "999999999",
            OrganizationName = "Updated Organization",
            Description = "Updated description",
            Address = "789 Updated St",
            City = "Updated City",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/organizations/{organization.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();

       
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedOrg = await context.Organizations.FindAsync(organization.Id);
        updatedOrg!.Email.Should().Be("updated@example.com");
        updatedOrg.OrganizationName.Should().Be("Updated Organization");
        updatedOrg.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOrganization_WhenNotExists_Returns404()
    {
        // Arrange
        var request = new UpdateOrganizationRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Organization",
            PhoneNumber = "999999999",
            OrganizationName = "Updated Organization",
            Description = "Updated description",
            Address = "456 Updated St",
            City = "Updated City",
            IsActive = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/organizations/99999", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOrganization_DoesNotChangeVerificationStatus()
    {
        // Arrange
        var organization = await CreateTestOrganizationInDb();
        
   
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var org = await context.Organizations.FindAsync(organization.Id);
            org!.IsVerified = true;
            org.VerifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        var request = new UpdateOrganizationRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Organization",
            PhoneNumber = "999999999",
            OrganizationName = "Updated Organization",
            City = "Updated City",
            IsActive = true
        };

        // Act
        await _client.PutAsJsonAsync($"/api/organizations/{organization.Id}", request);

        // Assert 
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedOrg = await verifyContext.Organizations.FindAsync(organization.Id);
        updatedOrg!.IsVerified.Should().BeTrue("IsVerified should not be changed via update endpoint");
        updatedOrg.VerifiedAt.Should().NotBeNull();
    }

    

    

    [Fact]
    public async Task DeleteOrganization_WhenNoProjects_Returns200()
    {
        // Arrange
        var organization = await CreateTestOrganizationInDb();

        // Act
        var response = await _client.DeleteAsync($"/api/organizations/{organization.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();

        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deletedOrg = await context.Organizations.FindAsync(organization.Id);
        deletedOrg.Should().BeNull();
    }

    [Fact]
    public async Task DeleteOrganization_WhenHasProjects_Returns500()
    {
        // Arrange
        var organization = await CreateTestOrganizationInDb($"orgwithproj{Guid.NewGuid():N}@example.com");
        var project = await CreateTestProjectForOrganization(organization.Id);
        
        
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var projectCount = await context.Projects.CountAsync(p => p.OrganizationId == organization.Id);
            projectCount.Should().Be(1, "project should be created in database");
        }

        // Act
        var response = await _client.DeleteAsync($"/api/organizations/{organization.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        verifyContext.ChangeTracker.Clear();
        var org = await verifyContext.Organizations.FindAsync(organization.Id);
        org.Should().NotBeNull("organization with projects should not be deleted");
    }

    [Fact]
    public async Task DeleteOrganization_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.DeleteAsync("/api/organizations/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    

   

    [Fact]
    public async Task VerifyOrganization_WhenNotVerified_Returns200()
    {
        // Arrange
        var organization = await CreateTestOrganizationInDb();

        // Act
        var response = await _client.PatchAsync($"/api/organizations/{organization.Id}/verify", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();

       
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
     
        context.ChangeTracker.Clear();
        var verifiedOrg = await context.Organizations.FindAsync(organization.Id);
        
        verifiedOrg.Should().NotBeNull();
        verifiedOrg!.IsVerified.Should().BeTrue();
        verifiedOrg.VerifiedAt.Should().NotBeNull();
        verifiedOrg.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task VerifyOrganization_WhenAlreadyVerified_Returns404()
    {
        // Arrange
        var organization = await CreateTestOrganizationInDb();
        
        await _client.PatchAsync($"/api/organizations/{organization.Id}/verify", null);

     
        var response = await _client.PatchAsync($"/api/organizations/{organization.Id}/verify", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VerifyOrganization_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.PatchAsync("/api/organizations/99999/verify", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    

   

    [Fact]
    public async Task UnverifyOrganization_WhenVerified_Returns200()
    {
        // Arrange
        var organization = await CreateTestOrganizationInDb();
        
       
        await _client.PatchAsync($"/api/organizations/{organization.Id}/verify", null);

        // Act - Unverify
        var response = await _client.PatchAsync($"/api/organizations/{organization.Id}/unverify", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();

        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        
        context.ChangeTracker.Clear();
        var unverifiedOrg = await context.Organizations.FindAsync(organization.Id);
        
        unverifiedOrg.Should().NotBeNull();
        unverifiedOrg!.IsVerified.Should().BeFalse();
        unverifiedOrg.VerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task UnverifyOrganization_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.PatchAsync("/api/organizations/99999/unverify", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    

    

    [Fact]
    public async Task CompleteOrganizationLifecycle_WorksEndToEnd()
    {
        
        var createRequest = new CreateOrganizationRequest
        {
            Email = $"lifecycle{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Lifecycle",
            LastName = "Test",
            PhoneNumber = "123456789",
            OrganizationName = "Lifecycle Organization",
            Description = "Testing complete lifecycle",
            Address = "123 Lifecycle St",
            City = "Lifecycle City"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/organizations", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdOrg = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = createdOrg!.Data!.Id;

        
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.ChangeTracker.Clear();
            var org = await context.Organizations.FindAsync(orgId);
            org.Should().NotBeNull();
            var canCreate = org!.IsVerified && org.IsActive;
            canCreate.Should().BeFalse("unverified organization shouldn't create projects");
        }

       
        var verifyResponse = await _client.PatchAsync($"/api/organizations/{orgId}/verify", null);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.ChangeTracker.Clear();
            var org = await context.Organizations.FindAsync(orgId);
            org.Should().NotBeNull();
            var canCreate = org!.IsVerified && org.IsActive;
            canCreate.Should().BeTrue("verified and active organization should create projects");
        }

        
        var updateRequest = new UpdateOrganizationRequest
        {
            Email = createRequest.Email,
            FirstName = "Updated",
            LastName = "Test",
            PhoneNumber = "987654321",
            OrganizationName = "Updated Lifecycle Organization",
            Description = "Updated description",
            Address = "456 Updated St",
            City = "Updated City",
            IsActive = true
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/organizations/{orgId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var org = await context.Organizations.FindAsync(orgId);
            org!.IsVerified.Should().BeTrue("verification should persist through updates");
            org.OrganizationName.Should().Be("Updated Lifecycle Organization");
        }

        
        var deleteResponse = await _client.DeleteAsync($"/api/organizations/{orgId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        
        var getResponse = await _client.GetAsync($"/api/organizations/{orgId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    
}
