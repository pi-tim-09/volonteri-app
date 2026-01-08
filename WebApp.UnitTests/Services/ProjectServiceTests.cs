using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.UnitTests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<ProjectService>> _logger = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IApplicationRepository> _appRepo = new();

    private ProjectService CreateSut()
    {
        _unitOfWork.SetupGet(x => x.Projects).Returns(_projectRepo.Object);
        _unitOfWork.SetupGet(x => x.Applications).Returns(_appRepo.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        return new ProjectService(_unitOfWork.Object, _logger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenUnitOfWorkIsNull_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new ProjectService(null!, _logger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new ProjectService(_unitOfWork.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region CreateProjectAsync Tests

    [Fact]
    public async Task CreateProjectAsync_WhenProjectIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.CreateProjectAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("project");
    }

    [Fact]
    public async Task CreateProjectAsync_WhenValid_SetsBusinessRulesCorrectly()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Title = "Test Project",
            Description = "Test Description",
            OrganizationId = 1,
            MaxVolunteers = 10
        };

        Project? capturedProject = null;
        _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .Callback<Project>(p => capturedProject = p)
            .ReturnsAsync((Project p) => p);

        // Act
        var result = await sut.CreateProjectAsync(project);

        // Assert
        capturedProject.Should().NotBeNull();
        capturedProject!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        capturedProject.Status.Should().Be(ProjectStatus.Draft, "new projects should start as drafts");
        capturedProject.CurrentVolunteers.Should().Be(0, "new projects should have zero volunteers");
    }

    [Fact]
    public async Task CreateProjectAsync_WhenValid_CallsRepositoryAndSavesChanges()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Title = "Test Project",
            Description = "Test Description",
            OrganizationId = 1,
            MaxVolunteers = 10
        };

        _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .ReturnsAsync(project);

        // Act
        var result = await sut.CreateProjectAsync(project);

        // Assert
        _projectRepo.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        result.Should().BeSameAs(project);
    }

    #endregion

    #region UpdateProjectAsync Tests

    [Fact]
    public async Task UpdateProjectAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        var updateData = new Project
        {
            Id = 1,
            Title = "Updated Title"
        };

        // Act
        var result = await sut.UpdateProjectAsync(1, updateData);

        // Assert
        result.Should().BeFalse();
        _projectRepo.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProjectAsync_WhenProjectFound_UpdatesAllowedFieldsOnly()
    {
        // Arrange
        var sut = CreateSut();
        var existingProject = new Project
        {
            Id = 1,
            Title = "Old Title",
            Description = "Old Description",
            Location = "Old Location",
            City = "Old City",
            Status = ProjectStatus.Draft,
            CurrentVolunteers = 5,
            MaxVolunteers = 10,
            OrganizationId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingProject);

        var updateData = new Project
        {
            Id = 1,
            Title = "New Title",
            Description = "New Description",
            Location = "New Location",
            City = "New City",
            MaxVolunteers = 20,
            RequiredSkills = new List<string> { "Skill1", "Skill2" },
            Categories = new List<string> { "Cat1" },
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(15),
            ApplicationDeadline = DateTime.UtcNow.AddDays(3)
        };

        // Act
        var result = await sut.UpdateProjectAsync(1, updateData);

        // Assert
        result.Should().BeTrue();
        existingProject.Title.Should().Be("New Title");
        existingProject.Description.Should().Be("New Description");
        existingProject.Location.Should().Be("New Location");
        existingProject.City.Should().Be("New City");
        existingProject.MaxVolunteers.Should().Be(20);
        existingProject.RequiredSkills.Should().BeEquivalentTo(new List<string> { "Skill1", "Skill2" });
        existingProject.Categories.Should().BeEquivalentTo(new List<string> { "Cat1" });
        existingProject.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Business rule: Status and CurrentVolunteers should NOT be updated via UpdateProjectAsync
        existingProject.Status.Should().Be(ProjectStatus.Draft);
        existingProject.CurrentVolunteers.Should().Be(5);
        
        _projectRepo.Verify(r => r.Update(existingProject), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region DeleteProjectAsync Tests

    [Fact]
    public async Task DeleteProjectAsync_WhenProjectHasAcceptedApplications_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetAcceptedApplicationsCountAsync(1)).ReturnsAsync(2);

        // Act
        Func<Task> act = async () => await sut.DeleteProjectAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Project cannot be deleted - it has accepted applications.");
    }

    [Fact]
    public async Task DeleteProjectAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetAcceptedApplicationsCountAsync(1)).ReturnsAsync(0);
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.DeleteProjectAsync(1);

        // Assert
        result.Should().BeFalse();
        _projectRepo.Verify(r => r.Remove(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProjectAsync_WhenProjectFoundAndNoAcceptedApplications_DeletesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project { Id = 1, Title = "Test Project" };
        
        _appRepo.Setup(r => r.GetAcceptedApplicationsCountAsync(1)).ReturnsAsync(0);
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.DeleteProjectAsync(1);

        // Assert
        result.Should().BeTrue();
        _projectRepo.Verify(r => r.Remove(project), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GetProjectByIdAsync Tests

    [Fact]
    public async Task GetProjectByIdAsync_WhenProjectExists_ReturnsProject()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project { Id = 1, Title = "Test Project" };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.GetProjectByIdAsync(1);

        // Assert
        result.Should().BeSameAs(project);
        _projectRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetProjectByIdAsync_WhenProjectNotFound_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.GetProjectByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllProjectsAsync Tests

    [Fact]
    public async Task GetAllProjectsAsync_ReturnsAllProjects()
    {
        // Arrange
        var sut = CreateSut();
        var projects = new List<Project>
        {
            new() { Id = 1, Title = "Project 1" },
            new() { Id = 2, Title = "Project 2" },
            new() { Id = 3, Title = "Project 3" }
        };
        _projectRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(projects);

        // Act
        var result = await sut.GetAllProjectsAsync();

        // Assert
        result.Should().BeEquivalentTo(projects);
        result.Should().HaveCount(3);
        _projectRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllProjectsAsync_WhenNoProjects_ReturnsEmptyCollection()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Project>());

        // Act
        var result = await sut.GetAllProjectsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetProjectsByOrganizationAsync Tests

    [Fact]
    public async Task GetProjectsByOrganizationAsync_ReturnsProjectsForOrganization()
    {
        // Arrange
        var sut = CreateSut();
        var projects = new List<Project>
        {
            new() { Id = 1, Title = "Org 1 Project 1", OrganizationId = 1 },
            new() { Id = 2, Title = "Org 1 Project 2", OrganizationId = 1 }
        };
        _projectRepo.Setup(r => r.GetProjectsByOrganizationAsync(1)).ReturnsAsync(projects);

        // Act
        var result = await sut.GetProjectsByOrganizationAsync(1);

        // Assert
        result.Should().BeEquivalentTo(projects);
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.OrganizationId == 1);
    }

    #endregion

    #region PublishProjectAsync Tests

    [Fact]
    public async Task PublishProjectAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.PublishProjectAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PublishProjectAsync_WhenProjectNotDraft_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Status = ProjectStatus.Published
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.PublishProjectAsync(1);

        // Assert
        result.Should().BeFalse("can only publish draft projects");
    }

    [Fact]
    public async Task PublishProjectAsync_WhenMaxVolunteersIsZero_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Status = ProjectStatus.Draft,
            MaxVolunteers = 0,
            ApplicationDeadline = DateTime.UtcNow.AddDays(5)
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.PublishProjectAsync(1);

        // Assert
        result.Should().BeFalse("cannot publish project with zero max volunteers");
    }

    [Fact]
    public async Task PublishProjectAsync_WhenDeadlineInPast_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Status = ProjectStatus.Draft,
            MaxVolunteers = 10,
            ApplicationDeadline = DateTime.UtcNow.AddDays(-1)
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.PublishProjectAsync(1);

        // Assert
        result.Should().BeFalse("cannot publish project with past deadline");
    }

    [Fact]
    public async Task PublishProjectAsync_WhenValid_PublishesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Status = ProjectStatus.Draft,
            MaxVolunteers = 10,
            ApplicationDeadline = DateTime.UtcNow.AddDays(5)
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.PublishProjectAsync(1);

        // Assert
        result.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Published);
        _projectRepo.Verify(r => r.Update(project), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region CompleteProjectAsync Tests

    [Fact]
    public async Task CompleteProjectAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.CompleteProjectAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteProjectAsync_WhenProjectDraft_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Status = ProjectStatus.Draft
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CompleteProjectAsync(1);

        // Assert
        result.Should().BeFalse("can only complete published or in-progress projects");
    }

    [Fact]
    public async Task CompleteProjectAsync_WhenProjectPublished_CompletesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Status = ProjectStatus.Published
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CompleteProjectAsync(1);

        // Assert
        result.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Completed);
        _projectRepo.Verify(r => r.Update(project), Times.Once);
    }

    [Fact]
    public async Task CompleteProjectAsync_WhenProjectInProgress_CompletesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Status = ProjectStatus.InProgress
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CompleteProjectAsync(1);

        // Assert
        result.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Completed);
    }

    #endregion

    #region CancelProjectAsync Tests

    [Fact]
    public async Task CancelProjectAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.CancelProjectAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelProjectAsync_WhenProjectFound_CancelsSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            Status = ProjectStatus.Published
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CancelProjectAsync(1);

        // Assert
        result.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Cancelled);
        _projectRepo.Verify(r => r.Update(project), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region CanAcceptVolunteersAsync Tests

    [Fact]
    public async Task CanAcceptVolunteersAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.CanAcceptVolunteersAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAcceptVolunteersAsync_WhenProjectNotPublished_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Status = ProjectStatus.Draft,
            ApplicationDeadline = DateTime.UtcNow.AddDays(5),
            CurrentVolunteers = 0,
            MaxVolunteers = 10
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CanAcceptVolunteersAsync(1);

        // Assert
        result.Should().BeFalse("project must be published");
    }

    [Fact]
    public async Task CanAcceptVolunteersAsync_WhenDeadlinePassed_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Status = ProjectStatus.Published,
            ApplicationDeadline = DateTime.UtcNow.AddDays(-1),
            CurrentVolunteers = 0,
            MaxVolunteers = 10
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CanAcceptVolunteersAsync(1);

        // Assert
        result.Should().BeFalse("deadline has passed");
    }

    [Fact]
    public async Task CanAcceptVolunteersAsync_WhenAtMaxCapacity_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Status = ProjectStatus.Published,
            ApplicationDeadline = DateTime.UtcNow.AddDays(5),
            CurrentVolunteers = 10,
            MaxVolunteers = 10
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CanAcceptVolunteersAsync(1);

        // Assert
        result.Should().BeFalse("project is at maximum capacity");
    }

    [Fact]
    public async Task CanAcceptVolunteersAsync_WhenAllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Status = ProjectStatus.Published,
            ApplicationDeadline = DateTime.UtcNow.AddDays(5),
            CurrentVolunteers = 5,
            MaxVolunteers = 10
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CanAcceptVolunteersAsync(1);

        // Assert
        result.Should().BeTrue("all conditions are met");
    }

    #endregion

    #region IncrementVolunteerCountAsync Tests

    [Fact]
    public async Task IncrementVolunteerCountAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.IncrementVolunteerCountAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementVolunteerCountAsync_WhenAtMaxCapacity_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            CurrentVolunteers = 10,
            MaxVolunteers = 10
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.IncrementVolunteerCountAsync(1);

        // Assert
        result.Should().BeFalse("cannot exceed max volunteers");
        project.CurrentVolunteers.Should().Be(10, "count should not change");
    }

    [Fact]
    public async Task IncrementVolunteerCountAsync_WhenBelowCapacity_IncrementsSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            CurrentVolunteers = 5,
            MaxVolunteers = 10
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.IncrementVolunteerCountAsync(1);

        // Assert
        result.Should().BeTrue();
        project.CurrentVolunteers.Should().Be(6);
        _projectRepo.Verify(r => r.Update(project), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region DecrementVolunteerCountAsync Tests

    [Fact]
    public async Task DecrementVolunteerCountAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.DecrementVolunteerCountAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DecrementVolunteerCountAsync_WhenAlreadyZero_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            CurrentVolunteers = 0,
            MaxVolunteers = 10
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.DecrementVolunteerCountAsync(1);

        // Assert
        result.Should().BeFalse("cannot go below zero");
        project.CurrentVolunteers.Should().Be(0, "count should not change");
    }

    [Fact]
    public async Task DecrementVolunteerCountAsync_WhenAboveZero_DecrementsSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            CurrentVolunteers = 5,
            MaxVolunteers = 10
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.DecrementVolunteerCountAsync(1);

        // Assert
        result.Should().BeTrue();
        project.CurrentVolunteers.Should().Be(4);
        _projectRepo.Verify(r => r.Update(project), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region ProjectExistsAsync Tests

    [Fact]
    public async Task ProjectExistsAsync_WhenProjectExists_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await sut.ProjectExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProjectExistsAsync_WhenProjectDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await sut.ProjectExistsAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CanEditProjectAsync Tests

    [Fact]
    public async Task CanEditProjectAsync_WhenProjectNotFound_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.CanEditProjectAsync(1, 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanEditProjectAsync_WhenDifferentOrganization_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            OrganizationId = 1
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CanEditProjectAsync(1, 2);

        // Assert
        result.Should().BeFalse("only owning organization can edit");
    }

    [Fact]
    public async Task CanEditProjectAsync_WhenSameOrganization_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            OrganizationId = 1
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.CanEditProjectAsync(1, 1);

        // Assert
        result.Should().BeTrue("owning organization can edit");
    }

    #endregion

    #region CanDeleteProjectAsync Tests

    [Fact]
    public async Task CanDeleteProjectAsync_WhenHasAcceptedApplications_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetAcceptedApplicationsCountAsync(1)).ReturnsAsync(3);

        // Act
        var result = await sut.CanDeleteProjectAsync(1);

        // Assert
        result.Should().BeFalse("cannot delete projects with accepted applications");
    }

    [Fact]
    public async Task CanDeleteProjectAsync_WhenNoAcceptedApplications_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        _appRepo.Setup(r => r.GetAcceptedApplicationsCountAsync(1)).ReturnsAsync(0);

        // Act
        var result = await sut.CanDeleteProjectAsync(1);

        // Assert
        result.Should().BeTrue("can delete projects without accepted applications");
    }

    #endregion

    #region IsApplicationDeadlinePassedAsync Tests

    [Fact]
    public async Task IsApplicationDeadlinePassedAsync_WhenProjectNotFound_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Project?)null);

        // Act
        var result = await sut.IsApplicationDeadlinePassedAsync(1);

        // Assert
        result.Should().BeTrue("treat not found as deadline passed");
    }

    [Fact]
    public async Task IsApplicationDeadlinePassedAsync_WhenDeadlineInFuture_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            ApplicationDeadline = DateTime.UtcNow.AddDays(5)
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.IsApplicationDeadlinePassedAsync(1);

        // Assert
        result.Should().BeFalse("deadline is in the future");
    }

    [Fact]
    public async Task IsApplicationDeadlinePassedAsync_WhenDeadlineInPast_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var project = new Project
        {
            Id = 1,
            Title = "Test Project",
            ApplicationDeadline = DateTime.UtcNow.AddDays(-1)
        };
        _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

        // Act
        var result = await sut.IsApplicationDeadlinePassedAsync(1);

        // Assert
        result.Should().BeTrue("deadline has passed");
    }

    #endregion
}
