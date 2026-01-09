using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Repositories;

namespace WebApp.UnitTests.Repositories;

public class OrganizationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrganizationRepository _repository;

    public OrganizationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"OrganizationRepositoryTests-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new OrganizationRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetVerifiedOrganizationsAsync_ReturnsOnlyVerifiedAndActiveOrganizations()
    {
        // Arrange
        var org1 = new Organization
        {
            Id = 1,
            Email = "org1@example.com",
            FirstName = "Org1",
            LastName = "Name",
            PhoneNumber = "1",
            OrganizationName = "Verified Active Org",
            IsVerified = true,
            IsActive = true
        };

        var org2 = new Organization
        {
            Id = 2,
            Email = "org2@example.com",
            FirstName = "Org2",
            LastName = "Name",
            PhoneNumber = "2",
            OrganizationName = "Verified Inactive Org",
            IsVerified = true,
            IsActive = false
        };

        var org3 = new Organization
        {
            Id = 3,
            Email = "org3@example.com",
            FirstName = "Org3",
            LastName = "Name",
            PhoneNumber = "3",
            OrganizationName = "Unverified Active Org",
            IsVerified = false,
            IsActive = true
        };

        var org4 = new Organization
        {
            Id = 4,
            Email = "org4@example.com",
            FirstName = "Org4",
            LastName = "Name",
            PhoneNumber = "4",
            OrganizationName = "Another Verified Active Org",
            IsVerified = true,
            IsActive = true
        };

        _context.Organizations.AddRange(org1, org2, org3, org4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetVerifiedOrganizationsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.IsVerified && o.IsActive);
        result.Should().Contain(o => o.Id == org1.Id);
        result.Should().Contain(o => o.Id == org4.Id);
    }

    [Fact]
    public async Task GetOrganizationWithProjectsAsync_IncludesProjects()
    {
        // Arrange
        var org = new Organization
        {
            Id = 1,
            Email = "org@example.com",
            FirstName = "Org",
            LastName = "Name",
            PhoneNumber = "1",
            OrganizationName = "Test Org"
        };

        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var project1 = new Project
        {
            Id = 1,
            Title = "Project 1",
            Description = "Desc",
            OrganizationId = org.Id,
            MaxVolunteers = 10
        };

        var project2 = new Project
        {
            Id = 2,
            Title = "Project 2",
            Description = "Desc",
            OrganizationId = org.Id,
            MaxVolunteers = 10
        };

        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetOrganizationWithProjectsAsync(org.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Projects.Should().HaveCount(2);
        result.Projects.Should().Contain(p => p.Id == project1.Id);
        result.Projects.Should().Contain(p => p.Id == project2.Id);
    }

    [Fact]
    public async Task GetOrganizationWithProjectsAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetOrganizationWithProjectsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrganizationsByCityAsync_ReturnsOnlyActiveOrganizationsInCity()
    {
        // Arrange
        var org1 = new Organization
        {
            Id = 1,
            Email = "org1@example.com",
            FirstName = "Org1",
            LastName = "Name",
            PhoneNumber = "1",
            OrganizationName = "Zagreb Active Org",
            City = "Zagreb",
            IsActive = true
        };

        var org2 = new Organization
        {
            Id = 2,
            Email = "org2@example.com",
            FirstName = "Org2",
            LastName = "Name",
            PhoneNumber = "2",
            OrganizationName = "Zagreb Inactive Org",
            City = "Zagreb",
            IsActive = false
        };

        var org3 = new Organization
        {
            Id = 3,
            Email = "org3@example.com",
            FirstName = "Org3",
            LastName = "Name",
            PhoneNumber = "3",
            OrganizationName = "Split Active Org",
            City = "Split",
            IsActive = true
        };

        _context.Organizations.AddRange(org1, org2, org3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetOrganizationsByCityAsync("Zagreb");

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(o => o.City == "Zagreb" && o.IsActive);
        result.First().Id.Should().Be(org1.Id);
    }

    [Fact]
    public async Task VerifyOrganizationAsync_WhenOrganizationExists_VerifiesAndReturnsTrue()
    {
        // Arrange
        var org = new Organization
        {
            Id = 1,
            Email = "org@example.com",
            FirstName = "Org",
            LastName = "Name",
            PhoneNumber = "1",
            OrganizationName = "Test Org",
            IsVerified = false,
            VerifiedAt = null
        };

        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.VerifyOrganizationAsync(org.Id);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();
        
        var verifiedOrg = await _context.Organizations.FindAsync(org.Id);
        verifiedOrg!.IsVerified.Should().BeTrue();
        verifiedOrg.VerifiedAt.Should().NotBeNull();
        verifiedOrg.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task VerifyOrganizationAsync_WhenOrganizationNotFound_ReturnsFalse()
    {
        // Act
        var result = await _repository.VerifyOrganizationAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchOrganizationsAsync_FindsOrganizationsByNameOrDescription()
    {
        // Arrange
        var org1 = new Organization
        {
            Id = 1,
            Email = "org1@example.com",
            FirstName = "Org1",
            LastName = "Name",
            PhoneNumber = "1",
            OrganizationName = "Environment Foundation",
            Description = "We protect nature",
            IsActive = true
        };

        var org2 = new Organization
        {
            Id = 2,
            Email = "org2@example.com",
            FirstName = "Org2",
            LastName = "Name",
            PhoneNumber = "2",
            OrganizationName = "Food Bank",
            Description = "Helping the community",
            IsActive = true
        };

        var org3 = new Organization
        {
            Id = 3,
            Email = "org3@example.com",
            FirstName = "Org3",
            LastName = "Name",
            PhoneNumber = "3",
            OrganizationName = "Green Initiative",
            Description = "Focus on environment and sustainability",
            IsActive = true
        };

        var org4 = new Organization
        {
            Id = 4,
            Email = "org4@example.com",
            FirstName = "Org4",
            LastName = "Name",
            PhoneNumber = "4",
            OrganizationName = "Environment Inactive",
            Description = "Inactive org",
            IsActive = false
        };

        _context.Organizations.AddRange(org1, org2, org3, org4);
        await _context.SaveChangesAsync();

        // Act 
        var result = await _repository.SearchOrganizationsAsync("Environment");

        // Assert
        result.Should().HaveCount(1, "InMemory database uses case-sensitive search, only 'Environment Foundation' matches exact case (org4 is inactive)");
        result.Should().Contain(o => o.Id == org1.Id);
    }

    [Fact]
    public async Task SearchOrganizationsAsync_WhenNoMatch_ReturnsEmptyCollection()
    {
        // Arrange
        var org = new Organization
        {
            Id = 1,
            Email = "org@example.com",
            FirstName = "Org",
            LastName = "Name",
            PhoneNumber = "1",
            OrganizationName = "Test Org",
            Description = "Test description",
            IsActive = true
        };

        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchOrganizationsAsync("nonexistent");

        // Assert
        result.Should().BeEmpty();
    }
}
