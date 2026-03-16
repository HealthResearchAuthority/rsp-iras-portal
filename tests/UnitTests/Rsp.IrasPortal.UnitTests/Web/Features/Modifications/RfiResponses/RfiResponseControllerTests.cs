using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Controllers;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;
using Rsp.Portal.Application.Constants;
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
        ProjectModificationReviewResponse rfiResponse)
    {
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

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponse);

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
       ProjectModificationReviewResponse rfiResponse)
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

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(projectId, modificationId))
            .ReturnsAsync(modRfiResponse);

        var result = await Sut.RfiDetails(projectId, modificationId);

        var viewResult = result.ShouldBeOfType<ForbidResult>();
    }
}