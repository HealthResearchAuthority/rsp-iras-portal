using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoFixture.Xunit2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NetDevPack.Security.Jwt.Core.Interfaces;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Infrastructure.Claims;
using Shouldly;

namespace Rsp.IrasPortal.UnitTests.Infrastructure.CustomClaimsTransformationTests;

public class UpdateAccessTokenTests : TestServiceBase<CustomClaimsTransformation>
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IJwtService> _jwtService;
    private readonly Mock<IOptionsSnapshot<AppSettings>> _appSettings;

    public UpdateAccessTokenTests()
    {
        _httpContextAccessor = Mocker.GetMock<IHttpContextAccessor>();
        _jwtService = Mocker.GetMock<IJwtService>();
        _appSettings = Mocker.GetMock<IOptionsSnapshot<AppSettings>>();

        // Mock AppSettings
        _appSettings
            .Setup(x => x.Value)
            .Returns(new AppSettings
            {
                AuthSettings = new AuthSettings { ClientId = "test-client-id" }
            });
    }

    [Theory, AutoData]
    public async Task UpdateAccessToken_Should_Update_AccessToken_In_HttpContext(string roleClaim)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, roleClaim)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Mock JwtService to return signing credentials
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(new byte[32]), SecurityAlgorithms.HmacSha256);
        _jwtService
            .Setup(x => x.GetCurrentSigningCredentials())
            .ReturnsAsync(signingCredentials);

        var token = GenerateToken(principal, signingCredentials);

        // Mock HttpContext
        var httpContext = new DefaultHttpContext();
        httpContext.Items[ContextItemKeys.AcessToken] = token;
        _httpContextAccessor
            .SetupGet(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        await Sut.UpdateAccessToken(principal);

        // Assert
        httpContext.Items[ContextItemKeys.AcessToken]
            .ShouldNotBeNull()
            .ShouldNotBe(token);

        // Verify
        var handler = new JwtSecurityTokenHandler();

        // get the updated access token
        var jsonToken = handler.ReadJwtToken(httpContext.Items[ContextItemKeys.AcessToken] as string);

        // get the claims from the updated access token
        var updatedClaims = jsonToken.Claims.ToList();
        updatedClaims.ShouldContain(c => c.Value == roleClaim);
    }

    // To generate token
    private static string GenerateToken(ClaimsPrincipal principal, SigningCredentials credentials)
    {
        var handler = new JwtSecurityTokenHandler();

        // configure the new token using the existing
        // access_token properties but with newly added
        // claims.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = "http://localhost", // Add this line
            Audience = "http://localhost",
            IssuedAt = DateTime.Now,
            Subject = (ClaimsIdentity)principal.Identity!,
            Expires = DateTime.Now.AddMinutes(15),
            SigningCredentials = credentials
        };

        // generate the security token
        var token = handler.CreateJwtSecurityToken(tokenDescriptor);

        return handler.WriteToken(token);
    }
}