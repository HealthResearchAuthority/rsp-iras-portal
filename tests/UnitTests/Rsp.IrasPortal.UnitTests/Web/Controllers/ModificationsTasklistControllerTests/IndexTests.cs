using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class IndexTests : TestServiceBase<ModificationsTasklistController>
{
    private const string TempDataKey = "td:ApprovalsSearchModel";

    public IndexTests()
    {
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        Sut.TempData = tempData;
    }

    [Fact]
    public async Task Welcome_ReturnsViewResult_WithIndexViewName()
    {
        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Theory, AutoData]
    public async Task Index_ViewModel_Test(GetModificationsResponse modificationResponse)
    {
        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ModificationsTasklistViewModel>();
        var modifications = model?.Modifications.ShouldBeOfType<List<TaskListModificationViewModel>>();
    }
}