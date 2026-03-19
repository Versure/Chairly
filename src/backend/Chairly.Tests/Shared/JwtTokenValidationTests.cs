using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Chairly.Tests.Tenancy;

public class JwtTokenValidationTests
{
    private static readonly SigningCredentials SigningCredentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-signing-key-for-tests-12345")),
        SecurityAlgorithms.HmacSha256);

    [Fact]
    public void ValidateToken_WithClientAudience_SucceedsForConfiguredClientId()
    {
        var token = CreateToken("chairly-frontend");

        var tokenValidationParameters = CreateValidationParameters(validAudiences: ["account", "chairly-frontend"]);
        var handler = new JwtSecurityTokenHandler();

        var principal = handler.ValidateToken(token, tokenValidationParameters, out _);

        Assert.NotNull(principal);
    }

    [Fact]
    public void ValidateToken_WithClientAudience_FailsWhenOnlyAccountAudienceIsAllowed()
    {
        var token = CreateToken("chairly-frontend");

        var tokenValidationParameters = CreateValidationParameters(validAudiences: ["account"]);
        var handler = new JwtSecurityTokenHandler();

        Assert.Throws<SecurityTokenInvalidAudienceException>(
            () => handler.ValidateToken(token, tokenValidationParameters, out _));
    }

    private static string CreateToken(string audience)
    {
        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Issuer = "http://localhost:8080/realms/chairly",
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("iss", "http://localhost:8080/realms/chairly"),
            ]),
            Expires = now.AddMinutes(10),
            NotBefore = now.AddMinutes(-1),
            SigningCredentials = SigningCredentials,
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.CreateEncodedJwt(tokenDescriptor);
    }

    private static TokenValidationParameters CreateValidationParameters(string[] validAudiences)
    {
        return new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudiences = validAudiences,
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:8080/realms/chairly",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SigningCredentials.Key,
            ValidateLifetime = true,
        };
    }
}
