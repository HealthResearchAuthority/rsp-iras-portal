using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ReviewAllChangesControllerTests;

public class ReviewAllChanges_ServiceErrorCases : TestServiceBase<ReviewAllChangesController>
{
    [Fact]
    public async Task Returns_StatusCode_When_GetModificationsByIds_Fails()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new ServiceResponse<Rsp.IrasPortal.Application.DTOs.Responses.GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ReviewAllChanges("PR1", "IRAS", "Short", Guid.NewGuid());

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Returns_BadRequest_When_No_Modification_Found()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new ServiceResponse<Rsp.IrasPortal.Application.DTOs.Responses.GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new() { Modifications = [] }
            });

        // Act
        var result = await Sut.ReviewAllChanges("PR1", "IRAS", "Short", Guid.NewGuid());

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Returns_StatusCode_When_GetModificationChanges_Fails()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var modId = Guid.NewGuid().ToString();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new ServiceResponse<Rsp.IrasPortal.Application.DTOs.Responses.GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new() { Modifications = [new Rsp.IrasPortal.Application.DTOs.ModificationsDto { Id = modId, ModificationId = modId, Status = "Draft" }] }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationChanges(Guid.Parse(modId)))
            .ReturnsAsync(new ServiceResponse<IEnumerable<Rsp.IrasPortal.Application.DTOs.Responses.ProjectModificationChangeResponse>>
            {
                StatusCode = HttpStatusCode.BadGateway
            });

        // Act
        var result = await Sut.ReviewAllChanges("PR1", "IRAS", "Short", Guid.Parse(modId));

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public async Task Returns_StatusCode_When_GetInitialModificationQuestions_Fails()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var modId = Guid.NewGuid().ToString();

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new ServiceResponse<Rsp.IrasPortal.Application.DTOs.Responses.GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new() { Modifications = [new Rsp.IrasPortal.Application.DTOs.ModificationsDto { Id = modId, ModificationId = modId, Status = "Draft" }] }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationChanges(Guid.Parse(modId)))
            .ReturnsAsync(new ServiceResponse<IEnumerable<Rsp.IrasPortal.Application.DTOs.Responses.ProjectModificationChangeResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications.StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await Sut.ReviewAllChanges("PR1", "IRAS", "Short", Guid.Parse(modId));

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }
}