using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ModificationDetailsControllerTests;

public class ModificationDetails_ServiceErrorCases : TestServiceBase<ModificationDetailsController>
{
    [Fact]
    public async Task Returns_StatusCode_When_GetModificationsByIds_Fails()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var mods = Mocker.GetMock<IProjectModificationsService>();
        mods
            .Setup(s => s.GetModificationsByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Error = "fail"
            });

        // Act
        var result = await Sut.ModificationDetails("PR1", "12345", "short", Guid.NewGuid());

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Returns_BadRequest_When_No_Modification_Found()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var mods = Mocker.GetMock<IProjectModificationsService>();
        mods
            .Setup(s => s.GetModificationsByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new() { Modifications = [] }
            });

        // Act
        var result = await Sut.ModificationDetails("PR1", "12345", "short", Guid.NewGuid());

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }
}