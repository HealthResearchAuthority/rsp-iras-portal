using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class SubmitReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public async Task SubmitReviewBody_WithValidData_ShouldReturnSuccessView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK
        };

        model.Id = Guid.Empty;
        model.EmailAddress = "valid.email@example.com";

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(serviceResponse);

        Mocker.GetMock<IValidator<AddUpdateReviewBodyModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AddUpdateReviewBodyModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SuccessMessage");
        viewResult.Model.ShouldBeEquivalentTo(model);

        // Verify the service method was called once
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SubmitReviewBody_WithInvalidData_ShouldReturnSuccessView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        model.Id = Guid.Empty;

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(serviceResponse);

        Mocker.GetMock<IValidator<AddUpdateReviewBodyModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AddUpdateReviewBodyModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        // Verify the service method was called once
        Mocker
            .GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SubmitReviewBody_WithValidationError_ShouldReturnCreateReviewBodyView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        model.Id = Guid.Empty;

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(serviceResponse);

        Mocker.GetMock<IValidator<AddUpdateReviewBodyModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AddUpdateReviewBodyModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()
            {
                Errors =
                [
                    new ValidationFailure()
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
    }
}