using Microsoft.AspNetCore.Mvc;
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
        Guid modificationId
    )
    {
        // Arrange
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationAuditTrail(modificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationAuditTrailResponse>()
            {
                StatusCode = HttpStatusCode.OK,
                Content = auditTrailResponse
            });

        var auditTrailModel = new AuditTrailModel
        {
            AuditTrail = auditTrailResponse,
            ModificationIdentifier = modificationIdentifier,
            ShortTitle = shortTitle
        };

        // Act
        var result = await Sut.AuditTrail(modificationId, shortTitle, modificationIdentifier);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AuditTrailModel>();
        viewResult.Model.ShouldBeEquivalentTo(auditTrailModel);
    }
}