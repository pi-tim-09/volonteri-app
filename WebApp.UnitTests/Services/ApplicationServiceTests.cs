using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.Patterns.Behavioral;
using WebApp.Patterns.Structural;
using WebApp.Services;

namespace WebApp.UnitTests.Services;

public class ApplicationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProjectService> _projectService = new();
    private readonly Mock<IApplicationStateContextFactory> _stateContextFactory = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ILogger<ApplicationService>> _logger = new();
    
    private readonly Mock<IApplicationRepository> _appRepo = new();
    private readonly Mock<IVolunteerRepository> _volunteerRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    
    // Use real state pattern with controlled application status
    private Application _testApplication = null!;

    private ApplicationService CreateSut()
    {
        // Create fresh application for each test
        _testApplication = new Application { Id = 1, Status = ApplicationStatus.Pending };
        
        _unitOfWork.SetupGet(x => x.Applications).Returns(_appRepo.Object);
        _unitOfWork.SetupGet(x => x.Volunteers).Returns(_volunteerRepo.Object);
        _unitOfWork.SetupGet(x => x.Projects).Returns(_projectRepo.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        
        // Use real state context factory
        var stateFactory = new ApplicationStateFactory(new Mock<ILogger<ApplicationStateContext>>().Object);
        var realFactory = new ApplicationStateContextFactory(
            stateFactory,
            new Mock<ILogger<ApplicationStateContext>>().Object);
        
        _stateContextFactory.Setup(x => x.CreateContext(It.IsAny<Application>()))
            .Returns((Application app) => realFactory.CreateContext(app ?? _testApplication));

        return new ApplicationService(
            _unitOfWork.Object,
            _projectService.Object,
            _stateContextFactory.Object,
            _notificationService.Object,
            _logger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenUnitOfWorkIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ApplicationService(
            null!,
            _projectService.Object,
            _stateContextFactory.Object,
            _notificationService.Object,
            _logger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_WhenProjectServiceIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ApplicationService(
            _unitOfWork.Object,
            null!,
            _stateContextFactory.Object,
            _notificationService.Object,
            _logger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("projectService");
    }

    [Fact]
    public void Constructor_WhenStateContextFactoryIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ApplicationService(
            _unitOfWork.Object,
            _projectService.Object,
            null!,
            _notificationService.Object,
            _logger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stateContextFactory");
    }

    [Fact]
    public void Constructor_WhenNotificationServiceIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ApplicationService(
            _unitOfWork.Object,
            _projectService.Object,
            _stateContextFactory.Object,
            null!,
            _logger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("notificationService");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ApplicationService(
            _unitOfWork.Object,
            _projectService.Object,
            _stateContextFactory.Object,
            _notificationService.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region CreateApplicationAsync Tests

    [Fact]
    public async Task CreateApplicationAsync_WhenCannotApplyToProject_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.CanAcceptVolunteersAsync(1)).ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await sut.CreateApplicationAsync(1, 1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Volunteer cannot apply to this project.");
    }

    [Fact]
    public async Task CreateApplicationAsync_WhenValid_CreatesApplicationWithCorrectDefaults()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.CanAcceptVolunteersAsync(1)).ReturnsAsync(true);
        
        var volunteer = new Volunteer { Id = 1, IsActive = true };
        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(volunteer);
        _appRepo.Setup(r => r.HasVolunteerAppliedAsync(1, 1)).ReturnsAsync(false);

        Application? capturedApp = null;
        _appRepo.Setup(r => r.AddAsync(It.IsAny<Application>()))
            .Callback<Application>(a => capturedApp = a)
            .ReturnsAsync((Application a) => a);

        // Act
        var result = await sut.CreateApplicationAsync(1, 1);

        // Assert
        capturedApp.Should().NotBeNull();
        capturedApp!.VolunteerId.Should().Be(1);
        capturedApp.ProjectId.Should().Be(1);
        capturedApp.AppliedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        capturedApp.Status.Should().Be(ApplicationStatus.Pending, "new applications start as pending");
    }

    [Fact]
    public async Task CreateApplicationAsync_WhenValid_SendsNotification()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.CanAcceptVolunteersAsync(1)).ReturnsAsync(true);
        
        var volunteer = new Volunteer { Id = 1, IsActive = true };
        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(volunteer);
        _appRepo.Setup(r => r.HasVolunteerAppliedAsync(1, 1)).ReturnsAsync(false);
        
        var application = new Application { Id = 1, VolunteerId = 1, ProjectId = 1 };
        _appRepo.Setup(r => r.AddAsync(It.IsAny<Application>())).ReturnsAsync(application);

        // Act
        await sut.CreateApplicationAsync(1, 1);

        // Assert - Decorator Pattern integration
        _notificationService.Verify(
            n => n.NotifyApplicationSubmittedAsync(It.IsAny<Application>()),
            Times.Once,
            "notification decorator should be called when application is submitted");
    }

    #endregion

    #region DeleteApplicationAsync Tests

    [Fact]
    public async Task DeleteApplicationAsync_WhenApplicationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Application?)null);

        // Act
        var result = await sut.DeleteApplicationAsync(1);

        // Assert
        result.Should().BeFalse();
        _appRepo.Verify(r => r.Remove(It.IsAny<Application>()), Times.Never);
    }

    [Fact]
    public async Task DeleteApplicationAsync_WhenApplicationWasAccepted_DecrementsVolunteerCount()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application
        {
            Id = 1,
            VolunteerId = 1,
            ProjectId = 1,
            Status = ApplicationStatus.Accepted
        };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.DeleteApplicationAsync(1);

        // Assert
        result.Should().BeTrue();
        _projectService.Verify(
            s => s.DecrementVolunteerCountAsync(1),
            Times.Once,
            "volunteer count should be decremented when accepted application is deleted");
        _appRepo.Verify(r => r.Remove(application), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteApplicationAsync_WhenApplicationWasPending_DoesNotDecrementCount()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application
        {
            Id = 1,
            VolunteerId = 1,
            ProjectId = 1,
            Status = ApplicationStatus.Pending
        };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.DeleteApplicationAsync(1);

        // Assert
        result.Should().BeTrue();
        _projectService.Verify(
            s => s.DecrementVolunteerCountAsync(It.IsAny<int>()),
            Times.Never,
            "volunteer count should not change for non-accepted applications");
    }

    #endregion

    #region GetApplicationByIdAsync Tests

    [Fact]
    public async Task GetApplicationByIdAsync_WhenApplicationExists_ReturnsApplication()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, VolunteerId = 1, ProjectId = 1 };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.GetApplicationByIdAsync(1);

        // Assert
        result.Should().BeSameAs(application);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_WhenApplicationNotFound_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Application?)null);

        // Act
        var result = await sut.GetApplicationByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

   

    [Fact]
    public async Task GetFilteredApplicationsAsync_WhenNoFilters_ReturnsAllApplications()
    {
        // Arrange
        var sut = CreateSut();
        var applications = new List<Application>
        {
            new() { Id = 1, ProjectId = 1, Status = ApplicationStatus.Pending },
            new() { Id = 2, ProjectId = 2, Status = ApplicationStatus.Accepted }
        };
        _appRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(applications);

        // Act
        var result = await sut.GetFilteredApplicationsAsync(null, null);

        // Assert
        result.Should().BeEquivalentTo(applications);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilteredApplicationsAsync_WhenFilteredByProjectId_ReturnsProjectApplications()
    {
        // Arrange
        var sut = CreateSut();
        var applications = new List<Application>
        {
            new() { Id = 1, ProjectId = 1, Status = ApplicationStatus.Pending },
            new() { Id = 2, ProjectId = 1, Status = ApplicationStatus.Accepted }
        };
        _appRepo.Setup(r => r.GetApplicationsByProjectAsync(1)).ReturnsAsync(applications);

        // Act
        var result = await sut.GetFilteredApplicationsAsync(1, null);

        // Assert
        result.Should().BeEquivalentTo(applications);
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.ProjectId == 1);
    }

    [Fact]
    public async Task GetFilteredApplicationsAsync_WhenFilteredByStatus_ReturnsStatusApplications()
    {
        // Arrange
        var sut = CreateSut();
        var applications = new List<Application>
        {
            new() { Id = 1, ProjectId = 1, Status = ApplicationStatus.Pending },
            new() { Id = 2, ProjectId = 2, Status = ApplicationStatus.Pending }
        };
        _appRepo.Setup(r => r.GetApplicationsByStatusAsync(ApplicationStatus.Pending))
            .ReturnsAsync(applications);

        // Act
        var result = await sut.GetFilteredApplicationsAsync(null, ApplicationStatus.Pending);

        // Assert
        result.Should().BeEquivalentTo(applications);
        result.Should().OnlyContain(a => a.Status == ApplicationStatus.Pending);
    }

    [Fact]
    public async Task GetFilteredApplicationsAsync_WhenFilteredByBoth_ReturnsCombinedFilter()
    {
        // Arrange
        var sut = CreateSut();
        var allProjectApps = new List<Application>
        {
            new() { Id = 1, ProjectId = 1, Status = ApplicationStatus.Pending },
            new() { Id = 2, ProjectId = 1, Status = ApplicationStatus.Accepted },
            new() { Id = 3, ProjectId = 1, Status = ApplicationStatus.Pending }
        };
        _appRepo.Setup(r => r.GetApplicationsByProjectAsync(1)).ReturnsAsync(allProjectApps);

        // Act
        var result = await sut.GetFilteredApplicationsAsync(1, ApplicationStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.ProjectId == 1 && a.Status == ApplicationStatus.Pending);
    }

    #endregion

    #region ApproveApplicationAsync Tests

    [Fact]
    public async Task ApproveApplicationAsync_WhenApplicationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Application?)null);

        // Act
        var result = await sut.ApproveApplicationAsync(1, "Notes");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveApplicationAsync_WhenValid_ApprovesAndIncrementsCount()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, ProjectId = 1, Status = ApplicationStatus.Pending };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);
        
        _projectRepo.Setup(r => r.HasAvailableSlotsAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.ApproveApplicationAsync(1, "Approved");

        // Assert
        result.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Accepted, "state should transition to Accepted");
        
        // Business rule: increment volunteer count
        _projectService.Verify(s => s.IncrementVolunteerCountAsync(1), Times.Once);
        
        // Decorator Pattern integration
        _notificationService.Verify(n => n.NotifyApplicationApprovedAsync(application), Times.Once);
        
        _appRepo.Verify(r => r.Update(application), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveApplicationAsync_WhenAlreadyAccepted_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, ProjectId = 1, Status = ApplicationStatus.Accepted };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);
        
        _projectRepo.Setup(r => r.HasAvailableSlotsAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.ApproveApplicationAsync(1, "Notes");

        // Assert
        result.Should().BeFalse("cannot approve already accepted application");
        _projectService.Verify(s => s.IncrementVolunteerCountAsync(It.IsAny<int>()), Times.Never);
        _notificationService.Verify(n => n.NotifyApplicationApprovedAsync(It.IsAny<Application>()), Times.Never);
    }

    #endregion

    #region RejectApplicationAsync Tests

    [Fact]
    public async Task RejectApplicationAsync_WhenApplicationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Application?)null);

        // Act
        var result = await sut.RejectApplicationAsync(1, "Notes");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RejectApplicationAsync_WhenValid_RejectsAndSendsNotification()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, ProjectId = 1, Status = ApplicationStatus.Pending };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.RejectApplicationAsync(1, "Not qualified");

        // Assert
        result.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Rejected, "state should transition to Rejected");
        
        // Decorator Pattern integration
        _notificationService.Verify(n => n.NotifyApplicationRejectedAsync(application), Times.Once);
        
        _appRepo.Verify(r => r.Update(application), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RejectApplicationAsync_WhenAlreadyRejected_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, ProjectId = 1, Status = ApplicationStatus.Rejected };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.RejectApplicationAsync(1, "Notes");

        // Assert
        result.Should().BeFalse("cannot reject already rejected application");
        _notificationService.Verify(n => n.NotifyApplicationRejectedAsync(It.IsAny<Application>()), Times.Never);
    }

    #endregion

    #region WithdrawApplicationAsync Tests

    [Fact]
    public async Task WithdrawApplicationAsync_WhenApplicationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Application?)null);

        // Act
        var result = await sut.WithdrawApplicationAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WithdrawApplicationAsync_WhenPreviouslyAccepted_DecrementsVolunteerCount()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application
        {
            Id = 1,
            ProjectId = 1,
            Status = ApplicationStatus.Accepted
        };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.WithdrawApplicationAsync(1);

        // Assert
        result.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Withdrawn, "state should transition to Withdrawn");
        
        // Business rule: decrement count for accepted applications
        _projectService.Verify(s => s.DecrementVolunteerCountAsync(1), Times.Once);
        
        // Decorator Pattern integration
        _notificationService.Verify(n => n.NotifyApplicationWithdrawnAsync(application), Times.Once);
    }

    [Fact]
    public async Task WithdrawApplicationAsync_WhenPreviouslyPending_DoesNotDecrementCount()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application
        {
            Id = 1,
            ProjectId = 1,
            Status = ApplicationStatus.Pending
        };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.WithdrawApplicationAsync(1);

        // Assert
        result.Should().BeTrue();
        application.Status.Should().Be(ApplicationStatus.Withdrawn, "state should transition to Withdrawn");
        _projectService.Verify(s => s.DecrementVolunteerCountAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawApplicationAsync_WhenAlreadyWithdrawn_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, VolunteerId = 1, ProjectId = 1, Status = ApplicationStatus.Withdrawn };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.WithdrawApplicationAsync(1);

        // Assert
        result.Should().BeFalse("already withdrawn applications cannot be withdrawn again");
    }

    [Fact]
    public async Task CanApplyToProjectAsync_WhenProjectCannotAcceptVolunteers_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.CanAcceptVolunteersAsync(1)).ReturnsAsync(false);

        // Act
        var result = await sut.CanApplyToProjectAsync(1, 1);

        // Assert
        result.Should().BeFalse("project cannot accept volunteers");
    }

    [Fact]
    public async Task CanApplyToProjectAsync_WhenVolunteerNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.CanAcceptVolunteersAsync(1)).ReturnsAsync(true);
        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Volunteer?)null);

        // Act
        var result = await sut.CanApplyToProjectAsync(1, 1);

        // Assert
        result.Should().BeFalse("volunteer does not exist");
    }

    [Fact]
    public async Task CanApplyToProjectAsync_WhenVolunteerNotActive_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.CanAcceptVolunteersAsync(1)).ReturnsAsync(true);
        
        var volunteer = new Volunteer { Id = 1, IsActive = false };
        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(volunteer);

        // Act
        var result = await sut.CanApplyToProjectAsync(1, 1);

        // Assert
        result.Should().BeFalse("volunteer is not active");
    }

    [Fact]
    public async Task CanApplyToProjectAsync_WhenVolunteerAlreadyApplied_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.CanAcceptVolunteersAsync(1)).ReturnsAsync(true);
        
        var volunteer = new Volunteer { Id = 1, IsActive = true };
        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(volunteer);
        _appRepo.Setup(r => r.HasVolunteerAppliedAsync(1, 1)).ReturnsAsync(true);

        // Act
        var result = await sut.CanApplyToProjectAsync(1, 1);

        // Assert
        result.Should().BeFalse("volunteer has already applied to this project");
    }

    [Fact]
    public async Task CanApplyToProjectAsync_WhenAllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        _projectService.Setup(s => s.CanAcceptVolunteersAsync(1)).ReturnsAsync(true);
        
        var volunteer = new Volunteer { Id = 1, IsActive = true };
        _volunteerRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(volunteer);
        _appRepo.Setup(r => r.HasVolunteerAppliedAsync(1, 1)).ReturnsAsync(false);

        // Act
        var result = await sut.CanApplyToProjectAsync(1, 1);

        // Assert
        result.Should().BeTrue("all conditions are met");
    }

    #endregion

    #region CanApproveApplicationAsync Tests

    [Fact]
    public async Task CanApproveApplicationAsync_WhenApplicationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Application?)null);

        // Act
        var result = await sut.CanApproveApplicationAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanApproveApplicationAsync_WhenStateDoesNotAllowApproval_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, Status = ApplicationStatus.Rejected };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.CanApproveApplicationAsync(1);

        // Assert
        result.Should().BeFalse("rejected applications cannot be approved");
    }

    [Fact]
    public async Task CanApproveApplicationAsync_WhenProjectHasNoSlots_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, ProjectId = 1, Status = ApplicationStatus.Pending };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);
        
        _projectRepo.Setup(r => r.HasAvailableSlotsAsync(1)).ReturnsAsync(false);

        // Act
        var result = await sut.CanApproveApplicationAsync(1);

        // Assert
        result.Should().BeFalse("project has no available slots");
    }

    [Fact]
    public async Task CanApproveApplicationAsync_WhenAllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, ProjectId = 1, Status = ApplicationStatus.Pending };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);
        
        _projectRepo.Setup(r => r.HasAvailableSlotsAsync(1)).ReturnsAsync(true);

        // Act
        var result = await sut.CanApproveApplicationAsync(1);

        // Assert
        result.Should().BeTrue("all conditions are met");
    }

    #endregion

    #region CanRejectApplicationAsync Tests

    [Fact]
    public async Task CanRejectApplicationAsync_WhenApplicationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Application?)null);

        // Act
        var result = await sut.CanRejectApplicationAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanRejectApplicationAsync_WhenStateAllowsRejection_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, Status = ApplicationStatus.Pending };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.CanRejectApplicationAsync(1);

        // Assert
        result.Should().BeTrue("pending applications can be rejected");
    }

    [Fact]
    public async Task CanRejectApplicationAsync_WhenAlreadyWithdrawn_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, Status = ApplicationStatus.Withdrawn };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.CanRejectApplicationAsync(1);

        // Assert
        result.Should().BeFalse("withdrawn applications cannot be rejected");
    }

    #endregion

    #region CanWithdrawApplicationAsync Tests

    [Fact]
    public async Task CanWithdrawApplicationAsync_WhenApplicationNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Application?)null);

        // Act
        var result = await sut.CanWithdrawApplicationAsync(1, 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanWithdrawApplicationAsync_WhenDifferentVolunteer_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, VolunteerId = 1, ProjectId = 1 };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.CanWithdrawApplicationAsync(1, 2);

        // Assert
        result.Should().BeFalse("volunteer can only withdraw their own applications");
    }

    [Fact]
    public async Task CanWithdrawApplicationAsync_WhenAlreadyWithdrawn_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, VolunteerId = 1, ProjectId = 1, Status = ApplicationStatus.Withdrawn };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.CanWithdrawApplicationAsync(1, 1);

        // Assert
        result.Should().BeFalse("already withdrawn applications cannot be withdrawn again");
    }

    [Fact]
    public async Task CanWithdrawApplicationAsync_WhenPending_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var application = new Application { Id = 1, VolunteerId = 1, ProjectId = 1, Status = ApplicationStatus.Pending };
        _appRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(application);

        // Act
        var result = await sut.CanWithdrawApplicationAsync(1, 1);

        // Assert
        result.Should().BeTrue("pending applications can be withdrawn");
    }

    #endregion
}
