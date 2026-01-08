using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Patterns.Behavioral;
using WebApp.Patterns.Creational;
using WebApp.Patterns.Structural;
using WebApp.Services;
using WebApp.ViewModels;

namespace WebApp.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserFactoryProvider> _factoryProvider = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IApplicationStateContextFactory> _stateContextFactory = new();
    private readonly Mock<ILogger<UserService>> _logger = new();
    private readonly Mock<IVolunteerProfileService> _volunteerProfileService = new();
    private readonly Mock<IVolunteerEventPublisher> _volunteerEventPublisher = new();

    private readonly Mock<IVolunteerRepository> _volunteerRepo = new();
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IAdminRepository> _adminRepo = new();
    private readonly Mock<IApplicationRepository> _appRepo = new();

    private UserService CreateSut()
    {
        _uow.SetupGet(x => x.Volunteers).Returns(_volunteerRepo.Object);
        _uow.SetupGet(x => x.Organizations).Returns(_orgRepo.Object);
        _uow.SetupGet(x => x.Admins).Returns(_adminRepo.Object);
        _uow.SetupGet(x => x.Applications).Returns(_appRepo.Object);

        _uow.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        return new UserService(
            _uow.Object,
            _factoryProvider.Object,
            _notificationService.Object,
            _stateContextFactory.Object,
            _logger.Object,
            _volunteerProfileService.Object,
            _volunteerEventPublisher.Object);
    }

    private void SetupEmailDoesNotExist()
    {
        _volunteerRepo
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>() ))
            .ReturnsAsync(false);
        _orgRepo
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Organization, bool>>>() ))
            .ReturnsAsync(false);
        _adminRepo
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>() ))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailExists_Throws()
    {
        var sut = CreateSut();

        _volunteerRepo
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>() ))
            .ReturnsAsync(true);

        var vm = new UserVM
        {
            Email = "test@example.com",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "123",
            Role = UserRole.Volunteer,
            IsActive = true
        };

        Func<Task> act = async () => await sut.CreateUserAsync(vm);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists.*");
    }

    [Fact]
    public async Task CreateUserAsync_WhenVolunteer_CreatesAndPublishesEvent()
    {
        var sut = CreateSut();

        _volunteerRepo
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Volunteer, bool>>>() ))
            .ReturnsAsync(false);
        _orgRepo
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Organization, bool>>>() ))
            .ReturnsAsync(false);
        _adminRepo
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>() ))
            .ReturnsAsync(false);

        var createdVolunteer = new Volunteer { Id = 1, Email = "v@example.com", FirstName = "V", LastName = "L" };
        _factoryProvider
            .Setup(f => f.CreateUser(UserRole.Volunteer, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(createdVolunteer);

        var vm = new UserVM
        {
            Email = createdVolunteer.Email,
            FirstName = createdVolunteer.FirstName,
            LastName = createdVolunteer.LastName,
            PhoneNumber = "123",
            Role = UserRole.Volunteer,
            IsActive = true
        };

        var result = await sut.CreateUserAsync(vm);

        result.Should().BeSameAs(createdVolunteer);
        _volunteerRepo.Verify(r => r.AddAsync(It.IsAny<Volunteer>()), Times.Once);
        _volunteerEventPublisher.Verify(p => p.NotifyVolunteerRegisteredAsync(It.IsAny<Volunteer>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetEnrichedVolunteerSummaryAsync_WhenNotFound_Throws()
    {
        var sut = CreateSut();

        _volunteerRepo.Setup(r => r.GetByIdAsync(123)).ReturnsAsync((Volunteer?)null);

        Func<Task> act = async () => await sut.GetEnrichedVolunteerSummaryAsync(123);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Volunteer 123 not found");
    }

    [Fact]
    public async Task GetEnrichedVolunteerSummaryAsync_WhenFound_UsesDecoratorService()
    {
        var sut = CreateSut();

        var volunteer = new Volunteer { Id = 5, Email = "a@a.com", FirstName = "Ana", LastName = "Horvat" };
        _volunteerRepo.Setup(r => r.GetByIdAsync(volunteer.Id)).ReturnsAsync(volunteer);

        _volunteerProfileService
            .Setup(s => s.FormatVolunteerSummaryAsync(volunteer))
            .ReturnsAsync("SUMMARY");

        var result = await sut.GetEnrichedVolunteerSummaryAsync(volunteer.Id);

        result.Should().Be("SUMMARY");
        _volunteerProfileService.Verify(s => s.FormatVolunteerSummaryAsync(volunteer), Times.Once);
    }

    [Fact]
    public async Task UpdateVolunteerSkillsAsync_WhenVolunteerFound_UpdatesAndNotifies()
    {
        var sut = CreateSut();

        var volunteer = new Volunteer { Id = 1, Skills = new List<string>() };
        _volunteerRepo.Setup(r => r.GetByIdAsync(volunteer.Id)).ReturnsAsync(volunteer);

        var skills = new List<string> { "C#", "ASP.NET" };
        var ok = await sut.UpdateVolunteerSkillsAsync(volunteer.Id, skills);

        ok.Should().BeTrue();
        volunteer.Skills.Should().BeEquivalentTo(skills);
        _volunteerRepo.Verify(r => r.Update(It.IsAny<Volunteer>()), Times.Once);
        _volunteerEventPublisher.Verify(p => p.NotifyVolunteerSkillsUpdatedAsync(volunteer, skills), Times.Once);
    }

    [Fact]
    public async Task SubmitApplicationAsync_CreatesApplication_AndSendsNotification()
    {
        var sut = CreateSut();

        _appRepo.Setup(r => r.AddAsync(It.IsAny<Application>())).ReturnsAsync((Application a) => a);

        var app = await sut.SubmitApplicationAsync(volunteerId: 10, projectId: 20);

        app.VolunteerId.Should().Be(10);
        app.ProjectId.Should().Be(20);
        app.Status.Should().Be(ApplicationStatus.Pending);

        _appRepo.Verify(r => r.AddAsync(It.IsAny<Application>()), Times.Once);
        _notificationService.Verify(n => n.NotifyApplicationSubmittedAsync(It.IsAny<Application>()), Times.Once);
    }

    private sealed class AlwaysFalseApproveState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Pending;
        public override bool CanApprove() => false;
    }

    private sealed class StubStateFactory : IApplicationStateFactory
    {
        private readonly IApplicationState _state;
        public StubStateFactory(IApplicationState state) => _state = state;
        public IApplicationState CreateState(ApplicationStatus status) => _state;
    }

    [Fact]
    public async Task ApproveApplicationAsync_WhenCannotApprove_ReturnsFalse_DoesNotNotify()
    {
        var sut = CreateSut();

        var application = new Application { Id = 1, Status = ApplicationStatus.Pending };

        var context = new ApplicationStateContext(
            application,
            new StubStateFactory(new AlwaysFalseApproveState()),
            Mock.Of<ILogger<ApplicationStateContext>>());

        _stateContextFactory.Setup(f => f.CreateContext(application)).Returns(context);

        var ok = await sut.ApproveApplicationAsync(application, "notes");

        ok.Should().BeFalse();
        _notificationService.Verify(n => n.NotifyApplicationApprovedAsync(It.IsAny<Application>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenOrganization_AddsOrganization()
    {
        var sut = CreateSut();
        SetupEmailDoesNotExist();

        var created = new Organization { Id = 2, Email = "o@example.com", FirstName = "Org", LastName = "User" };
        _factoryProvider
            .Setup(f => f.CreateUser(UserRole.Organization, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(created);

        var vm = new UserVM
        {
            Email = created.Email,
            FirstName = created.FirstName,
            LastName = created.LastName,
            PhoneNumber = "123",
            Role = UserRole.Organization,
            IsActive = true
        };

        var result = await sut.CreateUserAsync(vm);

        result.Should().BeSameAs(created);
        _orgRepo.Verify(r => r.AddAsync(It.IsAny<Organization>()), Times.Once);
        _volunteerRepo.Verify(r => r.AddAsync(It.IsAny<Volunteer>()), Times.Never);
        _adminRepo.Verify(r => r.AddAsync(It.IsAny<Admin>()), Times.Never);
        _volunteerEventPublisher.Verify(p => p.NotifyVolunteerRegisteredAsync(It.IsAny<Volunteer>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenAdmin_AddsAdmin()
    {
        var sut = CreateSut();
        SetupEmailDoesNotExist();

        var created = new Admin { Id = 3, Email = "admin@example.com", FirstName = "A", LastName = "D" };
        _factoryProvider
            .Setup(f => f.CreateUser(UserRole.Admin, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(created);

        var vm = new UserVM
        {
            Email = created.Email,
            FirstName = created.FirstName,
            LastName = created.LastName,
            PhoneNumber = "123",
            Role = UserRole.Admin,
            IsActive = true
        };

        var result = await sut.CreateUserAsync(vm);

        result.Should().BeSameAs(created);
        _adminRepo.Verify(r => r.AddAsync(It.IsAny<Admin>()), Times.Once);
        _volunteerRepo.Verify(r => r.AddAsync(It.IsAny<Volunteer>()), Times.Never);
        _orgRepo.Verify(r => r.AddAsync(It.IsAny<Organization>()), Times.Never);
        _volunteerEventPublisher.Verify(p => p.NotifyVolunteerRegisteredAsync(It.IsAny<Volunteer>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNotFound_ReturnsFalse()
    {
        var sut = CreateSut();

        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Volunteer?)null);
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.UpdateUserAsync(1, new UserVM { Id = 1, Email = "x@x.com", FirstName = "X", LastName = "Y", PhoneNumber = "1", Role = UserRole.Volunteer, IsActive = true });

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserAsync_WhenVolunteerFound_UpdatesAndSaves()
    {
        var sut = CreateSut();

        var volunteer = new Volunteer { Id = 10, Email = "old@x.com", FirstName = "Old", LastName = "Name", PhoneNumber = "0", IsActive = true };
        _volunteerRepo.Setup(r => r.GetByIdAsync(volunteer.Id)).ReturnsAsync(volunteer);

        var vm = new UserVM { Id = volunteer.Id, Email = "new@x.com", FirstName = "New", LastName = "Name", PhoneNumber = "9", Role = UserRole.Volunteer, IsActive = false };

        var ok = await sut.UpdateUserAsync(volunteer.Id, vm);

        ok.Should().BeTrue();
        volunteer.Email.Should().Be("new@x.com");
        volunteer.IsActive.Should().BeFalse();
        _volunteerRepo.Verify(r => r.Update(volunteer), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNotFound_ReturnsFalse()
    {
        var sut = CreateSut();

        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Volunteer?)null);
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.DeleteUserAsync(1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenOrganizationFound_RemovesAndSaves()
    {
        var sut = CreateSut();

        var org = new Organization { Id = 7, Email = "o@x.com" };
        _volunteerRepo.Setup(r => r.GetByIdAsync(org.Id)).ReturnsAsync((Volunteer?)null);
        _orgRepo.Setup(r => r.GetByIdAsync(org.Id)).ReturnsAsync(org);

        var ok = await sut.DeleteUserAsync(org.Id);

        ok.Should().BeTrue();
        _orgRepo.Verify(r => r.Remove(org), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ActivateUserAsync_WhenNotFound_ReturnsFalse()
    {
        var sut = CreateSut();

        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Volunteer?)null);
        _orgRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Organization?)null);
        _adminRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Admin?)null);

        var ok = await sut.ActivateUserAsync(1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenAdminFound_SetsInactiveAndSaves()
    {
        var sut = CreateSut();

        var admin = new Admin { Id = 9, Email = "a@a.com", IsActive = true };
        _volunteerRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync((Volunteer?)null);
        _orgRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync((Organization?)null);
        _adminRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

        var ok = await sut.DeactivateUserAsync(admin.Id);

        ok.Should().BeTrue();
        admin.IsActive.Should().BeFalse();
        _adminRepo.Verify(r => r.Update(admin), Times.Once);
    }

    private sealed class AlwaysTrueApproveState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Pending;
        public override bool CanApprove() => true;

        public override Task<bool> ApproveAsync(ApplicationStateContext context, string? reviewNotes)
        {
            // emulate state transition effect
            context.Application.ReviewNotes = reviewNotes;
            return Task.FromResult(true);
        }
    }

    private sealed class AlwaysFalseRejectState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Pending;
        public override bool CanReject() => false;
    }

    private sealed class AlwaysTrueWithdrawState : ApplicationStateBase
    {
        public override ApplicationStatus Status => ApplicationStatus.Pending;
        public override bool CanWithdraw() => true;
        public override Task<bool> WithdrawAsync(ApplicationStateContext context) => Task.FromResult(true);
    }

    [Fact]
    public async Task ApproveApplicationAsync_WhenCanApprove_ApprovesAndNotifies()
    {
        var sut = CreateSut();
        var application = new Application { Id = 1, Status = ApplicationStatus.Pending };

        var context = new ApplicationStateContext(
            application,
            new StubStateFactory(new AlwaysTrueApproveState()),
            Mock.Of<ILogger<ApplicationStateContext>>());

        _stateContextFactory.Setup(f => f.CreateContext(application)).Returns(context);

        var ok = await sut.ApproveApplicationAsync(application, "ok");

        ok.Should().BeTrue();
        _notificationService.Verify(n => n.NotifyApplicationApprovedAsync(application), Times.Once);
    }

    [Fact]
    public async Task RejectApplicationAsync_WhenCannotReject_ReturnsFalse_DoesNotNotify()
    {
        var sut = CreateSut();
        var application = new Application { Id = 1, Status = ApplicationStatus.Pending };

        var context = new ApplicationStateContext(
            application,
            new StubStateFactory(new AlwaysFalseRejectState()),
            Mock.Of<ILogger<ApplicationStateContext>>());

        _stateContextFactory.Setup(f => f.CreateContext(application)).Returns(context);

        var ok = await sut.RejectApplicationAsync(application, "no");

        ok.Should().BeFalse();
        _notificationService.Verify(n => n.NotifyApplicationRejectedAsync(It.IsAny<Application>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawApplicationAsync_WhenCanWithdraw_WithdrawsAndNotifies()
    {
        var sut = CreateSut();
        var application = new Application { Id = 1, Status = ApplicationStatus.Pending };

        var context = new ApplicationStateContext(
            application,
            new StubStateFactory(new AlwaysTrueWithdrawState()),
            Mock.Of<ILogger<ApplicationStateContext>>());

        _stateContextFactory.Setup(f => f.CreateContext(application)).Returns(context);

        var ok = await sut.WithdrawApplicationAsync(application);

        ok.Should().BeTrue();
        _notificationService.Verify(n => n.NotifyApplicationWithdrawnAsync(application), Times.Once);
    }
}
