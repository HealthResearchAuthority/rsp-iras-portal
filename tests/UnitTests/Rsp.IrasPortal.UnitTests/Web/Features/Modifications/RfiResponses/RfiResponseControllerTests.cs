using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Controllers;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.UnitTests;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.RfiResponses;

public class RfiResponseControllerTests : TestServiceBase<RfiResponseController>
{
    [Theory, AutoData]
    public async Task RfiDetails_Returns_View_When_No_Errors(
        string projectId,
        Guid modificationId,
        ProjectModificationResponse modificationResponse,
        IrasApplicationResponse projectRecordResponse,
        ProjectModificationReviewResponse rfiResponse,
        ModificationRfiResponseResponse rfiResponseResponse)
    {
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>());

        modificationResponse.Status = ModificationStatus.RequestForInformation;

        var modResponse = new ServiceResponse<ProjectModificationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification(projectId, modificationId))
            .ReturnsAsync(modResponse);

        var projectResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = projectRecordResponse
        };

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectId))
            .ReturnsAsync(projectResponse);

        var modRfiResponse = new ServiceResponse<ProjectModificationReviewResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponse
        };

        var modRfiResponsesResponse = new ServiceResponse<ModificationRfiResponseResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponseResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponse);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationRfiResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponsesResponse);

        var result = await Sut.RfiDetails(projectId, modificationId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<RfiDetailsViewModel>();
        model.IrasId.ShouldBe(projectRecordResponse.IrasId.ToString());
        model.ModificationId.ShouldBe(modificationResponse.ModificationIdentifier);
        model.RfiReasons.Count.ShouldBe(rfiResponse.RequestForInformationReasons.Count);
    }

    [Theory, AutoData]
    public async Task RfiDetails_Returns_Forbidden_When_Modification_Not_In_RFI(
       string projectId,
       Guid modificationId,
       ProjectModificationResponse modificationResponse,
       IrasApplicationResponse projectRecordResponse,
       ProjectModificationReviewResponse rfiResponse,
       ModificationRfiResponseResponse rfiResponsesResponse)
    {
        modificationResponse.Status = ModificationStatus.Received;

        var modResponse = new ServiceResponse<ProjectModificationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification(projectId, modificationId))
            .ReturnsAsync(modResponse);

        var projectResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = projectRecordResponse
        };

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectId))
            .ReturnsAsync(projectResponse);

        var modRfiResponse = new ServiceResponse<ProjectModificationReviewResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponse
        };

        var modRfiResponsesResponse = new ServiceResponse<ModificationRfiResponseResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rfiResponsesResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponse);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationRfiResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponsesResponse);

        var result = await Sut.RfiDetails(projectId, modificationId);

        var viewResult = result.ShouldBeOfType<ForbidResult>();
    }

    [Theory, AutoData]
    public async Task RfiResponses_GET_Returns_View_With_Model
    (
        RfiDetailsViewModel tempDataModel,
        Guid modificationId
    )
    {
        tempDataModel.ModificationId = modificationId.ToString();
        SetupTempData(tempDataModel);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var result = await Sut.RfiResponses(tempDataModel, false);

        var view = result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Theory, AutoData]
    public async Task RfiResponses_POST_Returns_View_With_Error_When_Reason_Missing
    (
        RfiDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiReasons = ["Reason 1", "Reason 2"];
        model.RfiResponses = ["Response 1", ""];

        SetupTempData(model);

        var result = await Sut.RfiResponses(model);

        var view = result.ShouldBeOfType<ViewResult>();
        view.Model.ShouldBeAssignableTo<RfiDetailsViewModel>();
        Sut.ModelState.IsValid.ShouldBeFalse();
    }

    [Theory, AutoData]
    public async Task RfiResponses_POST_Returns_Service_Error_When_Service_Fails
    (
        RfiDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiReasons = ["Reason 1"];
        model.RfiResponses = ["Response 1"];

        SetupTempData(model);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.InternalServerError });

        var result = await Sut.RfiResponses(model);
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(500);
    }

    [Theory, AutoData]
    public async Task RfiResponses_POST_Saves_And_Redirects_When_SaveForLater_Is_True
    (
        RfiDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiReasons = ["Reason 1"];
        model.RfiResponses = ["Response 1"];

        SetupTempData(model);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var result = await Sut.RfiResponses(model, saveForLater: true);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("ReviewAllChanges");
    }

    [Theory, AutoData]
    public async Task RfiResponses_POST_Saves_And_Continues_When_SaveForLater_Is_False
    (
        RfiDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiReasons = ["Reason 1"];
        model.RfiResponses = ["Response 1"];
        SetupTempData(model);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.SaveModificationRfiResponses(It.IsAny<ModificationRfiResponseRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var result = await Sut.RfiResponses(model, saveForLater: false);
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();

        redirectResult.ActionName.ShouldBe("RfiCheckAndSubmitResponses");
    }

    [Theory, AutoData]
    public void RfiCheckAndSubmitResponses_GET_Returns_View_With_Model
    (
        RfiDetailsViewModel tempDataModel,
        Guid modificationId
    )
    {
        tempDataModel.ModificationId = modificationId.ToString();

        SetupTempData(tempDataModel);

        var result = Sut.RfiCheckAndSubmitResponses();

        var view = result.ShouldBeOfType<ViewResult>();
        view.Model.ShouldBeAssignableTo<RfiDetailsViewModel>();
    }

    [Theory, AutoData]
    public async Task RfiCheckAndSubmitResponses_POST_Returns_Service_Error_When_Service_Fails
    (
        RfiDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiReasons = ["Reason 1"];
        model.RfiResponses = ["Response 1"];

        SetupTempData(model);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), null, null, null))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.InternalServerError });

        var result = await Sut.RfiSubmitResponses();
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(500);
    }

    [Theory, AutoData]
    public async Task RfiCheckAndSubmitResponses_POST_Submits_And_Redirects_When_Service_Succeeds
    (
        RfiDetailsViewModel model,
        Guid modificationId
    )
    {
        model.ModificationId = modificationId.ToString();
        model.RfiReasons = ["Reason 1"];
        model.RfiResponses = ["Response 1"];

        SetupTempData(model);
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), null, null, null))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var result = await Sut.RfiSubmitResponses();
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();

        redirectResult.ActionName.ShouldBe("RfiResponsesConfirmation");
    }

    private void SetupTempData(RfiDetailsViewModel model)
    {
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.RfiDetails] = JsonSerializer.Serialize(model)
        };
    }
}