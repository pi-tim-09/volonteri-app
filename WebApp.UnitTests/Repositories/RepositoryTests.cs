using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Repositories;

namespace WebApp.UnitTests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Repository<Admin> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"RepositoryTests-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new Repository<Admin>(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        var admin = new Admin { Id = 1, Email = "test@example.com", FirstName = "Test", LastName = "User", PhoneNumber = "123" };
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var admins = new List<Admin>
        {
            new() { Id = 1, Email = "admin1@example.com", FirstName = "A1", LastName = "L1", PhoneNumber = "1" },
            new() { Id = 2, Email = "admin2@example.com", FirstName = "A2", LastName = "L2", PhoneNumber = "2" },
            new() { Id = 3, Email = "admin3@example.com", FirstName = "A3", LastName = "L3", PhoneNumber = "3" }
        };
        _context.Admins.AddRange(admins);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(a => admins.Any(x => x.Id == a.Id));
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyCollection()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_WithPredicate_ReturnsMatchingEntities()
    {
        // Arrange
        var admins = new List<Admin>
        {
            new() { Id = 1, Email = "admin1@example.com", FirstName = "A1", LastName = "L1", PhoneNumber = "1", Department = "IT" },
            new() { Id = 2, Email = "admin2@example.com", FirstName = "A2", LastName = "L2", PhoneNumber = "2", Department = "HR" },
            new() { Id = 3, Email = "admin3@example.com", FirstName = "A3", LastName = "L3", PhoneNumber = "3", Department = "IT" }
        };
        _context.Admins.AddRange(admins);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(a => a.Department == "IT");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.Department == "IT");
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsEmptyCollection()
    {
        // Arrange
        var admin = new Admin { Id = 1, Email = "admin@example.com", FirstName = "A", LastName = "L", PhoneNumber = "1", Department = "IT" };
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(a => a.Department == "Finance");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WhenMatch_ReturnsFirstEntity()
    {
        // Arrange
        var admins = new List<Admin>
        {
            new() { Id = 1, Email = "admin1@example.com", FirstName = "A1", LastName = "L1", PhoneNumber = "1", Department = "IT" },
            new() { Id = 2, Email = "admin2@example.com", FirstName = "A2", LastName = "L2", PhoneNumber = "2", Department = "IT" }
        };
        _context.Admins.AddRange(admins);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FirstOrDefaultAsync(a => a.Department == "IT");

        // Assert
        result.Should().NotBeNull();
        result!.Department.Should().Be("IT");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WhenNoMatch_ReturnsNull()
    {
        // Act
        var result = await _repository.FirstOrDefaultAsync(a => a.Department == "Finance");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_AddsEntityToContext()
    {
        // Arrange
        var admin = new Admin { Email = "new@example.com", FirstName = "New", LastName = "Admin", PhoneNumber = "123" };

        // Act
        var result = await _repository.AddAsync(admin);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().BeSameAs(admin);
        _context.Admins.Should().Contain(admin);
        var saved = await _context.Admins.FindAsync(admin.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task AddRangeAsync_AddsMultipleEntities()
    {
        // Arrange
        var admins = new List<Admin>
        {
            new() { Email = "admin1@example.com", FirstName = "A1", LastName = "L1", PhoneNumber = "1" },
            new() { Email = "admin2@example.com", FirstName = "A2", LastName = "L2", PhoneNumber = "2" }
        };

        // Act
        await _repository.AddRangeAsync(admins);
        await _context.SaveChangesAsync();

        // Assert
        _context.Admins.Should().HaveCount(2);
    }

    [Fact]
    public async Task Update_UpdatesEntity()
    {
        // Arrange
        var admin = new Admin { Id = 1, Email = "old@example.com", FirstName = "Old", LastName = "Name", PhoneNumber = "1" };
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        admin.Email = "new@example.com";
        _repository.Update(admin);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Admins.FindAsync(1);
        updated!.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task UpdateRange_UpdatesMultipleEntities()
    {
        // Arrange
        var admins = new List<Admin>
        {
            new() { Id = 1, Email = "admin1@example.com", FirstName = "A1", LastName = "L1", PhoneNumber = "1", Department = "IT" },
            new() { Id = 2, Email = "admin2@example.com", FirstName = "A2", LastName = "L2", PhoneNumber = "2", Department = "IT" }
        };
        _context.Admins.AddRange(admins);
        await _context.SaveChangesAsync();

        // Act
        foreach (var admin in admins)
        {
            admin.Department = "HR";
        }
        _repository.UpdateRange(admins);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Admins.ToListAsync();
        updated.Should().OnlyContain(a => a.Department == "HR");
    }

    [Fact]
    public async Task Remove_RemovesEntity()
    {
        // Arrange
        var admin = new Admin { Id = 1, Email = "delete@example.com", FirstName = "Delete", LastName = "Me", PhoneNumber = "1" };
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        _repository.Remove(admin);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Admins.FindAsync(1);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task RemoveRange_RemovesMultipleEntities()
    {
        // Arrange
        var admins = new List<Admin>
        {
            new() { Id = 1, Email = "admin1@example.com", FirstName = "A1", LastName = "L1", PhoneNumber = "1" },
            new() { Id = 2, Email = "admin2@example.com", FirstName = "A2", LastName = "L2", PhoneNumber = "2" }
        };
        _context.Admins.AddRange(admins);
        await _context.SaveChangesAsync();

        // Act
        _repository.RemoveRange(admins);
        await _context.SaveChangesAsync();

        // Assert
        _context.Admins.Should().BeEmpty();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var admins = new List<Admin>
        {
            new() { Email = "admin1@example.com", FirstName = "A1", LastName = "L1", PhoneNumber = "1" },
            new() { Email = "admin2@example.com", FirstName = "A2", LastName = "L2", PhoneNumber = "2" },
            new() { Email = "admin3@example.com", FirstName = "A3", LastName = "L3", PhoneNumber = "3" }
        };
        _context.Admins.AddRange(admins);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task CountAsync_WhenEmpty_ReturnsZero()
    {
        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsMatchingCount()
    {
        // Arrange
        var admins = new List<Admin>
        {
            new() { Email = "admin1@example.com", FirstName = "A1", LastName = "L1", PhoneNumber = "1", Department = "IT" },
            new() { Email = "admin2@example.com", FirstName = "A2", LastName = "L2", PhoneNumber = "2", Department = "HR" },
            new() { Email = "admin3@example.com", FirstName = "A3", LastName = "L3", PhoneNumber = "3", Department = "IT" }
        };
        _context.Admins.AddRange(admins);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountAsync(a => a.Department == "IT");

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task AnyAsync_WhenMatch_ReturnsTrue()
    {
        // Arrange
        var admin = new Admin { Email = "admin@example.com", FirstName = "A", LastName = "L", PhoneNumber = "1", Department = "IT" };
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(a => a.Department == "IT");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WhenNoMatch_ReturnsFalse()
    {
        // Arrange
        var admin = new Admin { Email = "admin@example.com", FirstName = "A", LastName = "L", PhoneNumber = "1", Department = "IT" };
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(a => a.Department == "Finance");

        // Assert
        result.Should().BeFalse();
    }
}
