using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ModificationDetailsControllerTests;

public class RemoveChange_Action : TestServiceBase<ModificationDetailsController>
{
    [Fact]
    public async Task Redirects_To_ModificationDetails_On_Success()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ShortProjectTitle] = "Short"
        };

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.RemoveModificationChange(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.RemoveChange(Guid.NewGuid(), "name");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(ModificationDetailsController.ModificationDetails));
    }
}