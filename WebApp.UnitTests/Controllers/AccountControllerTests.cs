using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using WebApp.Controllers;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services;
using WebApp.ViewModels;

namespace WebApp.UnitTests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IVolunteerRepository> _volRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();

    private AccountController CreateSut()
    {
        _uow.SetupGet(x => x.Volunteers).Returns(_volRepo.Object);

        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var controller = new AccountController(_uow.Object, cfg, _hasher.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        return controller;
    }

    [Fact]
    public void Register_Get_ReturnsView()
    {
        var sut = CreateSut();

        var result = sut.Register();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Login_Get_ReturnsView()
    {
        var sut = CreateSut();

        var result = sut.Login();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Register_Post_WhenModelInvalid_ReturnsViewWithSameModel()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError("Email", "Required");

        var vm = new RegisterVM { Email = "x@x.com" };

        var result = await sut.Register(vm);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(vm);
    }

    [Fact]
    public async Task Register_Post_WhenEmailExists_ReturnsViewAndModelError()
    {
        var sut = CreateSut();

        _volRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>() ))
            .ReturnsAsync(true);

        var vm = new RegisterVM
        {
            Email = "exists@x.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "123",
            Role = UserRole.Volunteer
        };

        var result = await sut.Register(vm);

        result.Should().BeOfType<ViewResult>();
        sut.ModelState.ContainsKey("Email").Should().BeTrue();
    }

    [Fact]
    public async Task Login_Post_WhenUserNotFound_ReturnsViewWithModelError()
    {
        var sut = CreateSut();

        _volRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>() ))
            .ReturnsAsync((Volunteer?)null);

        var vm = new LoginVM { Email = "no@x.com", Password = "x" };

        var result = await sut.Login(vm);

        result.Should().BeOfType<ViewResult>();
        sut.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_Post_WhenPasswordInvalid_ReturnsViewWithModelError()
    {
        var sut = CreateSut();

        var user = new Volunteer { Email = "a@a.com", PasswordHash = "HASH", IsActive = true, Role = UserRole.Volunteer };
        _volRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>() ))
            .ReturnsAsync(user);

        _hasher.Setup(h => h.VerifyPassword(user.PasswordHash, It.IsAny<string>())).Returns(false);

        var vm = new LoginVM { Email = "a@a.com", Password = "wrong" };

        var result = await sut.Login(vm);

        result.Should().BeOfType<ViewResult>();
        sut.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }
}
