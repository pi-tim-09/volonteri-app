using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Common;
using WebApp.Data;
using WebApp.DTOs.Projects;
using WebApp.Models;

namespace WebApp.IntegrationTests;

public class ProjectsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProjectsApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    

    private async Task<Organization> CreateTestOrganizationInDb(string email = "testorg@example.com", bool verified = true)
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
            IsVerified = verified,
            VerifiedAt = verified ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        return organization;
    }

    private async Task<Project> CreateTestProjectInDb(int organizationId, ProjectStatus status = ProjectStatus.Draft)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var project = new Project
        {
            Title = "Test Project",
            Description = "Test Description",
            Location = "Test Location",
            City = "Test City",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(20),
            ApplicationDeadline = DateTime.UtcNow.AddDays(5),
            MaxVolunteers = 10,
            CurrentVolunteers = 0,
            RequiredSkills = new List<string> { "Skill1" },
            Categories = new List<string> { "Category1" },
            Status = status,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

 

   

    [Fact]
    public async Task GetProjects_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProjects_ReturnsValidApiResponse()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb("org1@example.com");
        await CreateTestProjectInDb(org.Id);

        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectListDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Projects.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProjects_FilterByOrganizationId_ReturnsFilteredResults()
    {
        // Arrange
        var org1 = await CreateTestOrganizationInDb("orgfilter1@example.com");
        var org2 = await CreateTestOrganizationInDb("orgfilter2@example.com");
        await CreateTestProjectInDb(org1.Id);
        await CreateTestProjectInDb(org2.Id);

        // Act
        var response = await _client.GetAsync($"/api/projects?organizationId={org1.Id}");

        // Assert
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectListDto>>();
        apiResponse!.Data!.Projects.Should().OnlyContain(p => p.OrganizationId == org1.Id);
    }

   

    #

    [Fact]
    public async Task GetProject_WhenExists_Returns200()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb();
        var project = await CreateTestProjectInDb(org.Id);

        // Act
        var response = await _client.GetAsync($"/api/projects/{project.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        apiResponse!.Data!.Id.Should().Be(project.Id);
        apiResponse.Data.Title.Should().Be("Test Project");
    }

    [Fact]
    public async Task GetProject_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/projects/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    

    

    [Fact]
    public async Task CreateProject_WhenValid_Returns201()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb("createproj@example.com");
        
        var request = new CreateProjectRequest
        {
            Title = "New Project",
            Description = "New Description",
            Location = "New Location",
            City = "New City",
            StartDate = DateTime.UtcNow.AddDays(15),
            EndDate = DateTime.UtcNow.AddDays(25),
            ApplicationDeadline = DateTime.UtcNow.AddDays(10),
            MaxVolunteers = 20,
            RequiredSkills = new List<string> { "Skill1", "Skill2" },
            Categories = new List<string> { "Cat1" },
            OrganizationId = org.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        apiResponse!.Data!.Title.Should().Be("New Project");
        apiResponse.Data.Status.Should().Be(ProjectStatus.Draft);
        apiResponse.Data.CurrentVolunteers.Should().Be(0);
    }

    [Fact]
    public async Task CreateProject_WhenOrganizationNotExists_Returns400()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Title = "New Project",
            Description = "New Description",
            Location = "New Location",
            City = "New City",
            StartDate = DateTime.UtcNow.AddDays(15),
            EndDate = DateTime.UtcNow.AddDays(25),
            ApplicationDeadline = DateTime.UtcNow.AddDays(10),
            MaxVolunteers = 20,
            OrganizationId = 99999
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    

   

    [Fact]
    public async Task UpdateProject_WhenValid_Returns200()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb();
        var project = await CreateTestProjectInDb(org.Id);

        var request = new UpdateProjectRequest
        {
            Title = "Updated Project",
            Description = "Updated Description",
            Location = "Updated Location",
            City = "Updated City",
            StartDate = DateTime.UtcNow.AddDays(20),
            EndDate = DateTime.UtcNow.AddDays(30),
            ApplicationDeadline = DateTime.UtcNow.AddDays(15),
            MaxVolunteers = 25,
            RequiredSkills = new List<string> { "NewSkill" },
            Categories = new List<string> { "NewCat" },
            Status = ProjectStatus.Draft,
            OrganizationId = org.Id
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/projects/{project.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

       
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await context.Projects.FindAsync(project.Id);
        updated!.Title.Should().Be("Updated Project");
        updated.MaxVolunteers.Should().Be(25);
    }

    [Fact]
    public async Task UpdateProject_WhenNotExists_Returns404()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb();
        
        var request = new UpdateProjectRequest
        {
            Title = "Updated",
            Description = "Updated",
            Location = "Updated",
            City = "Updated",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            ApplicationDeadline = DateTime.UtcNow.AddDays(5),
            MaxVolunteers = 10,
            Status = ProjectStatus.Draft,
            OrganizationId = org.Id
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/projects/99999", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

   

    

    [Fact]
    public async Task DeleteProject_WhenNoAcceptedApplications_Returns200()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb();
        var project = await CreateTestProjectInDb(org.Id);

        // Act
        var response = await _client.DeleteAsync($"/api/projects/{project.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

      
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deleted = await context.Projects.FindAsync(project.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProject_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.DeleteAsync("/api/projects/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

   

  

    [Fact]
    public async Task PublishProject_WhenValidDraft_Returns200()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb();
        var project = await CreateTestProjectInDb(org.Id, ProjectStatus.Draft);

        // Act
        var response = await _client.PatchAsync($"/api/projects/{project.Id}/publish", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

       
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        
        var published = await context.Projects.FindAsync(project.Id);
        published!.Status.Should().Be(ProjectStatus.Published);
    }

    [Fact]
    public async Task PublishProject_WhenAlreadyPublished_Returns404()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb();
        var project = await CreateTestProjectInDb(org.Id, ProjectStatus.Published);

        // Act
        var response = await _client.PatchAsync($"/api/projects/{project.Id}/publish", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    

   

    [Fact]
    public async Task CompleteProject_WhenPublished_Returns200()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb();
        var project = await CreateTestProjectInDb(org.Id, ProjectStatus.Published);

        // Act
        var response = await _client.PatchAsync($"/api/projects/{project.Id}/complete", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

       
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        
        var completed = await context.Projects.FindAsync(project.Id);
        completed!.Status.Should().Be(ProjectStatus.Completed);
    }

    

    

    [Fact]
    public async Task CancelProject_WhenValid_Returns200()
    {
        // Arrange
        var org = await CreateTestOrganizationInDb();
        var project = await CreateTestProjectInDb(org.Id, ProjectStatus.Published);

        // Act
        var response = await _client.PatchAsync($"/api/projects/{project.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

      
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        
        var cancelled = await context.Projects.FindAsync(project.Id);
        cancelled!.Status.Should().Be(ProjectStatus.Cancelled);
    }

  

    

    [Fact]
    public async Task CompleteProjectLifecycle_WorksEndToEnd()
    {
       
        var org = await CreateTestOrganizationInDb($"lifecycle{Guid.NewGuid():N}@example.com");

        
        var createRequest = new CreateProjectRequest
        {
            Title = "Lifecycle Project",
            Description = "Testing complete lifecycle",
            Location = "Lifecycle Location",
            City = "Lifecycle City",
            StartDate = DateTime.UtcNow.AddDays(15),
            EndDate = DateTime.UtcNow.AddDays(25),
            ApplicationDeadline = DateTime.UtcNow.AddDays(10),
            MaxVolunteers = 15,
            RequiredSkills = new List<string> { "Teamwork" },
            Categories = new List<string> { "Community" },
            OrganizationId = org.Id
        };

        var createResponse = await _client.PostAsJsonAsync("/api/projects", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdProject = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        var projectId = createdProject!.Data!.Id;

       
        createdProject.Data.Status.Should().Be(ProjectStatus.Draft);

    
        var publishResponse = await _client.PatchAsync($"/api/projects/{projectId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

       
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var proj = await context.Projects.FindAsync(projectId);
            proj!.Status.Should().Be(ProjectStatus.Published);
        }

       
        var updateRequest = new UpdateProjectRequest
        {
            Title = "Updated Lifecycle Project",
            Description = createRequest.Description,
            Location = createRequest.Location,
            City = createRequest.City,
            StartDate = createRequest.StartDate,
            EndDate = createRequest.EndDate,
            ApplicationDeadline = createRequest.ApplicationDeadline,
            MaxVolunteers = 20,
            RequiredSkills = createRequest.RequiredSkills,
            Categories = createRequest.Categories,
            Status = ProjectStatus.Published,
            OrganizationId = org.Id
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/projects/{projectId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

       
        var completeResponse = await _client.PatchAsync($"/api/projects/{projectId}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

       
        var getResponse = await _client.GetAsync($"/api/projects/{projectId}");
        var finalProject = await getResponse.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        finalProject!.Data!.Status.Should().Be(ProjectStatus.Completed);
        finalProject.Data.Title.Should().Be("Updated Lifecycle Project");
    }

    
}
