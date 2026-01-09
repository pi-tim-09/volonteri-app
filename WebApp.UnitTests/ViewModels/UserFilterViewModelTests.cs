using FluentAssertions;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.UnitTests.ViewModels;

public class UserFilterViewModelTests
{
    [Fact]
    public void UserFilterViewModel_DefaultValues_AreSetCorrectly()
    {
        var vm = new UserFilterViewModel();

        vm.PageNumber.Should().Be(1);
        vm.PageSize.Should().Be(10);
        vm.TotalPages.Should().Be(0);
        vm.TotalUsers.Should().Be(0);
        vm.SearchTerm.Should().BeNull();
        vm.RoleFilter.Should().BeNull();
        vm.IsActiveFilter.Should().BeNull();
        vm.Users.Should().BeEmpty();
        vm.TotalAdmins.Should().Be(0);
        vm.TotalOrganizations.Should().Be(0);
        vm.TotalVolunteers.Should().Be(0);
    }

    [Fact]
    public void UserFilterViewModel_HasPreviousPage_WhenPageNumberIsOne_ReturnsFalse()
    {
        var vm = new UserFilterViewModel { PageNumber = 1 };

        vm.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void UserFilterViewModel_HasPreviousPage_WhenPageNumberGreaterThanOne_ReturnsTrue()
    {
        var vm = new UserFilterViewModel { PageNumber = 2 };

        vm.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void UserFilterViewModel_HasNextPage_WhenPageNumberLessThanTotalPages_ReturnsTrue()
    {
        var vm = new UserFilterViewModel 
        { 
            PageNumber = 2,
            TotalPages = 5
        };

        vm.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void UserFilterViewModel_HasNextPage_WhenPageNumberEqualsToTotalPages_ReturnsFalse()
    {
        var vm = new UserFilterViewModel 
        { 
            PageNumber = 5,
            TotalPages = 5
        };

        vm.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void UserFilterViewModel_HasNextPage_WhenPageNumberGreaterThanTotalPages_ReturnsFalse()
    {
        var vm = new UserFilterViewModel 
        { 
            PageNumber = 6,
            TotalPages = 5
        };

        vm.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void UserFilterViewModel_CanSetAllProperties()
    {
        var users = new List<User>
        {
            new Volunteer { Id = 1, Email = "v@example.com" },
            new Organization { Id = 2, Email = "org@example.com" }
        };

        var vm = new UserFilterViewModel
        {
            PageNumber = 3,
            PageSize = 20,
            TotalPages = 10,
            TotalUsers = 200,
            SearchTerm = "test",
            RoleFilter = UserRole.Volunteer,
            IsActiveFilter = true,
            Users = users,
            TotalAdmins = 5,
            TotalOrganizations = 15,
            TotalVolunteers = 180
        };

        vm.PageNumber.Should().Be(3);
        vm.PageSize.Should().Be(20);
        vm.TotalPages.Should().Be(10);
        vm.TotalUsers.Should().Be(200);
        vm.SearchTerm.Should().Be("test");
        vm.RoleFilter.Should().Be(UserRole.Volunteer);
        vm.IsActiveFilter.Should().BeTrue();
        vm.Users.Should().BeEquivalentTo(users);
        vm.TotalAdmins.Should().Be(5);
        vm.TotalOrganizations.Should().Be(15);
        vm.TotalVolunteers.Should().Be(180);
    }

    [Fact]
    public void UserFilterViewModel_Statistics_CanBeUpdated()
    {
        var vm = new UserFilterViewModel
        {
            TotalAdmins = 10,
            TotalOrganizations = 20,
            TotalVolunteers = 30
        };

        vm.TotalAdmins.Should().Be(10);
        vm.TotalOrganizations.Should().Be(20);
        vm.TotalVolunteers.Should().Be(30);
    }

    [Fact]
    public void UserFilterViewModel_Filters_CanBeNull()
    {
        var vm = new UserFilterViewModel
        {
            SearchTerm = null,
            RoleFilter = null,
            IsActiveFilter = null
        };

        vm.SearchTerm.Should().BeNull();
        vm.RoleFilter.Should().BeNull();
        vm.IsActiveFilter.Should().BeNull();
    }

    [Fact]
    public void UserFilterViewModel_Filters_CanBeSet()
    {
        var vm = new UserFilterViewModel
        {
            SearchTerm = "search text",
            RoleFilter = UserRole.Admin,
            IsActiveFilter = false
        };

        vm.SearchTerm.Should().Be("search text");
        vm.RoleFilter.Should().Be(UserRole.Admin);
        vm.IsActiveFilter.Should().BeFalse();
    }
}
