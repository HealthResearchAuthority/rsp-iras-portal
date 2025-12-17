using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications;

public class AuditTrailTests : TestServiceBase<ModificationsController>
{
    [Theory, AutoData]
    public async Task AuditTrail_ShouldReturnAuditTrailModel_When_OK_Response
    (
        ProjectModificationAuditTrailResponse auditTrailResponse,
        string modificationIdentifier,
        string shortTitle,
        Guid modificationId,
        Guid projectRecordId
    )
    {
        var httpContext = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var tempDataProvider = new Mock<ITempDataProvider>();
        Sut.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
        Sut.TempData[TempDataKeys.ProjectRecordId] = projectRecordId;

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationAuditTrail(modificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = auditTrailResponse
            });

        var auditTrailModel = new AuditTrailModel
        {
            AuditTrail = auditTrailResponse,
            ModificationIdentifier = modificationIdentifier,
            ShortTitle = shortTitle,
            ProjectRecordId = projectRecordId.ToString()
        };

        // Act
        var result = await Sut.AuditTrail(modificationId, shortTitle, modificationIdentifier);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AuditTrailModel>();
        viewResult.Model.ShouldBeEquivalentTo(auditTrailModel);
    }
}