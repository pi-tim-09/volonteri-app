using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Repositories;

namespace WebApp.UnitTests.Repositories;

public class ApplicationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ApplicationRepository _repository;

    public ApplicationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ApplicationRepositoryTests-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ApplicationRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetApplicationsByVolunteerAsync_ReturnsApplicationsForSpecificVolunteer()
    {
        // Arrange
        var volunteer1 = new Volunteer { Id = 1, Email = "v1@example.com", FirstName = "V1", LastName = "L1", PhoneNumber = "1" };
        var volunteer2 = new Volunteer { Id = 2, Email = "v2@example.com", FirstName = "V2", LastName = "L2", PhoneNumber = "2" };
        var org = new Organization { Id = 3, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "3", OrganizationName = "Test Org" };
        
        _context.Volunteers.AddRange(volunteer1, volunteer2);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var app1 = new Application { Id = 1, VolunteerId = volunteer1.Id, ProjectId = project.Id, Status = ApplicationStatus.Pending };
        var app2 = new Application { Id = 2, VolunteerId = volunteer1.Id, ProjectId = project.Id, Status = ApplicationStatus.Accepted };
        var app3 = new Application { Id = 3, VolunteerId = volunteer2.Id, ProjectId = project.Id, Status = ApplicationStatus.Pending };
        
        _context.Applications.AddRange(app1, app2, app3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetApplicationsByVolunteerAsync(volunteer1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.VolunteerId == volunteer1.Id);
        result.Should().Contain(a => a.Id == app1.Id);
        result.Should().Contain(a => a.Id == app2.Id);
    }

    [Fact]
    public async Task GetApplicationsByProjectAsync_ReturnsApplicationsForSpecificProject()
    {
        // Arrange
        var volunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "1" };
        var org = new Organization { Id = 2, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "2", OrganizationName = "Test Org" };
        
        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project1 = new Project { Id = 1, Title = "Project 1", Description = "Desc1", OrganizationId = org.Id, MaxVolunteers = 10 };
        var project2 = new Project { Id = 2, Title = "Project 2", Description = "Desc2", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        var app1 = new Application { Id = 1, VolunteerId = volunteer.Id, ProjectId = project1.Id, Status = ApplicationStatus.Pending };
        var app2 = new Application { Id = 2, VolunteerId = volunteer.Id, ProjectId = project1.Id, Status = ApplicationStatus.Accepted };
        var app3 = new Application { Id = 3, VolunteerId = volunteer.Id, ProjectId = project2.Id, Status = ApplicationStatus.Pending };
        
        _context.Applications.AddRange(app1, app2, app3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetApplicationsByProjectAsync(project1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.ProjectId == project1.Id);
        result.Should().Contain(a => a.Id == app1.Id);
        result.Should().Contain(a => a.Id == app2.Id);
    }

    [Fact]
    public async Task GetApplicationWithDetailsAsync_IncludesVolunteerAndProjectDetails()
    {
        // Arrange
        var volunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "1" };
        var org = new Organization { Id = 2, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "2", OrganizationName = "Test Org" };
        
        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var application = new Application { Id = 1, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Pending };
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetApplicationWithDetailsAsync(application.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Volunteer.Should().NotBeNull();
        result.Volunteer.Id.Should().Be(volunteer.Id);
        result.Project.Should().NotBeNull();
        result.Project.Id.Should().Be(project.Id);
        result.Project.Organization.Should().NotBeNull();
        result.Project.Organization.Id.Should().Be(org.Id);
    }

    [Fact]
    public async Task GetApplicationWithDetailsAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetApplicationWithDetailsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingApplicationsAsync_ReturnsOnlyPendingApplications()
    {
        // Arrange
        var volunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "1" };
        var org = new Organization { Id = 2, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "2", OrganizationName = "Test Org" };
        
        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var app1 = new Application { Id = 1, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Pending };
        var app2 = new Application { Id = 2, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Accepted };
        var app3 = new Application { Id = 3, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Pending };
        
        _context.Applications.AddRange(app1, app2, app3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingApplicationsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.Status == ApplicationStatus.Pending);
        result.Should().Contain(a => a.Id == app1.Id);
        result.Should().Contain(a => a.Id == app3.Id);
    }

    [Fact]
    public async Task GetApplicationsByStatusAsync_ReturnsApplicationsWithSpecificStatus()
    {
        // Arrange
        var volunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "1" };
        var org = new Organization { Id = 2, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "2", OrganizationName = "Test Org" };
        
        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var app1 = new Application { Id = 1, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Accepted };
        var app2 = new Application { Id = 2, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Rejected };
        var app3 = new Application { Id = 3, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Accepted };
        
        _context.Applications.AddRange(app1, app2, app3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetApplicationsByStatusAsync(ApplicationStatus.Accepted);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.Status == ApplicationStatus.Accepted);
        result.Should().Contain(a => a.Id == app1.Id);
        result.Should().Contain(a => a.Id == app3.Id);
    }

    [Fact]
    public async Task HasVolunteerAppliedAsync_WhenVolunteerApplied_ReturnsTrue()
    {
        // Arrange
        var volunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "1" };
        var org = new Organization { Id = 2, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "2", OrganizationName = "Test Org" };
        
        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var application = new Application { Id = 1, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Pending };
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasVolunteerAppliedAsync(volunteer.Id, project.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasVolunteerAppliedAsync_WhenVolunteerNotApplied_ReturnsFalse()
    {
        // Arrange
        var volunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "1" };
        var org = new Organization { Id = 2, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "2", OrganizationName = "Test Org" };
        
        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasVolunteerAppliedAsync(volunteer.Id, project.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAcceptedApplicationsCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var volunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "1" };
        var org = new Organization { Id = 2, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "2", OrganizationName = "Test Org" };
        
        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var app1 = new Application { Id = 1, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Accepted };
        var app2 = new Application { Id = 2, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Pending };
        var app3 = new Application { Id = 3, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Accepted };
        var app4 = new Application { Id = 4, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Accepted };
        
        _context.Applications.AddRange(app1, app2, app3, app4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAcceptedApplicationsCountAsync(project.Id);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetAcceptedApplicationsCountAsync_WhenNoAcceptedApplications_ReturnsZero()
    {
        // Arrange
        var volunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "1" };
        var org = new Organization { Id = 2, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "2", OrganizationName = "Test Org" };
        
        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAcceptedApplicationsCountAsync(project.Id);

        // Assert
        result.Should().Be(0);
    }
}
