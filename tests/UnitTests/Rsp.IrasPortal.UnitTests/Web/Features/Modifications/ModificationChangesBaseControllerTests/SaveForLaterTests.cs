using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ModificationChangesBaseControllerTests.ModificationChangesBaseControllerTests;

public class SaveForLaterTests
{
    private ModificationChangesBaseController CreateControllerWithTempData(out TempDataDictionary tempData)
    {
        tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        var controller = new ModificationChangesBaseController(
            Mock.Of<IRespondentService>(),
            Mock.Of<ICmsQuestionsetService>(),
            Mock.Of<IValidator<QuestionnaireViewModel>>()
        )
        {
            TempData = tempData
        };

        return controller;
    }

    [Fact]
    public void SaveForLater_SetsTempDataAndRedirectsToRoute()
    {
        // Arrange
        var projectRecordId = "PRJ-999";
        var routeName = "pmc:modificationdetails";
        var controller = CreateControllerWithTempData(out var tempData);

        // Act
        var result = controller.SaveForLater(projectRecordId, routeName);

        // Assert
        tempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldNotBeNull();
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBeOfType<Guid>();

        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe(routeName);
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues!["projectRecordId"].ShouldBe(projectRecordId);
    }
}