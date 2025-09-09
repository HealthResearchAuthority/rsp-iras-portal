using Microsoft.AspNetCore.Mvc.ViewComponents;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.ViewComponents;

namespace Rsp.IrasPortal.UnitTests.Web.ViewComponents.LoginLandingViewComponentTests;

public class RenderLoginLandingContentTests : TestServiceBase<LoginLandingPageViewComponent>
{
    [Fact]
    public async Task Return_View_When_No_Content_Is_Found()
    {
        // Arrange
        var serviceResponse = new ServiceResponse<GenericPageResponse>
        {
            StatusCode = HttpStatusCode.NotFound,
        };

        Mocker.GetMock<ICmsContentService>()
            .Setup(s => s.GetHomeContent())
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.InvokeAsync();

        // Assert
        var componentResult = result.ShouldBeOfType<ViewViewComponentResult>();

        // Verify
        var model = componentResult?.ViewData?.Model.ShouldBeOfType<GenericPageResponse>();
        model?.Properties?.ShouldBeNull();

        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetHomeContent(), Times.Once);
    }

    [Theory, AutoData]
    public async Task Return_View_When_Content_Is_Found(GenericPageResponse pageResponse)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<GenericPageResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = pageResponse
        };

        Mocker.GetMock<ICmsContentService>()
            .Setup(s => s.GetHomeContent())
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.InvokeAsync();

        // Assert
        var componentResult = result.ShouldBeOfType<ViewViewComponentResult>();

        // Verify
        var viewModel = componentResult?.ViewData?.Model.ShouldBeOfType<GenericPageResponse>();
        viewModel?.Properties.LoginLandingPageAboveTheFold.ShouldNotBeNull();

        Mocker.GetMock<ICmsContentService>()
            .Verify(s => s.GetHomeContent(), Times.Once);
    }
}