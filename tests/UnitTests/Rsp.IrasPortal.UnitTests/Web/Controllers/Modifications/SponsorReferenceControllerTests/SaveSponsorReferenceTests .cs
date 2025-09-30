using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.ModificationChangesReviewControllerTests;

public class SaveSponsorReferenceTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task SaveSponsorReference_WithInvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var model = new SponsorReferenceViewModel();
        var validationFailures = new List<ValidationFailure>
    {
        new ValidationFailure("SponsorReference", "Sponsor reference is required.")
    };

        Mocker.GetMock<IValidator<SponsorReferenceViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SponsorReferenceViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await Sut.SaveSponsorReference(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SponsorReference");
        viewResult.Model.ShouldBe(model);
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ContainsKey("SponsorReference").ShouldBeTrue();
    }

    [Fact]
    public async Task SaveSponsorReference_WithValidModelAndSaveForLater_RedirectsToPostApproval()
    {
        // Arrange
        var model = new SponsorReferenceViewModel { ProjectRecordId = "PRJ-123" };

        Mocker.GetMock<IValidator<SponsorReferenceViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SponsorReferenceViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveSponsorReference(model, saveForLater: true);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:postapproval");
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues["ProjectRecordId"].ShouldBe("PRJ-123");
    }

    [Fact]
    public async Task SaveSponsorReference_WithValidModel_RedirectsToReviewAllChanges()
    {
        // Arrange
        var model = new SponsorReferenceViewModel();

        Mocker.GetMock<IValidator<SponsorReferenceViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SponsorReferenceViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveSponsorReference(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.ReviewAllChanges));
    }
}