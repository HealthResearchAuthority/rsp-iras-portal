using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs.Responses.CmsContent;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers.CmsContent;

namespace Rsp.Portal.UnitTests.Web.Controllers.CmsContentControllerTests;

public class RenderCmsContentTests : TestServiceBase<CmsContentController>
{
    private readonly DefaultHttpContext _http;

    public RenderCmsContentTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task Return_NotFound_When_Content_Is_Not_Found()
    {
        // Arrange
        var serviceResponse = new ServiceResponse<GenericPageResponse>
        {
            StatusCode = HttpStatusCode.NotFound,
        };

        Mocker.GetMock<ICmsContentService>()
            .Setup(s => s.GetPageContentByUrl(It.IsAny<string>(), false))
            .ReturnsAsync(serviceResponse);

        var nonExistingUrl = "/page-does-not-exist/";
        _http.Request.Path = PathString.FromUriComponent(nonExistingUrl);

        // Act
        var result = await Sut.Index();

        // Assert
        result.ShouldBeOfType<NotFoundResult>();

        // Verify
        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetPageContentByUrl(It.IsAny<string>(), false), Times.Once);
    }

    [Fact]
    public async Task Return_NotFound_Path_Is_Null()
    {
        // Arrange
        var serviceResponse = new ServiceResponse<GenericPageResponse>
        {
            StatusCode = HttpStatusCode.NotFound,
        };

        Mocker.GetMock<ICmsContentService>()
            .Setup(s => s.GetPageContentByUrl(It.IsAny<string>(), false))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.Index();

        // Assert
        result.ShouldBeOfType<NotFoundResult>();

        // Verify
        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetPageContentByUrl(It.IsAny<string>(), false), Times.Never);
    }

    [Theory, AutoData]
    public async Task Return_View_When_Content_Is_Found(GenericPageResponse pageContent)
    {
        var metaTitle = "test title";
        var pageTitle = "page title";

        pageContent.Properties.MetaTitle = metaTitle;
        pageContent.Name = pageTitle;
        // Arrange
        var serviceResponse = new ServiceResponse<GenericPageResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = pageContent
        };

        Mocker.GetMock<ICmsContentService>()
            .Setup(s => s.GetPageContentByUrl(It.IsAny<string>(), false))
            .ReturnsAsync(serviceResponse);

        var nonExistingUrl = "/page-does-exist/";
        _http.Request.Path = PathString.FromUriComponent(nonExistingUrl);

        // Act
        var result = await Sut.Index();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();

        // Verify
        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetPageContentByUrl(It.IsAny<string>(), false), Times.Once);

        viewResult.ViewData["Title"].ShouldBe(metaTitle);
    }

    [Theory, AutoData]
    public async Task MetaTitle_RevertsToPageTitle_When_Empty(GenericPageResponse pageContent)
    {
        var pageTitle = "page title";

        pageContent.Properties = null;
        pageContent.Name = pageTitle;
        // Arrange
        var serviceResponse = new ServiceResponse<GenericPageResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = pageContent
        };

        Mocker.GetMock<ICmsContentService>()
            .Setup(s => s.GetPageContentByUrl(It.IsAny<string>(), false))
            .ReturnsAsync(serviceResponse);

        var nonExistingUrl = "/page-does-exist/";
        _http.Request.Path = PathString.FromUriComponent(nonExistingUrl);

        // Act
        var result = await Sut.Index();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();

        // Verify
        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetPageContentByUrl(It.IsAny<string>(), false), Times.Once);

        viewResult.ViewData["Title"].ShouldBe(pageTitle);
    }

    [Theory, AutoData]
    public async Task MetaTitle_RevertsToPageTitle_When_EmptyString(GenericPageResponse pageContent)
    {
        var pageTitle = "page title";
        var metaTitle = "";

        pageContent.Properties.MetaTitle = metaTitle;
        pageContent.Name = pageTitle;
        // Arrange
        var serviceResponse = new ServiceResponse<GenericPageResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = pageContent
        };

        Mocker.GetMock<ICmsContentService>()
            .Setup(s => s.GetPageContentByUrl(It.IsAny<string>(), false))
            .ReturnsAsync(serviceResponse);

        var nonExistingUrl = "/page-does-exist/";
        _http.Request.Path = PathString.FromUriComponent(nonExistingUrl);

        // Act
        var result = await Sut.Index();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();

        // Verify
        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetPageContentByUrl(It.IsAny<string>(), false), Times.Once);

        viewResult.ViewData["Title"].ShouldBe(pageTitle);
    }

    [Theory, AutoData]
    public async Task Return_Preview_Content_When_Flagged(GenericPageResponse pageContent)
    {
        var pageTitle = "page title";
        pageContent.Properties.MetaTitle = null;
        pageContent.Name = pageTitle;

        // Arrange
        var serviceResponse = new ServiceResponse<GenericPageResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = pageContent
        };

        Mocker.GetMock<ICmsContentService>()
            .Setup(s => s.GetPageContentByUrl(It.IsAny<string>(), true))
            .ReturnsAsync(serviceResponse);

        var existingUrl = "/page-exists";
        _http.Request.Path = PathString.FromUriComponent(existingUrl);
        _http.Request.QueryString = new QueryString("?preview=true");

        // Act
        var result = await Sut.Index();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();

        // Verify
        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetPageContentByUrl(It.IsAny<string>(), true), Times.Once);

        viewResult.ViewData["Title"].ShouldBe(pageTitle);
    }
}