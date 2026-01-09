using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Repositories;

namespace WebApp.UnitTests.Repositories;

public class VolunteerRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly VolunteerRepository _repository;

    public VolunteerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"VolunteerRepositoryTests-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new VolunteerRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetVolunteersBySkillAsync_ReturnsVolunteersWithSpecificSkill()
    {
        // Arrange
        var volunteer1 = new Volunteer
        {
            Id = 1,
            Email = "v1@example.com",
            FirstName = "V1",
            LastName = "L1",
            PhoneNumber = "1",
            Skills = new List<string> { "C#", "ASP.NET", "SQL" }
        };

        var volunteer2 = new Volunteer
        {
            Id = 2,
            Email = "v2@example.com",
            FirstName = "V2",
            LastName = "L2",
            PhoneNumber = "2",
            Skills = new List<string> { "JavaScript", "React" }
        };

        var volunteer3 = new Volunteer
        {
            Id = 3,
            Email = "v3@example.com",
            FirstName = "V3",
            LastName = "L3",
            PhoneNumber = "3",
            Skills = new List<string> { "C#", "Azure" }
        };

        _context.Volunteers.AddRange(volunteer1, volunteer2, volunteer3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetVolunteersBySkillAsync("C#");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(v => v.Id == volunteer1.Id);
        result.Should().Contain(v => v.Id == volunteer3.Id);
        result.Should().NotContain(v => v.Id == volunteer2.Id);
    }

    [Fact]
    public async Task GetVolunteersBySkillAsync_WhenNoMatch_ReturnsEmptyCollection()
    {
        // Arrange
        var volunteer = new Volunteer
        {
            Id = 1,
            Email = "v@example.com",
            FirstName = "V",
            LastName = "L",
            PhoneNumber = "1",
            Skills = new List<string> { "C#", "ASP.NET" }
        };

        _context.Volunteers.Add(volunteer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetVolunteersBySkillAsync("Python");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVolunteersByCityAsync_ReturnsOnlyActiveVolunteersInCity()
    {
        // Arrange
        var volunteer1 = new Volunteer
        {
            Id = 1,
            Email = "v1@example.com",
            FirstName = "V1",
            LastName = "L1",
            PhoneNumber = "1",
            City = "Zagreb",
            IsActive = true
        };

        var volunteer2 = new Volunteer
        {
            Id = 2,
            Email = "v2@example.com",
            FirstName = "V2",
            LastName = "L2",
            PhoneNumber = "2",
            City = "Zagreb",
            IsActive = false
        };

        var volunteer3 = new Volunteer
        {
            Id = 3,
            Email = "v3@example.com",
            FirstName = "V3",
            LastName = "L3",
            PhoneNumber = "3",
            City = "Split",
            IsActive = true
        };

        _context.Volunteers.AddRange(volunteer1, volunteer2, volunteer3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetVolunteersByCityAsync("Zagreb");

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(v => v.City == "Zagreb" && v.IsActive);
        result.First().Id.Should().Be(volunteer1.Id);
    }

    [Fact]
    public async Task GetVolunteerWithApplicationsAsync_IncludesApplicationsAndProjects()
    {
        // Arrange
        var volunteer = new Volunteer
        {
            Id = 1,
            Email = "v@example.com",
            FirstName = "V",
            LastName = "L",
            PhoneNumber = "1"
        };

        var org = new Organization
        {
            Id = 2,
            Email = "org@example.com",
            FirstName = "Org",
            LastName = "Name",
            PhoneNumber = "2",
            OrganizationName = "Test Org"
        };

        _context.Volunteers.Add(volunteer);
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Description = "Desc",
            OrganizationId = org.Id,
            MaxVolunteers = 10
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var application = new Application
        {
            Id = 1,
            VolunteerId = volunteer.Id,
            ProjectId = project.Id,
            Status = ApplicationStatus.Pending
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetVolunteerWithApplicationsAsync(volunteer.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Applications.Should().HaveCount(1);
        result.Applications.First().Project.Should().NotBeNull();
        result.Applications.First().Project.Id.Should().Be(project.Id);
    }

    [Fact]
    public async Task GetVolunteerWithApplicationsAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetVolunteerWithApplicationsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveVolunteersAsync_ReturnsOnlyActiveVolunteers()
    {
        // Arrange
        var volunteer1 = new Volunteer
        {
            Id = 1,
            Email = "v1@example.com",
            FirstName = "V1",
            LastName = "L1",
            PhoneNumber = "1",
            IsActive = true
        };

        var volunteer2 = new Volunteer
        {
            Id = 2,
            Email = "v2@example.com",
            FirstName = "V2",
            LastName = "L2",
            PhoneNumber = "2",
            IsActive = false
        };

        var volunteer3 = new Volunteer
        {
            Id = 3,
            Email = "v3@example.com",
            FirstName = "V3",
            LastName = "L3",
            PhoneNumber = "3",
            IsActive = true
        };

        _context.Volunteers.AddRange(volunteer1, volunteer2, volunteer3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveVolunteersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.IsActive);
        result.Should().Contain(v => v.Id == volunteer1.Id);
        result.Should().Contain(v => v.Id == volunteer3.Id);
        result.Should().NotContain(v => v.Id == volunteer2.Id);
    }

    [Fact]
    public async Task GetTotalVolunteerHoursAsync_ReturnsCorrectHours()
    {
        // Arrange
        var volunteer = new Volunteer
        {
            Id = 1,
            Email = "v@example.com",
            FirstName = "V",
            LastName = "L",
            PhoneNumber = "1",
            VolunteerHours = 150
        };

        _context.Volunteers.Add(volunteer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTotalVolunteerHoursAsync(volunteer.Id);

        // Assert
        result.Should().Be(150);
    }

    [Fact]
    public async Task GetTotalVolunteerHoursAsync_WhenNotFound_ReturnsZero()
    {
        // Act
        var result = await _repository.GetTotalVolunteerHoursAsync(999);

        // Assert
        result.Should().Be(0);
    }
}
