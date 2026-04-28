using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class SubmitReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public async Task SubmitReviewBody_WithValidCreateData_ShouldReturnSuccessView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.Id = Guid.Empty;
        model.EmailAddress = "valid.email@example.com";
        model.ResearchEthicsCommitteeId = "123";

        SetupValidValidation();
        SetupExistingRecIdResponse(0);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(CreateReviewBodyResponse(HttpStatusCode.OK));

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SuccessMessage");
        viewResult.Model.ShouldBeEquivalentTo(model);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.Is<ReviewBodyDto>(x =>
                x.IsActive == true)),
                Times.Once);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.UpdateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SubmitReviewBody_WithValidUpdateData_ShouldRedirectToViewReviewBody(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.Id = Guid.NewGuid();
        model.EmailAddress = "valid.email@example.com";
        model.ResearchEthicsCommitteeId = "123";

        SetupValidValidation();
        SetupExistingRecIdResponse(0);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.UpdateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(CreateReviewBodyResponse(HttpStatusCode.OK));

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("ViewReviewBody");
        redirectResult.RouteValues.ShouldNotBeNull();

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.UpdateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Once);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SubmitReviewBody_WithExistingRecId_ShouldRedirectToRecIdAlreadyAdded(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.Id = Guid.Empty;
        model.EmailAddress = "valid.email@example.com";
        model.ResearchEthicsCommitteeId = "123";
        model.ReviewBodyType = ReviewBodyType.ResearchEthicsCommittee;

        SetupValidValidation();
        SetupExistingRecIdResponse(1);

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("RecIdAlreadyAdded");
        redirectResult.RouteValues.ShouldNotBeNull();

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Never);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.UpdateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SubmitReviewBody_WithInvalidServiceResponse_ShouldReturnServiceError(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.Id = Guid.Empty;
        model.EmailAddress = "valid.email@example.com";
        model.ResearchEthicsCommitteeId = "123";

        SetupValidValidation();
        SetupExistingRecIdResponse(0);
        SetupTempData();

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(CreateReviewBodyResponse(HttpStatusCode.InternalServerError));

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SubmitReviewBody_WithValidationError_ShouldReturnCreateReviewBodyView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.Id = Guid.Empty;
        model.EmailAddress = "valid.email@example.com";

        Mocker.GetMock<IValidator<AddUpdateReviewBodyModel>>()
            .Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<AddUpdateReviewBodyModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors =
                [
                    new ValidationFailure
                    {
                        ErrorMessage = "error",
                        PropertyName = "name"
                    }
                ]
            });

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("CreateReviewBody");
        viewResult.Model.ShouldBeEquivalentTo(model);

        Sut.ModelState["name"]!.Errors.Single().ErrorMessage.ShouldBe("error");

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(
                It.IsAny<ReviewBodySearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
                Times.Never);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Never);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.UpdateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Never);
    }

    private void SetupValidValidation()
    {
        Mocker.GetMock<IValidator<AddUpdateReviewBodyModel>>()
            .Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<AddUpdateReviewBodyModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupExistingRecIdResponse(int totalCount)
    {
        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies(
                It.IsAny<ReviewBodySearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<AllReviewBodiesResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new AllReviewBodiesResponse
                {
                    TotalCount = totalCount
                }
            });
    }

    private static ServiceResponse<ReviewBodyDto> CreateReviewBodyResponse(HttpStatusCode statusCode)
    {
        return new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = statusCode,
            Content = new ReviewBodyDto()
        };
    }

    private void SetupTempData()
    {
        var httpContext = new DefaultHttpContext
        {
            Session = Mock.Of<ISession>()
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }
}