using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Common;
using WebApp.Data;
using WebApp.DTOs.Applications;
using WebApp.Models;

namespace WebApp.IntegrationTests;

public class ApplicationsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ApplicationsApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

   

    private async Task<Volunteer> CreateTestVolunteerInDb(string email = "volunteer@example.com")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var volunteer = new Volunteer
        {
            Email = email,
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "Volunteer",
            PhoneNumber = "123456789",
            City = "Test City",
            Role = UserRole.Volunteer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Volunteers.Add(volunteer);
        await context.SaveChangesAsync();
        return volunteer;
    }

    private async Task<Project> CreateTestProjectInDb(int organizationId = 1)
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
            Status = ProjectStatus.Published,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private async Task<Application> CreateTestApplicationInDb(int volunteerId, int projectId, ApplicationStatus status = ApplicationStatus.Pending)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var application = new Application
        {
            VolunteerId = volunteerId,
            ProjectId = projectId,
            Status = status,
            AppliedAt = DateTime.UtcNow
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();
        return application;
    }

    

    

    [Fact]
    public async Task GetApplications_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/applications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApplications_ReturnsValidApiResponse()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("app1@example.com");
        var project = await CreateTestProjectInDb();
        await CreateTestApplicationInDb(volunteer.Id, project.Id);

        // Act
        var response = await _client.GetAsync("/api/applications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ApplicationListDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Applications.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetApplications_FilterByProjectId_ReturnsFilteredResults()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("filtertest@example.com");
        var project1 = await CreateTestProjectInDb();
        var project2 = await CreateTestProjectInDb();
        await CreateTestApplicationInDb(volunteer.Id, project1.Id);

        // Act
        var response = await _client.GetAsync($"/api/applications?projectId={project1.Id}");

        // Assert
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ApplicationListDto>>();
        apiResponse!.Data!.Applications.Should().OnlyContain(a => a.ProjectId == project1.Id);
    }

    [Fact]
    public async Task GetApplications_FilterByStatus_ReturnsFilteredResults()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("statusfilter@example.com");
        var project = await CreateTestProjectInDb();
        await CreateTestApplicationInDb(volunteer.Id, project.Id, ApplicationStatus.Pending);

        // Act
        var response = await _client.GetAsync($"/api/applications?status={ApplicationStatus.Pending}");

        // Assert
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ApplicationListDto>>();
        apiResponse!.Data!.Applications.Should().OnlyContain(a => a.Status == ApplicationStatus.Pending);
    }

    

    

    [Fact]
    public async Task GetApplication_WhenExists_Returns200()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb();
        var project = await CreateTestProjectInDb();
        var application = await CreateTestApplicationInDb(volunteer.Id, project.Id);

        // Act
        var response = await _client.GetAsync($"/api/applications/{application.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>();
        apiResponse!.Data!.Id.Should().Be(application.Id);
        apiResponse.Data.VolunteerId.Should().Be(volunteer.Id);
        apiResponse.Data.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task GetApplication_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/applications/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

  

    

    [Fact]
    public async Task CreateApplication_WhenValid_Returns201()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("newapp@example.com");
        var project = await CreateTestProjectInDb();
        
        var request = new CreateApplicationRequest
        {
            VolunteerId = volunteer.Id,
            ProjectId = project.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>();
        apiResponse!.Data!.VolunteerId.Should().Be(volunteer.Id);
        apiResponse.Data.ProjectId.Should().Be(project.Id);
        apiResponse.Data.Status.Should().Be(ApplicationStatus.Pending);
    }

    [Fact]
    public async Task CreateApplication_WhenProjectFull_Returns400()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("fullproject@example.com");
        var project = await CreateTestProjectInDb();
        
        // Fill the project
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var proj = await context.Projects.FindAsync(project.Id);
            proj!.CurrentVolunteers = proj.MaxVolunteers;
            await context.SaveChangesAsync();
        }

        var request = new CreateApplicationRequest
        {
            VolunteerId = volunteer.Id,
            ProjectId = project.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_WhenAlreadyApplied_Returns400()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("duplicate@example.com");
        var project = await CreateTestProjectInDb();
        await CreateTestApplicationInDb(volunteer.Id, project.Id);

        var request = new CreateApplicationRequest
        {
            VolunteerId = volunteer.Id,
            ProjectId = project.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    

   

    [Fact]
    public async Task ApproveApplication_WhenValid_Returns200()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("approve@example.com");
        var project = await CreateTestProjectInDb();
        var application = await CreateTestApplicationInDb(volunteer.Id, project.Id);

        var request = new ReviewApplicationRequest
        {
            Status = ApplicationStatus.Accepted,
            ReviewNotes = "Approved"
        };

        // Act
        var response = await _client.PatchAsync($"/api/applications/{application.Id}/approve", JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        
        var updatedApp = await context.Applications.FindAsync(application.Id);
        updatedApp!.Status.Should().Be(ApplicationStatus.Accepted);
        
        var updatedProject = await context.Projects.FindAsync(project.Id);
        updatedProject!.CurrentVolunteers.Should().Be(1);
    }

    [Fact]
    public async Task ApproveApplication_WhenAlreadyApproved_Returns400()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("alreadyapproved@example.com");
        var project = await CreateTestProjectInDb();
        var application = await CreateTestApplicationInDb(volunteer.Id, project.Id, ApplicationStatus.Accepted);

        var request = new ReviewApplicationRequest
        {
            Status = ApplicationStatus.Accepted,
            ReviewNotes = "Already approved"
        };

        // Act
        var response = await _client.PatchAsync($"/api/applications/{application.Id}/approve", JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

   

    

    [Fact]
    public async Task RejectApplication_WhenValid_Returns200()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("reject@example.com");
        var project = await CreateTestProjectInDb();
        var application = await CreateTestApplicationInDb(volunteer.Id, project.Id);

        var request = new ReviewApplicationRequest
        {
            Status = ApplicationStatus.Rejected,
            ReviewNotes = "Not qualified"
        };

        // Act
        var response = await _client.PatchAsync($"/api/applications/{application.Id}/reject", JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status changed
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        
        var updatedApp = await context.Applications.FindAsync(application.Id);
        updatedApp!.Status.Should().Be(ApplicationStatus.Rejected);
    }

    

    

    [Fact]
    public async Task WithdrawApplication_WhenPending_Returns200()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("withdraw@example.com");
        var project = await CreateTestProjectInDb();
        var application = await CreateTestApplicationInDb(volunteer.Id, project.Id);

        // Act
        var response = await _client.PatchAsync($"/api/applications/{application.Id}/withdraw?volunteerId={volunteer.Id}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.ChangeTracker.Clear();
        
        var updatedApp = await context.Applications.FindAsync(application.Id);
        updatedApp!.Status.Should().Be(ApplicationStatus.Withdrawn);
    }

    [Fact]
    public async Task WithdrawApplication_WhenAccepted_DecrementsVolunteerCount()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("withdrawaccepted@example.com");
        var project = await CreateTestProjectInDb();
        var application = await CreateTestApplicationInDb(volunteer.Id, project.Id, ApplicationStatus.Accepted);
        
        
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var proj = await context.Projects.FindAsync(project.Id);
            proj!.CurrentVolunteers = 1;
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.PatchAsync($"/api/applications/{application.Id}/withdraw?volunteerId={volunteer.Id}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        verifyContext.ChangeTracker.Clear();
        
        var updatedProject = await verifyContext.Projects.FindAsync(project.Id);
        updatedProject!.CurrentVolunteers.Should().Be(0);
    }

    

    

    [Fact]
    public async Task DeleteApplication_WhenExists_Returns200()
    {
        // Arrange
        var volunteer = await CreateTestVolunteerInDb("delete@example.com");
        var project = await CreateTestProjectInDb();
        var application = await CreateTestApplicationInDb(volunteer.Id, project.Id);

        // Act
        var response = await _client.DeleteAsync($"/api/applications/{application.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deleted = await context.Applications.FindAsync(application.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteApplication_WhenNotExists_Returns404()
    {
        // Act
        var response = await _client.DeleteAsync("/api/applications/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

  

    

    [Fact]
    public async Task CompleteApplicationLifecycle_WorksEndToEnd()
    {
        
        var volunteer = await CreateTestVolunteerInDb($"lifecycle{Guid.NewGuid():N}@example.com");
        var project = await CreateTestProjectInDb();

      
        var createRequest = new CreateApplicationRequest
        {
            VolunteerId = volunteer.Id,
            ProjectId = project.Id
        };

        var createResponse = await _client.PostAsJsonAsync("/api/applications", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdApp = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>();
        var appId = createdApp!.Data!.Id;

        
        createdApp.Data.Status.Should().Be(ApplicationStatus.Pending);

       
        var approveRequest = new ReviewApplicationRequest
        {
            Status = ApplicationStatus.Accepted,
            ReviewNotes = "Great candidate"
        };

        var approveResponse = await _client.PatchAsync($"/api/applications/{appId}/approve", JsonContent.Create(approveRequest));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var app = await context.Applications.FindAsync(appId);
            app!.Status.Should().Be(ApplicationStatus.Accepted);
            
            var proj = await context.Projects.FindAsync(project.Id);
            proj!.CurrentVolunteers.Should().Be(1);
        }

        
        var withdrawResponse = await _client.PatchAsync($"/api/applications/{appId}/withdraw?volunteerId={volunteer.Id}", null);
        withdrawResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.ChangeTracker.Clear();
            
            var app = await context.Applications.FindAsync(appId);
            app!.Status.Should().Be(ApplicationStatus.Withdrawn);
            
            var proj = await context.Projects.FindAsync(project.Id);
            proj!.CurrentVolunteers.Should().Be(0);
        }
    }

   
}
