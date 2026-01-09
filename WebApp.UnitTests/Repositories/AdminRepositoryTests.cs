using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Repositories;

namespace WebApp.UnitTests.Repositories;

public class AdminRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AdminRepository _repository;

    public AdminRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"AdminRepositoryTests-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new AdminRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAdminsByDepartmentAsync_ReturnsOnlyActiveAdminsInDepartment()
    {
        // Arrange
        var admin1 = new Admin
        {
            Id = 1,
            Email = "admin1@example.com",
            FirstName = "Admin",
            LastName = "One",
            PhoneNumber = "1",
            Department = "IT",
            IsActive = true
        };

        var admin2 = new Admin
        {
            Id = 2,
            Email = "admin2@example.com",
            FirstName = "Admin",
            LastName = "Two",
            PhoneNumber = "2",
            Department = "IT",
            IsActive = true
        };

        var admin3 = new Admin
        {
            Id = 3,
            Email = "admin3@example.com",
            FirstName = "Admin",
            LastName = "Three",
            PhoneNumber = "3",
            Department = "IT",
            IsActive = false
        };

        var admin4 = new Admin
        {
            Id = 4,
            Email = "admin4@example.com",
            FirstName = "Admin",
            LastName = "Four",
            PhoneNumber = "4",
            Department = "HR",
            IsActive = true
        };

        _context.Admins.AddRange(admin1, admin2, admin3, admin4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAdminsByDepartmentAsync("IT");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.Department == "IT" && a.IsActive);
        result.Should().Contain(a => a.Id == admin1.Id);
        result.Should().Contain(a => a.Id == admin2.Id);
        result.Should().NotContain(a => a.Id == admin3.Id, "inactive admins should be excluded");
    }

    [Fact]
    public async Task GetAdminsByDepartmentAsync_WhenNoneFound_ReturnsEmptyCollection()
    {
        // Arrange
        var admin = new Admin
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "One",
            PhoneNumber = "1",
            Department = "IT",
            IsActive = true
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAdminsByDepartmentAsync("HR");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdminWithPermissionsAsync_ReturnsAdminWithPermissions()
    {
        // Arrange
        var admin = new Admin
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1",
            Department = "IT",
            CanManageUsers = true,
            CanManageOrganizations = false,
            CanManageProjects = true,
            IsActive = true
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAdminWithPermissionsAsync(admin.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(admin.Id);
        result.CanManageUsers.Should().BeTrue();
        result.CanManageOrganizations.Should().BeFalse();
        result.CanManageProjects.Should().BeTrue();
    }

    [Fact]
    public async Task GetAdminWithPermissionsAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetAdminWithPermissionsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CanManageUsersAsync_WhenAdminHasPermission_ReturnsTrue()
    {
        // Arrange
        var admin = new Admin
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1",
            Department = "IT",
            CanManageUsers = true,
            IsActive = true
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CanManageUsersAsync(admin.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageUsersAsync_WhenAdminDoesNotHavePermission_ReturnsFalse()
    {
        // Arrange
        var admin = new Admin
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1",
            Department = "IT",
            CanManageUsers = false,
            IsActive = true
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CanManageUsersAsync(admin.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageUsersAsync_WhenAdminNotFound_ReturnsFalse()
    {
        // Act
        var result = await _repository.CanManageUsersAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageOrganizationsAsync_WhenAdminHasPermission_ReturnsTrue()
    {
        // Arrange
        var admin = new Admin
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1",
            Department = "IT",
            CanManageOrganizations = true,
            IsActive = true
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CanManageOrganizationsAsync(admin.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageOrganizationsAsync_WhenAdminDoesNotHavePermission_ReturnsFalse()
    {
        // Arrange
        var admin = new Admin
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1",
            Department = "IT",
            CanManageOrganizations = false,
            IsActive = true
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CanManageOrganizationsAsync(admin.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageOrganizationsAsync_WhenAdminNotFound_ReturnsFalse()
    {
        // Act
        var result = await _repository.CanManageOrganizationsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageProjectsAsync_WhenAdminHasPermission_ReturnsTrue()
    {
        // Arrange
        var admin = new Admin
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1",
            Department = "IT",
            CanManageProjects = true,
            IsActive = true
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CanManageProjectsAsync(admin.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageProjectsAsync_WhenAdminDoesNotHavePermission_ReturnsFalse()
    {
        // Arrange
        var admin = new Admin
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1",
            Department = "IT",
            CanManageProjects = false,
            IsActive = true
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CanManageProjectsAsync(admin.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageProjectsAsync_WhenAdminNotFound_ReturnsFalse()
    {
        // Act
        var result = await _repository.CanManageProjectsAsync(999);

        // Assert
        result.Should().BeFalse();
    }
}
