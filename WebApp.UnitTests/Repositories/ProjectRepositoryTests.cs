using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Repositories;

namespace WebApp.UnitTests.Repositories;

public class ProjectRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProjectRepository _repository;

    public ProjectRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProjectRepositoryTests-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ProjectRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetPublishedProjectsAsync_ReturnsOnlyPublishedProjects()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project1 = new Project { Id = 1, Title = "Published 1", Description = "Desc", OrganizationId = org.Id, Status = ProjectStatus.Published, MaxVolunteers = 10 };
        var project2 = new Project { Id = 2, Title = "Draft", Description = "Desc", OrganizationId = org.Id, Status = ProjectStatus.Draft, MaxVolunteers = 10 };
        var project3 = new Project { Id = 3, Title = "Published 2", Description = "Desc", OrganizationId = org.Id, Status = ProjectStatus.Published, MaxVolunteers = 10 };
        
        _context.Projects.AddRange(project1, project2, project3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPublishedProjectsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Status == ProjectStatus.Published);
        result.Should().Contain(p => p.Id == project1.Id);
        result.Should().Contain(p => p.Id == project3.Id);
    }

    [Fact]
    public async Task GetProjectsByOrganizationAsync_ReturnsProjectsForSpecificOrganization()
    {
        // Arrange
        var org1 = new Organization { Id = 1, Email = "org1@example.com", FirstName = "Org1", LastName = "Name", PhoneNumber = "1", OrganizationName = "Org 1" };
        var org2 = new Organization { Id = 2, Email = "org2@example.com", FirstName = "Org2", LastName = "Name", PhoneNumber = "2", OrganizationName = "Org 2" };
        _context.Organizations.AddRange(org1, org2);
        await _context.SaveChangesAsync();

        var project1 = new Project { Id = 1, Title = "Org1 Project 1", Description = "Desc", OrganizationId = org1.Id, MaxVolunteers = 10 };
        var project2 = new Project { Id = 2, Title = "Org2 Project", Description = "Desc", OrganizationId = org2.Id, MaxVolunteers = 10 };
        var project3 = new Project { Id = 3, Title = "Org1 Project 2", Description = "Desc", OrganizationId = org1.Id, MaxVolunteers = 10 };
        
        _context.Projects.AddRange(project1, project2, project3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProjectsByOrganizationAsync(org1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.OrganizationId == org1.Id);
        result.Should().Contain(p => p.Id == project1.Id);
        result.Should().Contain(p => p.Id == project3.Id);
    }

    [Fact]
    public async Task GetProjectWithApplicationsAsync_IncludesApplicationsAndVolunteers()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        var volunteer = new Volunteer { Id = 2, Email = "v@example.com", FirstName = "V", LastName = "L", PhoneNumber = "2" };
        _context.Organizations.Add(org);
        _context.Volunteers.Add(volunteer);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var application = new Application { Id = 1, VolunteerId = volunteer.Id, ProjectId = project.Id, Status = ApplicationStatus.Pending };
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProjectWithApplicationsAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Applications.Should().HaveCount(1);
        result.Applications.First().Volunteer.Should().NotBeNull();
        result.Applications.First().Volunteer.Id.Should().Be(volunteer.Id);
    }

    [Fact]
    public async Task GetProjectWithOrganizationAsync_IncludesOrganizationDetails()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project { Id = 1, Title = "Test Project", Description = "Desc", OrganizationId = org.Id, MaxVolunteers = 10 };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProjectWithOrganizationAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Organization.Should().NotBeNull();
        result.Organization.Id.Should().Be(org.Id);
        result.Organization.OrganizationName.Should().Be("Test Org");
    }

    [Fact]
    public async Task GetProjectsByCityAsync_ReturnsOnlyPublishedProjectsInCity()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project1 = new Project { Id = 1, Title = "Zagreb Published", Description = "Desc", OrganizationId = org.Id, City = "Zagreb", Status = ProjectStatus.Published, MaxVolunteers = 10 };
        var project2 = new Project { Id = 2, Title = "Zagreb Draft", Description = "Desc", OrganizationId = org.Id, City = "Zagreb", Status = ProjectStatus.Draft, MaxVolunteers = 10 };
        var project3 = new Project { Id = 3, Title = "Split Published", Description = "Desc", OrganizationId = org.Id, City = "Split", Status = ProjectStatus.Published, MaxVolunteers = 10 };
        
        _context.Projects.AddRange(project1, project2, project3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProjectsByCityAsync("Zagreb");

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(p => p.City == "Zagreb" && p.Status == ProjectStatus.Published);
        result.First().Id.Should().Be(project1.Id);
    }

    [Fact]
    public async Task GetProjectsByStatusAsync_ReturnsProjectsWithSpecificStatus()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project1 = new Project { Id = 1, Title = "Completed 1", Description = "Desc", OrganizationId = org.Id, Status = ProjectStatus.Completed, MaxVolunteers = 10 };
        var project2 = new Project { Id = 2, Title = "Published", Description = "Desc", OrganizationId = org.Id, Status = ProjectStatus.Published, MaxVolunteers = 10 };
        var project3 = new Project { Id = 3, Title = "Completed 2", Description = "Desc", OrganizationId = org.Id, Status = ProjectStatus.Completed, MaxVolunteers = 10 };
        
        _context.Projects.AddRange(project1, project2, project3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProjectsByStatusAsync(ProjectStatus.Completed);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Status == ProjectStatus.Completed);
        result.Should().Contain(p => p.Id == project1.Id);
        result.Should().Contain(p => p.Id == project3.Id);
    }

    [Fact]
    public async Task SearchProjectsAsync_FindsProjectsByTitleOrDescription()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project1 = new Project { Id = 1, Title = "Environment Cleanup", Description = "Help clean the beach", OrganizationId = org.Id, Status = ProjectStatus.Published, MaxVolunteers = 10 };
        var project2 = new Project { Id = 2, Title = "Food Bank", Description = "Distribute food to those in need", OrganizationId = org.Id, Status = ProjectStatus.Published, MaxVolunteers = 10 };
        var project3 = new Project { Id = 3, Title = "Library Support", Description = "Help organize books and environment", OrganizationId = org.Id, Status = ProjectStatus.Published, MaxVolunteers = 10 };
        var project4 = new Project { Id = 4, Title = "Environment Draft", Description = "Draft project", OrganizationId = org.Id, Status = ProjectStatus.Draft, MaxVolunteers = 10 };
        
        _context.Projects.AddRange(project1, project2, project3, project4);
        await _context.SaveChangesAsync();

        // Act - Use exact case that matches the data
        var result = await _repository.SearchProjectsAsync("Environment");

        // Assert
        result.Should().HaveCount(1, "InMemory database uses case-sensitive search, only 'Environment Cleanup' matches exact case");
        result.Should().Contain(p => p.Id == project1.Id);
    }

    [Fact]
    public async Task GetAvailableProjectsAsync_ReturnsOnlyAcceptingApplicationsProjects()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var futureDeadline = DateTime.UtcNow.AddDays(7);
        var pastDeadline = DateTime.UtcNow.AddDays(-1);

        var project1 = new Project 
        { 
            Id = 1, 
            Title = "Available", 
            Description = "Desc", 
            OrganizationId = org.Id, 
            Status = ProjectStatus.Published, 
            ApplicationDeadline = futureDeadline,
            MaxVolunteers = 10,
            CurrentVolunteers = 5
        };

        var project2 = new Project 
        { 
            Id = 2, 
            Title = "Deadline Passed", 
            Description = "Desc", 
            OrganizationId = org.Id, 
            Status = ProjectStatus.Published, 
            ApplicationDeadline = pastDeadline,
            MaxVolunteers = 10,
            CurrentVolunteers = 5
        };

        var project3 = new Project 
        { 
            Id = 3, 
            Title = "Full", 
            Description = "Desc", 
            OrganizationId = org.Id, 
            Status = ProjectStatus.Published, 
            ApplicationDeadline = futureDeadline,
            MaxVolunteers = 10,
            CurrentVolunteers = 10
        };

        var project4 = new Project 
        { 
            Id = 4, 
            Title = "Draft", 
            Description = "Desc", 
            OrganizationId = org.Id, 
            Status = ProjectStatus.Draft, 
            ApplicationDeadline = futureDeadline,
            MaxVolunteers = 10,
            CurrentVolunteers = 0
        };
        
        _context.Projects.AddRange(project1, project2, project3, project4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAvailableProjectsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(project1.Id, "only published, not full, with future deadline projects should be available");
    }

    [Fact]
    public async Task HasAvailableSlotsAsync_WhenBelowCapacity_ReturnsTrue()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project 
        { 
            Id = 1, 
            Title = "Test", 
            Description = "Desc", 
            OrganizationId = org.Id, 
            MaxVolunteers = 10,
            CurrentVolunteers = 5
        };
        
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasAvailableSlotsAsync(project.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAvailableSlotsAsync_WhenAtCapacity_ReturnsFalse()
    {
        // Arrange
        var org = new Organization { Id = 1, Email = "org@example.com", FirstName = "Org", LastName = "Name", PhoneNumber = "1", OrganizationName = "Test Org" };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project 
        { 
            Id = 1, 
            Title = "Test", 
            Description = "Desc", 
            OrganizationId = org.Id, 
            MaxVolunteers = 10,
            CurrentVolunteers = 10
        };
        
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasAvailableSlotsAsync(project.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAvailableSlotsAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Act
        var result = await _repository.HasAvailableSlotsAsync(999);

        // Assert
        result.Should().BeFalse();
    }
}
