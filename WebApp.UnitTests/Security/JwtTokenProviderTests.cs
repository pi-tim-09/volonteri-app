using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;

namespace WebApp.UnitTests.Security;

public class JwtTokenProviderTests
{
    [Fact]
    public void CreateToken_ReturnsValidJwtContainingNameAndRoleClaims()
    {
        var secureKey = "super-secret-key-for-testing-purposes-only";

        var token = JwtTokenProvider.CreateToken(
            secureKey,
            expiration: 10,
            username: "user@example.com",
            role: "Admin");

        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "user@example.com");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");

        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }
}
