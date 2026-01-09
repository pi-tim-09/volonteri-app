using FluentAssertions;
using WebApp.Models;
using WebApp.Patterns.Creational;

namespace WebApp.UnitTests.Patterns.Creational;

public class AdminFactoryTests
{
   

    [Fact]
    public void SupportedRole_ReturnsAdmin()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var supportedRole = factory.SupportedRole;

        // Assert
        supportedRole.Should().Be(UserRole.Admin);
    }

 

    

    [Fact]
    public void CreateUser_CreatesAdminWithCorrectBasicProperties()
    {
        // Arrange
        var factory = new AdminFactory();
        var email = "admin@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var phoneNumber = "123456789";

        // Act
        var result = factory.CreateUser(email, firstName, lastName, phoneNumber);

        // Assert
        result.Should().BeOfType<Admin>();
        var admin = (Admin)result;
        admin.Email.Should().Be(email);
        admin.FirstName.Should().Be(firstName);
        admin.LastName.Should().Be(lastName);
        admin.PhoneNumber.Should().Be(phoneNumber);
    }

    [Fact]
    public void CreateUser_SetsRoleToAdmin()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var result = factory.CreateUser("admin@example.com", "John", "Doe", "123456789");

        // Assert
        result.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void CreateUser_SetsIsActiveToTrue()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var result = factory.CreateUser("admin@example.com", "John", "Doe", "123456789");

        // Assert
        result.IsActive.Should().BeTrue("new admins should be active by default");
    }

    [Fact]
    public void CreateUser_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var result = factory.CreateUser("admin@example.com", "John", "Doe", "123456789");

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateUser_SetsPermissionsToFalse()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var result = factory.CreateUser("admin@example.com", "John", "Doe", "123456789");
        var admin = (Admin)result;

        // Assert
        admin.CanManageUsers.Should().BeFalse("new admins should have no permissions by default (business rule)");
        admin.CanManageOrganizations.Should().BeFalse("new admins should have no permissions by default (business rule)");
        admin.CanManageProjects.Should().BeFalse("new admins should have no permissions by default (business rule)");
    }

    [Fact]
    public void CreateUser_InitializesAdminSpecificPropertiesToEmptyStrings()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var result = factory.CreateUser("admin@example.com", "John", "Doe", "123456789");
        var admin = (Admin)result;

        // Assert
        admin.Department.Should().Be(string.Empty);
    }

    [Fact]
    public void CreateUser_WithDifferentInputs_CreatesDistinctAdmins()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var admin1 = factory.CreateUser("admin1@example.com", "First", "Admin", "111111111");
        var admin2 = factory.CreateUser("admin2@example.com", "Second", "Admin", "222222222");

        // Assert
        admin1.Should().NotBeSameAs(admin2, "factory should create new instances");
        admin1.Email.Should().Be("admin1@example.com");
        admin2.Email.Should().Be("admin2@example.com");
    }

    [Fact]
    public void CreateUser_ImplementsIUserFactoryInterface()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act & Assert
        factory.Should().BeAssignableTo<IUserFactory>("AdminFactory should implement IUserFactory");
    }

    [Fact]
    public void CreateUser_ReturnsUserBaseType()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var result = factory.CreateUser("admin@example.com", "John", "Doe", "123456789");

        // Assert
        result.Should().BeAssignableTo<User>("factory should return User base type");
    }

    [Fact]
    public void CreateUser_WithEmptyStrings_CreatesAdmin()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var result = factory.CreateUser("", "", "", "");
        var admin = (Admin)result;

        // Assert
        admin.Email.Should().Be("");
        admin.FirstName.Should().Be("");
        admin.LastName.Should().Be("");
        admin.PhoneNumber.Should().Be("");
        admin.Role.Should().Be(UserRole.Admin);
        admin.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateUser_VerifiesSecurityDefaultOfNoPermissions()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var result = factory.CreateUser("newadmin@example.com", "New", "Admin", "999999999");
        var admin = (Admin)result;

        // Assert 
        var allPermissionsFalse = !admin.CanManageUsers && 
                                   !admin.CanManageOrganizations && 
                                   !admin.CanManageProjects;
        allPermissionsFalse.Should().BeTrue("security rule: new admins must have all permissions disabled");
    }

  

   

    [Fact]
    public void CreateUser_CanBeUsedByUserFactoryProvider()
    {
        // Arrange
        var adminFactory = new AdminFactory();
        var volunteerFactory = new VolunteerFactory();
        var organizationFactory = new OrganizationFactory();
        var factories = new List<IUserFactory> { adminFactory, volunteerFactory, organizationFactory };
        var provider = new UserFactoryProvider(factories);

        // Act
        var result = provider.CreateUser(UserRole.Admin, "admin@example.com", "John", "Doe", "123456789");

        // Assert
        result.Should().BeOfType<Admin>();
        result.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void CreateUser_ValidatesBusinessRuleForPermissions()
    {
        // Arrange
        var factory = new AdminFactory();

        // Act
        var admin1 = factory.CreateUser("admin1@example.com", "Admin", "One", "111111111") as Admin;
        var admin2 = factory.CreateUser("admin2@example.com", "Admin", "Two", "222222222") as Admin;
        var admin3 = factory.CreateUser("admin3@example.com", "Admin", "Three", "333333333") as Admin;

        // Assert 
        var allAdminsHaveNoPermissions = 
            admin1!.CanManageUsers == false &&
            admin2!.CanManageOrganizations == false &&
            admin3!.CanManageProjects == false;

        allAdminsHaveNoPermissions.Should().BeTrue("business rule: all new admins must be created without permissions");
    }

 
}
