using FluentAssertions;
using WebApp.Services;

namespace WebApp.UnitTests.Services;

public class PasswordHasherServiceTests
{
    [Fact]
    public void HashPassword_WhenNullOrWhitespace_Throws()
    {
        var sut = new PasswordHasherService();

        Action act1 = () => sut.HashPassword("");
        Action act2 = () => sut.HashPassword("   ");

        act1.Should().Throw<ArgumentException>().WithParameterName("password");
        act2.Should().Throw<ArgumentException>().WithParameterName("password");
    }

    [Fact]
    public void HashPassword_WhenValid_ReturnsDifferentValueThanPlainText()
    {
        var sut = new PasswordHasherService();

        var hash = sut.HashPassword("Password123!");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("Password123!");
    }

    [Fact]
    public void VerifyPassword_WhenValidPassword_ReturnsTrue()
    {
        var sut = new PasswordHasherService();

        var password = "Password123!";
        var hash = sut.HashPassword(password);

        sut.VerifyPassword(hash, password).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WhenWrongPassword_ReturnsFalse()
    {
        var sut = new PasswordHasherService();

        var hash = sut.HashPassword("Password123!");

        sut.VerifyPassword(hash, "WrongPassword!").Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WhenInputsInvalid_Throws()
    {
        var sut = new PasswordHasherService();
        var hash = sut.HashPassword("Password123!");

        Action act1 = () => sut.VerifyPassword("", "x");
        Action act2 = () => sut.VerifyPassword(hash, "");

        act1.Should().Throw<ArgumentException>().WithParameterName("hashedPassword");
        act2.Should().Throw<ArgumentException>().WithParameterName("providedPassword");
    }
}
