using FluentAssertions;
using WebApp.Models;
using WebApp.Patterns.Creational;

namespace WebApp.UnitTests.Patterns.Creational;

public class OrganizationFactoryTests
{
   

    [Fact]
    public void SupportedRole_ReturnsOrganization()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var supportedRole = factory.SupportedRole;

        // Assert
        supportedRole.Should().Be(UserRole.Organization);
    }

    

    

    [Fact]
    public void CreateUser_CreatesOrganizationWithCorrectBasicProperties()
    {
        // Arrange
        var factory = new OrganizationFactory();
        var email = "org@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var phoneNumber = "123456789";

        // Act
        var result = factory.CreateUser(email, firstName, lastName, phoneNumber);

        // Assert
        result.Should().BeOfType<Organization>();
        var organization = (Organization)result;
        organization.Email.Should().Be(email);
        organization.FirstName.Should().Be(firstName);
        organization.LastName.Should().Be(lastName);
        organization.PhoneNumber.Should().Be(phoneNumber);
    }

    [Fact]
    public void CreateUser_SetsRoleToOrganization()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var result = factory.CreateUser("org@example.com", "John", "Doe", "123456789");

        // Assert
        result.Role.Should().Be(UserRole.Organization);
    }

    [Fact]
    public void CreateUser_SetsIsActiveToTrue()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var result = factory.CreateUser("org@example.com", "John", "Doe", "123456789");

        // Assert
        result.IsActive.Should().BeTrue("new organizations should be active by default");
    }

    [Fact]
    public void CreateUser_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var result = factory.CreateUser("org@example.com", "John", "Doe", "123456789");

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateUser_SetsIsVerifiedToFalse()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var result = factory.CreateUser("org@example.com", "John", "Doe", "123456789");
        var organization = (Organization)result;

        // Assert
        organization.IsVerified.Should().BeFalse("new organizations should start unverified");
    }

    [Fact]
    public void CreateUser_InitializesOrganizationSpecificPropertiesToEmptyStrings()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var result = factory.CreateUser("org@example.com", "John", "Doe", "123456789");
        var organization = (Organization)result;

        // Assert
        organization.OrganizationName.Should().Be(string.Empty);
        organization.Description.Should().Be(string.Empty);
        organization.Address.Should().Be(string.Empty);
        organization.City.Should().Be(string.Empty);
    }

    [Fact]
    public void CreateUser_WithDifferentInputs_CreatesDistinctOrganizations()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var org1 = factory.CreateUser("org1@example.com", "First", "Org", "111111111");
        var org2 = factory.CreateUser("org2@example.com", "Second", "Org", "222222222");

        // Assert
        org1.Should().NotBeSameAs(org2, "factory should create new instances");
        org1.Email.Should().Be("org1@example.com");
        org2.Email.Should().Be("org2@example.com");
    }

    [Fact]
    public void CreateUser_ImplementsIUserFactoryInterface()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act & Assert
        factory.Should().BeAssignableTo<IUserFactory>("OrganizationFactory should implement IUserFactory");
    }

    [Fact]
    public void CreateUser_ReturnsUserBaseType()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var result = factory.CreateUser("org@example.com", "John", "Doe", "123456789");

        // Assert
        result.Should().BeAssignableTo<User>("factory should return User base type");
    }

    [Fact]
    public void CreateUser_WithEmptyStrings_CreatesOrganization()
    {
        // Arrange
        var factory = new OrganizationFactory();

        // Act
        var result = factory.CreateUser("", "", "", "");
        var organization = (Organization)result;

        // Assert
        organization.Email.Should().Be("");
        organization.FirstName.Should().Be("");
        organization.LastName.Should().Be("");
        organization.PhoneNumber.Should().Be("");
        organization.Role.Should().Be(UserRole.Organization);
        organization.IsActive.Should().BeTrue();
    }

   


    [Fact]
    public void CreateUser_CanBeUsedByUserFactoryProvider()
    {
        // Arrange
        var organizationFactory = new OrganizationFactory();
        var volunteerFactory = new VolunteerFactory();
        var adminFactory = new AdminFactory();
        var factories = new List<IUserFactory> { organizationFactory, volunteerFactory, adminFactory };
        var provider = new UserFactoryProvider(factories);

        // Act
        var result = provider.CreateUser(UserRole.Organization, "org@example.com", "John", "Doe", "123456789");

        // Assert
        result.Should().BeOfType<Organization>();
        result.Role.Should().Be(UserRole.Organization);
    }

    
}
