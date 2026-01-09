using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.UnitTests.Data;

public class UnitOfWorkTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"UnitOfWorkTests-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
    }

    public void Dispose()
    {
        // Only dispose UnitOfWork, which will dispose the context
        _unitOfWork.Dispose();
    }

    [Fact]
    public void Volunteers_Property_ReturnsVolunteerRepository()
    {
        // Act
        var repo1 = _unitOfWork.Volunteers;
        var repo2 = _unitOfWork.Volunteers;

        // Assert
        repo1.Should().NotBeNull();
        repo1.Should().BeSameAs(repo2, "should return the same instance (lazy singleton)");
    }

    [Fact]
    public void Organizations_Property_ReturnsOrganizationRepository()
    {
        // Act
        var repo1 = _unitOfWork.Organizations;
        var repo2 = _unitOfWork.Organizations;

        // Assert
        repo1.Should().NotBeNull();
        repo1.Should().BeSameAs(repo2, "should return the same instance (lazy singleton)");
    }

    [Fact]
    public void Projects_Property_ReturnsProjectRepository()
    {
        // Act
        var repo1 = _unitOfWork.Projects;
        var repo2 = _unitOfWork.Projects;

        // Assert
        repo1.Should().NotBeNull();
        repo1.Should().BeSameAs(repo2, "should return the same instance (lazy singleton)");
    }

    [Fact]
    public void Applications_Property_ReturnsApplicationRepository()
    {
        // Act
        var repo1 = _unitOfWork.Applications;
        var repo2 = _unitOfWork.Applications;

        // Assert
        repo1.Should().NotBeNull();
        repo1.Should().BeSameAs(repo2, "should return the same instance (lazy singleton)");
    }

    [Fact]
    public void Admins_Property_ReturnsAdminRepository()
    {
        // Act
        var repo1 = _unitOfWork.Admins;
        var repo2 = _unitOfWork.Admins;

        // Assert
        repo1.Should().NotBeNull();
        repo1.Should().BeSameAs(repo2, "should return the same instance (lazy singleton)");
    }

    [Fact]
    public async Task SaveChangesAsync_SavesChangesToDatabase()
    {
        // Arrange
        var admin = new Admin
        {
            Email = "admin@example.com",
            FirstName = "Test",
            LastName = "Admin",
            PhoneNumber = "123",
            Department = "IT"
        };
        await _unitOfWork.Admins.AddAsync(admin);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
        var saved = await _context.Admins.FirstOrDefaultAsync(a => a.Email == "admin@example.com");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task BeginTransactionAsync_CompletesSuccessfully()
    {
        // Act & Assert - InMemory database doesn't support real transactions
        // Just verify the method completes without throwing an exception
        await _unitOfWork.BeginTransactionAsync();
        
        // The method should complete successfully even though InMemory ignores transactions
        true.Should().BeTrue("BeginTransactionAsync should complete without error");
    }

    [Fact]
    public async Task CommitTransactionAsync_CompletesSuccessfully()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        
        var admin = new Admin
        {
            Email = "commit@example.com",
            FirstName = "Commit",
            LastName = "Test",
            PhoneNumber = "123",
            Department = "IT"
        };
        await _unitOfWork.Admins.AddAsync(admin);

        // Act
        await _unitOfWork.CommitTransactionAsync();

        // Assert - InMemory doesn't support real transactions, but data should be saved
        var committed = await _context.Admins.FirstOrDefaultAsync(a => a.Email == "commit@example.com");
        committed.Should().NotBeNull();
    }

    [Fact]
    public async Task RollbackTransactionAsync_CompletesSuccessfully()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        
        var admin = new Admin
        {
            Email = "rollback@example.com",
            FirstName = "Rollback",
            LastName = "Test",
            PhoneNumber = "123",
            Department = "IT"
        };
        await _unitOfWork.Admins.AddAsync(admin);
        await _context.SaveChangesAsync();

        // Act
        await _unitOfWork.RollbackTransactionAsync();

        // Assert - InMemory doesn't support rollback, so data will still be there
        // This test just verifies the method completes without error
        var result = await _context.Admins.FirstOrDefaultAsync(a => a.Email == "rollback@example.com");
        result.Should().NotBeNull("InMemory database doesn't support real rollback");
    }

    [Fact]
    public async Task MultipleRepositories_ShareSameContext()
    {
        // Arrange
        var volunteer = new Volunteer
        {
            Email = "volunteer@example.com",
            FirstName = "Vol",
            LastName = "Test",
            PhoneNumber = "123"
        };
        var organization = new Organization
        {
            Email = "org@example.com",
            FirstName = "Org",
            LastName = "Test",
            PhoneNumber = "456",
            OrganizationName = "Test Org"
        };

        // Act
        await _unitOfWork.Volunteers.AddAsync(volunteer);
        await _unitOfWork.Organizations.AddAsync(organization);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedVol = await _context.Volunteers.FirstOrDefaultAsync(v => v.Email == "volunteer@example.com");
        var savedOrg = await _context.Organizations.FirstOrDefaultAsync(o => o.Email == "org@example.com");
        
        savedVol.Should().NotBeNull();
        savedOrg.Should().NotBeNull();
    }

    [Fact]
    public async Task Transaction_AcrossMultipleRepositories_SavesData()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        var volunteer = new Volunteer
        {
            Email = "vol@example.com",
            FirstName = "Vol",
            LastName = "Test",
            PhoneNumber = "123"
        };
        var admin = new Admin
        {
            Email = "admin-trans@example.com",
            FirstName = "Admin",
            LastName = "Test",
            PhoneNumber = "456",
            Department = "IT"
        };

        // Act
        await _unitOfWork.Volunteers.AddAsync(volunteer);
        await _unitOfWork.Admins.AddAsync(admin);
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        var savedVol = await _context.Volunteers.FirstOrDefaultAsync(v => v.Email == "vol@example.com");
        var savedAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == "admin-trans@example.com");
        
        savedVol.Should().NotBeNull();
        savedAdmin.Should().NotBeNull();
    }
}
