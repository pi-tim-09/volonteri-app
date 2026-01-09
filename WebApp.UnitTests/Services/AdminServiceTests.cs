using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.UnitTests.Services;

public class AdminServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAdminRepository> _adminRepo = new();
    private readonly Mock<ILogger<AdminService>> _logger = new();

    private AdminService CreateSut()
    {
        _unitOfWork.SetupGet(x => x.Admins).Returns(_adminRepo.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        return new AdminService(_unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public void Ctor_WhenUnitOfWorkNull_Throws()
    {
        Action act = () => new AdminService(null!, _logger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("unitOfWork");
    }

    [Fact]
    public void Ctor_WhenLoggerNull_Throws()
    {
        Action act = () => new AdminService(_unitOfWork.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CreateAdminAsync_WhenNull_Throws()
    {
        var sut = CreateSut();

        Func<Task> act = async () => await sut.CreateAdminAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("admin");
    }

    [Fact]
    public async Task CreateAdminAsync_WhenValid_SetsBusinessRules_AndSaves()
    {
        var sut = CreateSut();

        var admin = new Admin
        {
            Email = "admin@example.com",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "123",
            Department = "IT",
            CanManageUsers = true,
            CanManageOrganizations = true,
            CanManageProjects = true,
            IsActive = false
        };

        _adminRepo.Setup(r => r.AddAsync(It.IsAny<Admin>())).ReturnsAsync((Admin a) => a);

        var result = await sut.CreateAdminAsync(admin);

        result.Role.Should().Be(UserRole.Admin);
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.CanManageUsers.Should().BeFalse();
        result.CanManageOrganizations.Should().BeFalse();
        result.CanManageProjects.Should().BeFalse();

        _adminRepo.Verify(r => r.AddAsync(admin), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAdminAsync_WhenNotFound_ReturnsFalse()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.UpdateAdminAsync(1, new Admin { Email = "x@x.com" });

        ok.Should().BeFalse();
        _adminRepo.Verify(r => r.Update(It.IsAny<Admin>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAdminAsync_WhenFound_UpdatesAllowedFields_WithoutChangingPermissions()
    {
        var sut = CreateSut();

        var existing = new Admin
        {
            Id = 1,
            Email = "old@x.com",
            FirstName = "Old",
            LastName = "Admin",
            PhoneNumber = "0",
            Department = "OldDept",
            IsActive = true,
            CanManageUsers = true,
            CanManageOrganizations = true,
            CanManageProjects = true
        };

        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

        var incoming = new Admin
        {
            Email = "new@x.com",
            FirstName = "New",
            LastName = "Admin",
            PhoneNumber = "9",
            Department = "NewDept",
            IsActive = false,
            CanManageUsers = false,
            CanManageOrganizations = false,
            CanManageProjects = false
        };

        var ok = await sut.UpdateAdminAsync(1, incoming);

        ok.Should().BeTrue();
        existing.Email.Should().Be("new@x.com");
        existing.Department.Should().Be("NewDept");
        existing.IsActive.Should().BeFalse();
        existing.CanManageUsers.Should().BeTrue();
        existing.CanManageOrganizations.Should().BeTrue();
        existing.CanManageProjects.Should().BeTrue();

        _adminRepo.Verify(r => r.Update(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAdminAsync_WhenLastAdmin_Throws()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>() ))
            .ReturnsAsync(1);

        Func<Task> act = async () => await sut.DeleteAdminAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete the last admin in the system.");
    }

    [Fact]
    public async Task DeleteAdminAsync_WhenNotLastAdmin_DeletesSuccessfully()
    {
        var sut = CreateSut();
        var admin = new Admin { Id = 1, Email = "admin@example.com" };
        
        _adminRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()))
            .ReturnsAsync(2);
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(admin);

        var ok = await sut.DeleteAdminAsync(1);

        ok.Should().BeTrue();
        _adminRepo.Verify(r => r.Remove(admin), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAdminAsync_WhenAdminNotFound_ReturnsFalse()
    {
        var sut = CreateSut();
        
        _adminRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()))
            .ReturnsAsync(2);
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.DeleteAdminAsync(1);

        ok.Should().BeFalse();
        _adminRepo.Verify(r => r.Remove(It.IsAny<Admin>()), Times.Never);
    }

    [Fact]
    public async Task GetAdminByIdAsync_WhenAdminExists_ReturnsAdmin()
    {
        var sut = CreateSut();
        var admin = new Admin { Id = 1, Email = "admin@example.com" };
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(admin);

        var result = await sut.GetAdminByIdAsync(1);

        result.Should().BeSameAs(admin);
        _adminRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetAdminByIdAsync_WhenAdminNotFound_ReturnsNull()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var result = await sut.GetAdminByIdAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAdminsAsync_ReturnsAllAdmins()
    {
        var sut = CreateSut();
        var admins = new List<Admin>
        {
            new() { Id = 1, Email = "admin1@example.com" },
            new() { Id = 2, Email = "admin2@example.com" },
            new() { Id = 3, Email = "admin3@example.com" }
        };
        _adminRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(admins);

        var result = await sut.GetAllAdminsAsync();

        result.Should().BeEquivalentTo(admins);
        result.Should().HaveCount(3);
        _adminRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GrantAndRevokePermissions_WhenAdminExists_UpdatesAndSaves()
    {
        var sut = CreateSut();
        var admin = new Admin { Id = 5, CanManageUsers = false, CanManageOrganizations = false, CanManageProjects = false };
        _adminRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

        (await sut.GrantUserManagementPermissionAsync(admin.Id)).Should().BeTrue();
        admin.CanManageUsers.Should().BeTrue();

        (await sut.RevokeUserManagementPermissionAsync(admin.Id)).Should().BeTrue();
        admin.CanManageUsers.Should().BeFalse();

        (await sut.GrantOrganizationManagementPermissionAsync(admin.Id)).Should().BeTrue();
        admin.CanManageOrganizations.Should().BeTrue();

        (await sut.GrantProjectManagementPermissionAsync(admin.Id)).Should().BeTrue();
        admin.CanManageProjects.Should().BeTrue();

        _adminRepo.Verify(r => r.Update(admin), Times.AtLeastOnce);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RevokeOrganizationManagementPermissionAsync_WhenAdminExists_RevokesPermission()
    {
        var sut = CreateSut();
        var admin = new Admin { Id = 1, CanManageOrganizations = true };
        _adminRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

        var ok = await sut.RevokeOrganizationManagementPermissionAsync(admin.Id);

        ok.Should().BeTrue();
        admin.CanManageOrganizations.Should().BeFalse();
        _adminRepo.Verify(r => r.Update(admin), Times.Once);
    }

    [Fact]
    public async Task RevokeProjectManagementPermissionAsync_WhenAdminExists_RevokesPermission()
    {
        var sut = CreateSut();
        var admin = new Admin { Id = 1, CanManageProjects = true };
        _adminRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

        var ok = await sut.RevokeProjectManagementPermissionAsync(admin.Id);

        ok.Should().BeTrue();
        admin.CanManageProjects.Should().BeFalse();
        _adminRepo.Verify(r => r.Update(admin), Times.Once);
    }

    [Fact]
    public async Task GrantUserManagementPermissionAsync_WhenAdminNotFound_ReturnsFalse()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.GrantUserManagementPermissionAsync(1);

        ok.Should().BeFalse();
        _adminRepo.Verify(r => r.Update(It.IsAny<Admin>()), Times.Never);
    }

    [Fact]
    public async Task CanManageUsersAsync_DelegatesToRepository()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.CanManageUsersAsync(3)).ReturnsAsync(true);

        var ok = await sut.CanManageUsersAsync(3);

        ok.Should().BeTrue();
        _adminRepo.Verify(r => r.CanManageUsersAsync(3), Times.Once);
    }

    [Fact]
    public async Task CanManageOrganizationsAsync_DelegatesToRepository()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.CanManageOrganizationsAsync(3)).ReturnsAsync(false);

        var ok = await sut.CanManageOrganizationsAsync(3);

        ok.Should().BeFalse();
        _adminRepo.Verify(r => r.CanManageOrganizationsAsync(3), Times.Once);
    }

    [Fact]
    public async Task CanManageProjectsAsync_DelegatesToRepository()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.CanManageProjectsAsync(5)).ReturnsAsync(true);

        var ok = await sut.CanManageProjectsAsync(5);

        ok.Should().BeTrue();
        _adminRepo.Verify(r => r.CanManageProjectsAsync(5), Times.Once);
    }

    [Fact]
    public async Task CanDeleteAdminAsync_WhenOnlyOneActiveAdmin_ReturnsFalse()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()))
            .ReturnsAsync(1);

        var ok = await sut.CanDeleteAdminAsync(1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task CanDeleteAdminAsync_WhenMultipleActiveAdmins_ReturnsTrue()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()))
            .ReturnsAsync(3);

        var ok = await sut.CanDeleteAdminAsync(1);

        ok.Should().BeTrue();
    }

    

    [Fact]
    public async Task CreateAdminAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        var admin = new Admin { Email = "test@example.com", FirstName = "Test", LastName = "User", PhoneNumber = "123" };
        
        _adminRepo.Setup(r => r.AddAsync(It.IsAny<Admin>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await sut.CreateAdminAsync(admin);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public async Task UpdateAdminAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        var existing = new Admin { Id = 1, Email = "old@x.com", FirstName = "Old", LastName = "Admin", PhoneNumber = "0" };
        
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _adminRepo.Setup(r => r.Update(It.IsAny<Admin>()))
            .Throws(new InvalidOperationException("Update failed"));

        Func<Task> act = async () => await sut.UpdateAdminAsync(1, new Admin { Email = "new@x.com" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Update failed");
    }

    [Fact]
    public async Task DeleteAdminAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        var admin = new Admin { Id = 1, Email = "admin@example.com" };
        
        _adminRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()))
            .ReturnsAsync(2);
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(admin);
        _adminRepo.Setup(r => r.Remove(It.IsAny<Admin>()))
            .Throws(new InvalidOperationException("Delete failed"));

        Func<Task> act = async () => await sut.DeleteAdminAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Delete failed");
    }

    [Fact]
    public async Task GetAdminByIdAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        
        _adminRepo.Setup(r => r.GetByIdAsync(1))
            .ThrowsAsync(new InvalidOperationException("Get failed"));

        Func<Task> act = async () => await sut.GetAdminByIdAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Get failed");
    }

    [Fact]
    public async Task GetAllAdminsAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        
        _adminRepo.Setup(r => r.GetAllAsync())
            .ThrowsAsync(new InvalidOperationException("GetAll failed"));

        Func<Task> act = async () => await sut.GetAllAdminsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("GetAll failed");
    }

    [Fact]
    public async Task GrantUserManagementPermissionAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        var admin = new Admin { Id = 1, CanManageUsers = false };
        
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(admin);
        _adminRepo.Setup(r => r.Update(It.IsAny<Admin>()))
            .Throws(new InvalidOperationException("Grant failed"));

        Func<Task> act = async () => await sut.GrantUserManagementPermissionAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Grant failed");
    }

    [Fact]
    public async Task RevokeUserManagementPermissionAsync_WhenAdminNotFound_ReturnsFalse()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.RevokeUserManagementPermissionAsync(1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task GrantOrganizationManagementPermissionAsync_WhenAdminNotFound_ReturnsFalse()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.GrantOrganizationManagementPermissionAsync(1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeOrganizationManagementPermissionAsync_WhenAdminNotFound_ReturnsFalse()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.RevokeOrganizationManagementPermissionAsync(1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task GrantProjectManagementPermissionAsync_WhenAdminNotFound_ReturnsFalse()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.GrantProjectManagementPermissionAsync(1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeProjectManagementPermissionAsync_WhenAdminNotFound_ReturnsFalse()
    {
        var sut = CreateSut();
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.RevokeProjectManagementPermissionAsync(1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageUsersAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        
        _adminRepo.Setup(r => r.CanManageUsersAsync(1))
            .ThrowsAsync(new InvalidOperationException("Check failed"));

        Func<Task> act = async () => await sut.CanManageUsersAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Check failed");
    }

    [Fact]
    public async Task CanManageOrganizationsAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        
        _adminRepo.Setup(r => r.CanManageOrganizationsAsync(1))
            .ThrowsAsync(new InvalidOperationException("Check failed"));

        Func<Task> act = async () => await sut.CanManageOrganizationsAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Check failed");
    }

    [Fact]
    public async Task CanManageProjectsAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        
        _adminRepo.Setup(r => r.CanManageProjectsAsync(1))
            .ThrowsAsync(new InvalidOperationException("Check failed"));

        Func<Task> act = async () => await sut.CanManageProjectsAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Check failed");
    }

    [Fact]
    public async Task CanDeleteAdminAsync_WhenRepositoryThrows_RethrowsException()
    {
        var sut = CreateSut();
        
        _adminRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()))
            .ThrowsAsync(new InvalidOperationException("Count failed"));

        Func<Task> act = async () => await sut.CanDeleteAdminAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Count failed");
    }

    
}
