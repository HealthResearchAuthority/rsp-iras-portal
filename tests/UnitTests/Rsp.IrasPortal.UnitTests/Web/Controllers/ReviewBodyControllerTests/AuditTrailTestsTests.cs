using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class AuditTrailTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public async Task AuditTrailBodies_ShouldReturnCorrectResult(ReviewBodyAuditTrailResponse auditTrail, Guid reviewBodyId)
    {
        // Arrange
        var skip = 0;
        var take = 3;
        var page = 1;
        var serviceResponse = new ServiceResponse<ReviewBodyAuditTrailResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = auditTrail
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.ReviewBodyAuditTrail(reviewBodyId, skip, take))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.AuditTrail(reviewBodyId, page, take);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ReviewBodyAuditTrailViewModel>();
        model!.Items.ShouldBeEquivalentTo(auditTrail.Items);

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.ReviewBodyAuditTrail(reviewBodyId, skip, take), Times.Once);
    }
}