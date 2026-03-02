using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.UnitTests;
using Rsp.Portal.Web.Features.Modifications;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ModificationDetailsControllerTests;

public class MakeRequestRevisionByApplicantTests : TestServiceBase<ModificationDetailsController>
{
    [Fact]
    public async Task RequestForRevision_WhenPrepareReturnsActionResult_ReturnsThatResult()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        ctx.Session = new InMemorySession(); // ensure Session is available to controller
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR1"
        };
        var modId = Guid.NewGuid();
        Mocker
          .GetMock<IProjectModificationsService>()
          .Setup(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
          .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
          {
              StatusCode = HttpStatusCode.OK,
              Content = new ProjectModificationResponse
              {
                  Id = modId,
                  ModificationIdentifier = modId.ToString(),
                  Status = ModificationStatus.RequestRevisions,
                  ProjectRecordId = "PR1",
                  ModificationNumber = 1,
                  CreatedDate = DateTime.UtcNow,
                  UpdatedDate = DateTime.UtcNow,
                  CreatedBy = "TestUser",
                  UpdatedBy = "TestUser",
                  ModificationType = "Substantial",
                  Category = "Category A",
                  ReviewType = "Full Review"
              }
          });

        // Act
        var result = await Sut.RequestForRevision(
                projectRecordId: "PRJ-1",
                projectModificationId: Guid.NewGuid());

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ViewResult>(result);
        Assert.Equal("MakeRevisonByApplicant", view.ViewName);
    }

    [Fact]
    public async Task RequestForRevision_WhenModificationIsNull_ReturnsBadRequestServiceError()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        ctx.Session = new InMemorySession(); // ensure Session is available to controller
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR1"
        };
        var modId = Guid.NewGuid();
        Mocker
          .GetMock<IProjectModificationsService>()
          .Setup(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
          .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
          {
              StatusCode = HttpStatusCode.BadRequest,
              Content = null
          });

        // Act
        var result = await Sut.RequestForRevision(
                projectRecordId: "PRJ-1",
                projectModificationId: Guid.NewGuid());

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        Mocker.GetMock<IProjectModificationsService>().Verify(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
    }
}