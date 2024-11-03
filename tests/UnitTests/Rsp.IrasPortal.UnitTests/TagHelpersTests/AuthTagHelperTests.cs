using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using Rsp.IrasPortal.Web.TagHelpers;
using Rsp.IrasPortal.Web.TagHelpers.Models;
using Shouldly;

namespace Rsp.IrasPortal.UnitTests.TagHelpersTests;

public class AuthTagHelperTests : TestServiceBase
{
    [Fact]
    public async Task ProcessAsync_Should_ShowOutput_When_ShowWhenAuthenticated_And_UserIsAuthenticated_And_UserInRoles()
    {
        // Arrange
        var authParams = new AuthTagHelperParams
        {
            ShowWhenAuthenticated = true,
            Roles = "Admin,User",
            RolesLogic = RolesProcessing.Or
        };

        var claims = new List<Claim>()
        {
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "Reviewer")
        };

        var identityMock = new Mock<ClaimsIdentity>();
        identityMock
            .Setup(i => i.IsAuthenticated)
            .Returns(true);

        identityMock
            .Setup(i => i.Claims)
            .Returns(claims);

        var userMock = new Mock<ClaimsPrincipal>();
        userMock
            .Setup(u => u.Identity)
            .Returns(identityMock.Object);

        userMock
            .Setup(u => u.Claims)
            .Returns(claims);

        userMock
            .Setup(u => u.FindAll(It.IsAny<Predicate<Claim>>()))
            .Returns(claims);

        var httpContextMock = Mocker.GetMock<IHttpContextAccessor>();
        httpContextMock
            .Setup(h => h.HttpContext)
            .Returns(new DefaultHttpContext { User = userMock.Object });

        var tagHelper = new AuthTagHelper(httpContextMock.Object)
        {
            AuthParams = authParams
        };

        var context = new TagHelperContext
        (
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "test",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("test content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Should_SuppressOutput_When_ShowWhenAuthenticated_And_UserIsAuthenticated_But_NotInRoles()

    {
        // Arrange
        var authParams = new AuthTagHelperParams
        {
            ShowWhenAuthenticated = true,
            Roles = "Admin",
            RolesLogic = RolesProcessing.And
        };

        var claims = new List<Claim>();

        var identityMock = new Mock<ClaimsIdentity>();
        identityMock
            .Setup(i => i.IsAuthenticated)
            .Returns(true);

        identityMock
            .Setup(i => i.Claims)
            .Returns(claims);

        var userMock = new Mock<ClaimsPrincipal>();
        userMock
            .Setup(u => u.Identity)
            .Returns(identityMock.Object);

        userMock
            .Setup(u => u.Claims)
            .Returns(claims);

        var httpContextMock = Mocker.GetMock<IHttpContextAccessor>();
        httpContextMock
            .Setup(h => h.HttpContext)
            .Returns(new DefaultHttpContext { User = userMock.Object });

        var tagHelper = new AuthTagHelper(httpContextMock.Object)
        {
            AuthParams = authParams
        };

        var context = new TagHelperContext
        (
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "test",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("test content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Should_ShowOutput_When_ShowWhenAuthenticated_And_UserIsAuthenticated_And_RolesNotProvided()
    {
        // Arrange
        var authParams = new AuthTagHelperParams
        {
            ShowWhenAuthenticated = true,
            Roles = null,
            RolesLogic = RolesProcessing.Or
        };

        var claims = new List<Claim>();

        var identityMock = new Mock<ClaimsIdentity>();
        identityMock
            .Setup(i => i.IsAuthenticated)
            .Returns(true);

        identityMock
            .Setup(i => i.Claims)
            .Returns(claims);

        var userMock = new Mock<ClaimsPrincipal>();
        userMock
            .Setup(u => u.Identity)
            .Returns(identityMock.Object);

        userMock
            .Setup(u => u.Claims)
            .Returns(claims);

        var httpContextMock = Mocker.GetMock<IHttpContextAccessor>();
        httpContextMock
            .Setup(h => h.HttpContext)
            .Returns(new DefaultHttpContext { User = userMock.Object });

        var tagHelper = new AuthTagHelper(httpContextMock.Object)
        {
            AuthParams = authParams
        };

        var context = new TagHelperContext
        (
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "test",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("test content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Should_SuppressOutput_When_ShowWhenAuthenticated_And_UserIsNotAuthenticated()
    {
        // Arrange
        var authParams = new AuthTagHelperParams
        {
            ShowWhenAuthenticated = true
        };

        var identityMock = new Mock<ClaimsIdentity>();
        identityMock
            .Setup(i => i.IsAuthenticated)
            .Returns(false);

        var userMock = new Mock<ClaimsPrincipal>();
        userMock
            .Setup(u => u.Identity)
            .Returns(identityMock.Object);

        var httpContextMock = Mocker.GetMock<IHttpContextAccessor>();
        httpContextMock
            .Setup(h => h.HttpContext)
            .Returns(new DefaultHttpContext { User = userMock.Object });

        var tagHelper = new AuthTagHelper(httpContextMock.Object)
        {
            AuthParams = authParams
        };

        var context = new TagHelperContext
        (
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "test",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("test content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Should_ShowOutput_When_NotShowWhenAuthenticated_And_UserIsNotAuthenticated()
    {
        // Arrange
        var authParams = new AuthTagHelperParams
        {
            ShowWhenAuthenticated = false
        };

        var identityMock = new Mock<ClaimsIdentity>();
        identityMock
            .Setup(i => i.IsAuthenticated)
            .Returns(false);

        var userMock = new Mock<ClaimsPrincipal>();
        userMock
            .Setup(u => u.Identity)
            .Returns(identityMock.Object);

        var httpContextMock = Mocker.GetMock<IHttpContextAccessor>();
        httpContextMock
            .Setup(h => h.HttpContext)
            .Returns(new DefaultHttpContext { User = userMock.Object });

        var tagHelper = new AuthTagHelper(httpContextMock.Object)
        {
            AuthParams = authParams
        };

        var context = new TagHelperContext
        (
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "test",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("test content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Should_SuppressOutput_When_NotShowWhenAuthenticated_And_UserIsAuthenticated()
    {
        // Arrange
        var authParams = new AuthTagHelperParams
        {
            ShowWhenAuthenticated = false
        };

        var identityMock = new Mock<ClaimsIdentity>();
        identityMock
            .Setup(i => i.IsAuthenticated)
            .Returns(true);

        var userMock = new Mock<ClaimsPrincipal>();
        userMock
            .Setup(u => u.Identity)
            .Returns(identityMock.Object);

        var httpContextMock = Mocker.GetMock<IHttpContextAccessor>();
        httpContextMock
            .Setup(h => h.HttpContext)
            .Returns(new DefaultHttpContext { User = userMock.Object });

        var tagHelper = new AuthTagHelper(httpContextMock.Object)
        {
            AuthParams = authParams
        };

        var context = new TagHelperContext
        (
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "test",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        output.Content.SetHtmlContent("test content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }
}