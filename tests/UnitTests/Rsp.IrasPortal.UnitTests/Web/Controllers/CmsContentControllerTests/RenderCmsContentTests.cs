using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers.CmsContent;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.CmsContentControllerTests;

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
        result.ShouldBeOfType<ViewResult>();

        // Verify
        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetPageContentByUrl(It.IsAny<string>(), false), Times.Once);
    }
}