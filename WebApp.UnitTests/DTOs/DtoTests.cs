using FluentAssertions;
using WebApp.DTOs.Admins;
using WebApp.DTOs.Applications;
using WebApp.DTOs.Projects;
using WebApp.DTOs.Users;
using WebApp.Models;

namespace WebApp.UnitTests.DTOs;

public class DtoTests
{
    

    [Fact]
    public void AdminDto_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new AdminDto
        {
            Id = 1,
            Email = "admin@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123456789",
            Department = "IT",
            CanManageUsers = true,
            CanManageOrganizations = false,
            CanManageProjects = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.Email.Should().Be("admin@example.com");
        dto.FirstName.Should().Be("John");
        dto.LastName.Should().Be("Doe");
        dto.PhoneNumber.Should().Be("123456789");
        dto.Department.Should().Be("IT");
        dto.CanManageUsers.Should().BeTrue();
        dto.CanManageOrganizations.Should().BeFalse();
        dto.CanManageProjects.Should().BeTrue();
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dto.LastLoginAt.Should().NotBeNull();
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AdminListDto_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var admins = new List<AdminDto>
        {
            new() { Id = 1, Email = "admin1@example.com" },
            new() { Id = 2, Email = "admin2@example.com" }
        };

        var dto = new AdminListDto
        {
            Admins = admins,
            TotalCount = 2
        };

        // Assert
        dto.Admins.Should().HaveCount(2);
        dto.TotalCount.Should().Be(2);
    }

    [Fact]
    public void CreateAdminRequest_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new CreateAdminRequest
        {
            Email = "newadmin@example.com",
            Password = "SecurePassword123!",
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "987654321",
            Department = "HR",
            CanManageUsers = false,
            CanManageOrganizations = true,
            CanManageProjects = false
        };

        // Assert
        dto.Email.Should().Be("newadmin@example.com");
        dto.Password.Should().Be("SecurePassword123!");
        dto.FirstName.Should().Be("Jane");
        dto.LastName.Should().Be("Smith");
        dto.PhoneNumber.Should().Be("987654321");
        dto.Department.Should().Be("HR");
        dto.CanManageUsers.Should().BeFalse();
        dto.CanManageOrganizations.Should().BeTrue();
        dto.CanManageProjects.Should().BeFalse();
    }

    [Fact]
    public void UpdateAdminRequest_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new UpdateAdminRequest
        {
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "Name",
            PhoneNumber = "111222333",
            Department = "Finance",
            CanManageUsers = true,
            CanManageOrganizations = true,
            CanManageProjects = true,
            IsActive = false
        };

        // Assert
        dto.Email.Should().Be("updated@example.com");
        dto.FirstName.Should().Be("Updated");
        dto.LastName.Should().Be("Name");
        dto.PhoneNumber.Should().Be("111222333");
        dto.Department.Should().Be("Finance");
        dto.CanManageUsers.Should().BeTrue();
        dto.CanManageOrganizations.Should().BeTrue();
        dto.CanManageProjects.Should().BeTrue();
        dto.IsActive.Should().BeFalse();
    }

    

    

    [Fact]
    public void ApplicationDto_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new ApplicationDto
        {
            Id = 1,
            VolunteerId = 10,
            VolunteerName = "John Volunteer",
            ProjectId = 20,
            ProjectTitle = "Test Project",
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow,
            ReviewedAt = null,
            ReviewNotes = null
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.VolunteerId.Should().Be(10);
        dto.VolunteerName.Should().Be("John Volunteer");
        dto.ProjectId.Should().Be(20);
        dto.ProjectTitle.Should().Be("Test Project");
        dto.Status.Should().Be(ApplicationStatus.Pending);
        dto.AppliedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dto.ReviewedAt.Should().BeNull();
        dto.ReviewNotes.Should().BeNull();
    }

    [Fact]
    public void ApplicationListDto_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var applications = new List<ApplicationDto>
        {
            new() { Id = 1, ProjectTitle = "Project 1" },
            new() { Id = 2, ProjectTitle = "Project 2" }
        };

        var dto = new ApplicationListDto
        {
            Applications = applications,
            TotalCount = 2
        };

        // Assert
        dto.Applications.Should().HaveCount(2);
        dto.TotalCount.Should().Be(2);
    }

    [Fact]
    public void CreateApplicationRequest_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new CreateApplicationRequest
        {
            VolunteerId = 5,
            ProjectId = 15
        };

        // Assert
        dto.VolunteerId.Should().Be(5);
        dto.ProjectId.Should().Be(15);
    }

    [Fact]
    public void ReviewApplicationRequest_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new ReviewApplicationRequest
        {
            Status = ApplicationStatus.Accepted,
            ReviewNotes = "Great application"
        };

        // Assert
        dto.Status.Should().Be(ApplicationStatus.Accepted);
        dto.ReviewNotes.Should().Be("Great application");
    }

    

  

    [Fact]
    public void ProjectDto_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new ProjectDto
        {
            Id = 1,
            Title = "Community Cleanup",
            Description = "Clean the park",
            Location = "Park Area",
            OrganizationId = 5,
            OrganizationName = "Green Org",
            City = "Zagreb",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            MaxVolunteers = 20,
            CurrentVolunteers = 5,
            Status = ProjectStatus.Published,
            Categories = new List<string> { "Environment", "Community" },
            RequiredSkills = new List<string> { "Teamwork" },
            ApplicationDeadline = DateTime.UtcNow.AddDays(3),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.Title.Should().Be("Community Cleanup");
        dto.Description.Should().Be("Clean the park");
        dto.Location.Should().Be("Park Area");
        dto.OrganizationId.Should().Be(5);
        dto.OrganizationName.Should().Be("Green Org");
        dto.City.Should().Be("Zagreb");
        dto.MaxVolunteers.Should().Be(20);
        dto.CurrentVolunteers.Should().Be(5);
        dto.Status.Should().Be(ProjectStatus.Published);
        dto.Categories.Should().Contain("Environment");
        dto.RequiredSkills.Should().Contain("Teamwork");
    }

    [Fact]
    public void ProjectListDto_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var projects = new List<ProjectDto>
        {
            new() { Id = 1, Title = "Project 1" },
            new() { Id = 2, Title = "Project 2" }
        };

        var dto = new ProjectListDto
        {
            Projects = projects,
            TotalCount = 2
        };

        // Assert
        dto.Projects.Should().HaveCount(2);
        dto.TotalCount.Should().Be(2);
    }

    [Fact]
    public void CreateProjectRequest_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new CreateProjectRequest
        {
            Title = "New Project",
            Description = "Description here",
            Location = "Location here",
            City = "Split",
            MaxVolunteers = 15,
            Categories = new List<string> { "Education" },
            RequiredSkills = new List<string> { "Teaching" },
            ApplicationDeadline = DateTime.UtcNow.AddDays(5),
            OrganizationId = 3
        };

        // Assert
        dto.Title.Should().Be("New Project");
        dto.Description.Should().Be("Description here");
        dto.Location.Should().Be("Location here");
        dto.City.Should().Be("Split");
        dto.MaxVolunteers.Should().Be(15);
        dto.Categories.Should().Contain("Education");
        dto.RequiredSkills.Should().Contain("Teaching");
        dto.OrganizationId.Should().Be(3);
    }

    [Fact]
    public void UpdateProjectRequest_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new UpdateProjectRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Location = "New Location",
            City = "Rijeka",
            MaxVolunteers = 25,
            Status = ProjectStatus.Completed,
            Categories = new List<string> { "Health" },
            RequiredSkills = new List<string> { "Medical" },
            ApplicationDeadline = DateTime.UtcNow.AddDays(2),
            OrganizationId = 7
        };

        // Assert
        dto.Title.Should().Be("Updated Title");
        dto.Description.Should().Be("Updated Description");
        dto.Location.Should().Be("New Location");
        dto.City.Should().Be("Rijeka");
        dto.MaxVolunteers.Should().Be(25);
        dto.Status.Should().Be(ProjectStatus.Completed);
        dto.Categories.Should().Contain("Health");
        dto.RequiredSkills.Should().Contain("Medical");
        dto.OrganizationId.Should().Be(7);
    }

   

   

    [Fact]
    public void ChangePasswordRequest_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456!",
            ConfirmNewPassword = "NewPass456!"
        };

        // Assert
        dto.CurrentPassword.Should().Be("OldPass123!");
        dto.NewPassword.Should().Be("NewPass456!");
        dto.ConfirmNewPassword.Should().Be("NewPass456!");
    }

    [Fact]
    public void UpdateUserRequest_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new UpdateUserRequest
        {
            Email = "user@example.com",
            FirstName = "Updated",
            LastName = "User",
            PhoneNumber = "555-1234",
            IsActive = true
        };

        // Assert
        dto.Email.Should().Be("user@example.com");
        dto.FirstName.Should().Be("Updated");
        dto.LastName.Should().Be("User");
        dto.PhoneNumber.Should().Be("555-1234");
        dto.IsActive.Should().BeTrue();
    }

  

    

    [Fact]
    public void AdminDto_DefaultValues_AreCorrect()
    {
        // Act
        var dto = new AdminDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Email.Should().NotBeNull(); 
        dto.CanManageUsers.Should().BeFalse();
        dto.CanManageOrganizations.Should().BeFalse();
        dto.CanManageProjects.Should().BeFalse();
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ApplicationDto_DefaultValues_AreCorrect()
    {
        // Act
        var dto = new ApplicationDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.VolunteerId.Should().Be(0);
        dto.ProjectId.Should().Be(0);
        dto.Status.Should().Be(ApplicationStatus.Pending);
    }

    [Fact]
    public void ProjectDto_DefaultValues_AreCorrect()
    {
        // Act
        var dto = new ProjectDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Title.Should().NotBeNull();
        dto.Description.Should().NotBeNull();
        dto.MaxVolunteers.Should().Be(0);
        dto.CurrentVolunteers.Should().Be(0);
    }

    

    

    [Fact]
    public void AdminListDto_WithEmptyList_Works()
    {
        // Arrange & Act
        var dto = new AdminListDto
        {
            Admins = new List<AdminDto>(),
            TotalCount = 0
        };

        // Assert
        dto.Admins.Should().BeEmpty();
        dto.TotalCount.Should().Be(0);
    }

    [Fact]
    public void ApplicationListDto_WithEmptyList_Works()
    {
        // Arrange & Act
        var dto = new ApplicationListDto
        {
            Applications = new List<ApplicationDto>(),
            TotalCount = 0
        };

        // Assert
        dto.Applications.Should().BeEmpty();
        dto.TotalCount.Should().Be(0);
    }

    [Fact]
    public void ProjectListDto_WithEmptyList_Works()
    {
        // Arrange & Act
        var dto = new ProjectListDto
        {
            Projects = new List<ProjectDto>(),
            TotalCount = 0
        };

        // Assert
        dto.Projects.Should().BeEmpty();
        dto.TotalCount.Should().Be(0);
    }

    

    

    [Fact]
    public void ApplicationDto_WithNullReviewData_AllowsNull()
    {
        // Arrange & Act
        var dto = new ApplicationDto
        {
            Id = 1,
            ReviewedAt = null,
            ReviewNotes = null
        };

        // Assert
        dto.ReviewedAt.Should().BeNull();
        dto.ReviewNotes.Should().BeNull();
    }

    [Fact]
    public void AdminDto_WithNullLastLoginAt_AllowsNull()
    {
        // Arrange & Act
        var dto = new AdminDto
        {
            Id = 1,
            Email = "admin@example.com",
            LastLoginAt = null
        };

        // Assert
        dto.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void ProjectDto_WithNullUpdatedAt_AllowsNull()
    {
        // Arrange & Act
        var dto = new ProjectDto
        {
            Id = 1,
            Title = "Test",
            UpdatedAt = null
        };

        // Assert
        dto.UpdatedAt.Should().BeNull();
    }

    
}
