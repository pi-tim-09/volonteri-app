using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Common;
using WebApp.Controllers.Api;
using WebApp.DTOs.Auth;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.UnitTests.Controllers.Api;

public class AuthApiControllerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ILogger<AuthApiController>> _logger = new();
    private readonly Mock<IVolunteerRepository> _volRepo = new();
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IAdminRepository> _adminRepo = new();

    private AuthApiController CreateSut()
    {
        _uow.SetupGet(x => x.Volunteers).Returns(_volRepo.Object);
        _uow.SetupGet(x => x.Organizations).Returns(_orgRepo.Object);
        _uow.SetupGet(x => x.Admins).Returns(_adminRepo.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "JWT:SecureKey", "super-secret-key-for-testing-purposes-only" } })
            .Build();

        return new AuthApiController(_uow.Object, _hasher.Object, config, _logger.Object);
    }

    [Fact]
    public async Task Login_WhenModelInvalid_ReturnsBadRequest()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError("Email", "Required");

        var result = await sut.Login(new LoginRequest());

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_WhenUserNotFound_ReturnsUnauthorized()
    {
        var sut = CreateSut();
        _volRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>())).ReturnsAsync((Volunteer?)null);
        _orgRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Organization, bool>>>())).ReturnsAsync((Organization?)null);
        _adminRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>())).ReturnsAsync((Admin?)null);

        var result = await sut.Login(new LoginRequest { Email = "nonexistent@example.com", Password = "password" });

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorized.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Login_WhenPasswordIncorrect_ReturnsUnauthorized()
    {
        var sut = CreateSut();
        var user = new Volunteer { Email = "test@example.com", PasswordHash = "hashed_password" };
        _volRepo.Setup(r => r.FirstOrDefaultAsync(u => u.Email == user.Email)).ReturnsAsync(user);
        _hasher.Setup(h => h.VerifyPassword(user.PasswordHash, "wrong_password")).Returns(false);

        var result = await sut.Login(new LoginRequest { Email = user.Email, Password = "wrong_password" });

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorized.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Login_WhenUserInactive_ReturnsUnauthorized()
    {
        var sut = CreateSut();
        var user = new Volunteer
        {
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            IsActive = false,
            Role = UserRole.Volunteer
        };

        _volRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>()))
            .ReturnsAsync(user);
        _orgRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Organization, bool>>>()))
            .ReturnsAsync((Organization?)null);
        _adminRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()))
            .ReturnsAsync((Admin?)null);

        _hasher.Setup(h => h.VerifyPassword(user.PasswordHash, "password")).Returns(true);

        var result = await sut.Login(new LoginRequest { Email = user.Email, Password = "password" });

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorized.Value.Should().BeOfType<ApiResponse>().Subject;
        response.Message.Should().Be("Account is not active");
    }

    [Fact]
    public async Task Login_WhenSuccessful_ReturnsOkWithToken()
    {
        var sut = CreateSut();
        var user = new Volunteer
        {
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            IsActive = true,
            Role = UserRole.Volunteer
        };

        _volRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>()))
            .ReturnsAsync(user);
        _orgRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Organization, bool>>>()))
            .ReturnsAsync((Organization?)null);
        _adminRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()))
            .ReturnsAsync((Admin?)null);

        _hasher.Setup(h => h.VerifyPassword(user.PasswordHash, "password")).Returns(true);

        var result = await sut.Login(new LoginRequest { Email = user.Email, Password = "password" });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<AuthResponse>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Token.Should().NotBeNullOrWhiteSpace();
        response.Data.Email.Should().Be(user.Email);
        response.Data.Role.Should().Be(UserRole.Volunteer.ToString());
    }
}
