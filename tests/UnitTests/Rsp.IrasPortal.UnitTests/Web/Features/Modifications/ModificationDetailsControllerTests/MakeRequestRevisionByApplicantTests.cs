using System.Security.Claims;
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
        var httpContext = new DefaultHttpContext { Session = new InMemorySession() };
        httpContext.User = new ClaimsPrincipal(
        new ClaimsIdentity());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.IrasId] = "123456",
            [TempDataKeys.ShortProjectTitle] = "Test Project",
        };
        Sut.TempData = tempData;
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
                irasId: "12345",
                shortTitle: "test",
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
        var httpContext = new DefaultHttpContext { Session = new InMemorySession() };
        httpContext.User = new ClaimsPrincipal(
        new ClaimsIdentity());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.IrasId] = "123456",
            [TempDataKeys.ShortProjectTitle] = "Test Project",
        };
        Sut.TempData = tempData;
        Mocker
          .GetMock<IProjectModificationsService>()
          .Setup(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
          .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
          {
              StatusCode = HttpStatusCode.BadRequest,
              Content = null
          });

        // Act
        // Act
        var result = await Sut.RequestForRevision(
                projectRecordId: "PRJ-1",
                irasId: "12345",
                shortTitle: "test",
                projectModificationId: Guid.NewGuid());

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        Mocker.GetMock<IProjectModificationsService>().Verify(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task RequestForRevision_WhenContentIsNull_ReturnsBadRequestServiceError()
    {
        // Arrange
        var projectRecordId = "PR123";
        var irasId = "IRAS456";
        var shortTitle = "Sample Short Title";
        var modificationId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext { Session = new InMemorySession() };
        httpContext.User = new ClaimsPrincipal(
        new ClaimsIdentity());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.IrasId] = "123456",
            [TempDataKeys.ShortProjectTitle] = "Test Project",
        };
        Sut.TempData = tempData;

        // Mock service to return success but with Content = null
        Mocker
          .GetMock<IProjectModificationsService>()
          .Setup(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
          .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
          {
              StatusCode = HttpStatusCode.OK,
              Content = null
          });

        // Act
        var result = await Sut.RequestForRevision(
            projectRecordId,
            irasId,
            shortTitle,
            modificationId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }
}