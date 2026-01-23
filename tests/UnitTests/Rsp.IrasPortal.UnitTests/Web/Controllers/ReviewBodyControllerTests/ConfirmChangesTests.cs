using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ConfirmChangesTests : TestServiceBase<ReviewBodyController>

{
    [Theory]
    [AutoData]
    public async Task ConfirmChanges_WithValidModel_ShouldReturnConfirmView(AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.EmailAddress = "valid.email@example.com";

        Mocker.GetMock<IValidator<AddUpdateReviewBodyModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AddUpdateReviewBodyModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.ConfirmChanges(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ConfirmChanges");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmChanges_WithInvalidModel_ShouldReturnCreateEditView(AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.EmailAddress = "valid.email@example.com";
        Sut.ModelState.AddModelError("ErrorKey", "Some error message");

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
        var result = await Sut.ConfirmChanges(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("CreateReviewBody");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }
}