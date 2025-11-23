using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ReviewAllChangesControllerTests;

public class ReviewOutcome_Journey : TestServiceBase<ReviewAllChangesController>
{
    private readonly Mock<IProjectModificationsService> _modificationService;

    public ReviewOutcome_Journey()
    {
        _modificationService = Mocker.GetMock<IProjectModificationsService>();
    }

    [Fact]
    public async Task ReviewOutcome_Get_ReturnsError_When_NoTempData()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.ReviewOutcome();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);
    }

    [Theory, AutoData]
    public async Task ReviewOutcome_Get_ReturnsView_WithModel_When_TempData_Exists
    (
        Guid modificationId,
        ReviewOutcomeViewModel tempDataModel,
        ProjectModificationReviewResponse modificationReviewResponse
    )
    {
        // Arrange
        tempDataModel.ModificationDetails.ModificationId = modificationId.ToString();
        SetupTempData(tempDataModel);

        _modificationService
            .Setup(s => s.GetModificationReviewResponses(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = modificationReviewResponse
            });

        // Act
        var result = await Sut.ReviewOutcome();

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var returned = view.Model.ShouldBeOfType<ReviewOutcomeViewModel>();
        returned.ReviewOutcome.ShouldBe(modificationReviewResponse.ReviewOutcome);
        returned.Comment.ShouldBe(modificationReviewResponse.Comment);
    }

    [Fact]
    public async Task ReviewOutcome_Post_ReturnsView_When_ValidationFails()
    {
        // Arrange
        var stored = new ReviewOutcomeViewModel();
        SetupTempData(stored);

        var model = new ReviewOutcomeViewModel
        {
            ReviewOutcome = "" // triggers validation failure
        };

        // Act
        var result = await Sut.ReviewOutcome(model, saveForLater: false);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        Sut.ModelState.ContainsKey(nameof(model.ReviewOutcome)).ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task ReviewOutcome_Post_SaveForLater_Redirects_To_MyTasklist
    (
        Guid modificationId,
        ReviewOutcomeViewModel stored
    )
    {
        // Arrange
        stored.ModificationDetails.ModificationId = modificationId.ToString();
        SetupTempData(stored);

        _modificationService
            .Setup(s => s.SaveModificationReviewResponses(It.IsAny<ProjectModificationReviewRequest>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.ReviewOutcome(stored, saveForLater: true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        redirect.ControllerName.ShouldBe("ModificationsTasklist");
    }

    [Theory, AutoData]
    public async Task ReviewOutcome_Post_Redirects_To_ReasonNotApproved_When_NotApproved
    (
        Guid modificationId,
        ReviewOutcomeViewModel stored
    )
    {
        // Arrange
        stored.ModificationDetails.ModificationId = modificationId.ToString();
        stored.ReviewOutcome = ModificationStatus.NotApproved;

        SetupTempData(stored);

        _modificationService
            .Setup(s => s.SaveModificationReviewResponses(It.IsAny<ProjectModificationReviewRequest>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.ReviewOutcome(stored);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.ReasonNotApproved));
    }

    [Theory, AutoData]
    public async Task ReviewOutcome_Post_Redirects_To_ConfirmReviewOutcome_When_Approved
    (
        Guid modificationId,
        ReviewOutcomeViewModel stored
    )
    {
        // Arrange
        stored.ModificationDetails.ModificationId = modificationId.ToString();
        stored.ReviewOutcome = ModificationStatus.Approved;

        SetupTempData(stored);

        _modificationService
            .Setup(s => s.SaveModificationReviewResponses(It.IsAny<ProjectModificationReviewRequest>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.ReviewOutcome(stored);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.ConfirmReviewOutcome));
    }

    [Fact]
    public void ReasonNotApproved_Get_ReturnsError_When_NoTempData()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.ReasonNotApproved();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);
    }

    [Theory, AutoData]
    public void ReasonNotApproved_Get_ReturnsView_When_TempData_Exists
    (
        Guid modificationId,
        ReviewOutcomeViewModel model
    )
    {
        // Arrange
        model.ModificationDetails.ModificationId = modificationId.ToString();
        SetupTempData(model);

        // Act
        var result = Sut.ReasonNotApproved();

        // Assert
        result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<ReviewOutcomeViewModel>();
    }

    [Fact]
    public async Task ReasonNotApproved_Post_ReturnsView_When_ValidationFails()
    {
        // Arrange
        SetupTempData(new ReviewOutcomeViewModel());

        var model = new ReviewOutcomeViewModel
        {
            ReasonNotApproved = "" // validation fail
        };

        // Act
        var result = await Sut.ReasonNotApproved(model, saveForLater: false);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        Sut.ModelState.ContainsKey(nameof(model.ReviewOutcome)).ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task ReasonNotApproved_Post_SaveForLater_Redirects_To_MyTasklist
    (
        Guid modificationId,
        ReviewOutcomeViewModel stored
    )
    {
        // Arrange
        stored.ModificationDetails.ModificationId = modificationId.ToString();
        SetupTempData(stored);

        _modificationService
            .Setup(s => s.SaveModificationReviewResponses(It.IsAny<ProjectModificationReviewRequest>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.ReasonNotApproved(stored, saveForLater: true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        redirect.ControllerName.ShouldBe("ModificationsTasklist");
    }

    [Theory, AutoData]
    public async Task ReasonNotApproved_Post_Redirects_To_ConfirmReviewOutcome_When_Valid
    (
        Guid modificationId,
        ReviewOutcomeViewModel stored
    )
    {
        // Arrange
        stored.ModificationDetails.ModificationId = modificationId.ToString();
        SetupTempData(stored);

        _modificationService
            .Setup(s => s.SaveModificationReviewResponses(It.IsAny<ProjectModificationReviewRequest>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.ReasonNotApproved(stored);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.ConfirmReviewOutcome));
    }

    [Fact]
    public void ConfirmReviewOutcome_Get_ReturnsError_When_NoTempData()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.ConfirmReviewOutcome();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);
    }

    [Theory, AutoData]
    public void ConfirmReviewOutcome_Get_ReturnsView_When_Valid
    (
        Guid modificationId,
        ReviewOutcomeViewModel model
    )
    {
        // Arrange
        model.ModificationDetails.ModificationId = modificationId.ToString();
        SetupTempData(model);

        // Act
        var result = Sut.ConfirmReviewOutcome();

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Theory, AutoData]
    public async Task SubmitReviewOutcome_ReturnsError_When_SaveResponses_Fails
    (
        Guid modificationId,
        ReviewOutcomeViewModel model
    )
    {
        // Arrange
        model.ModificationDetails.ModificationId = modificationId.ToString();
        SetupTempData(model);

        _modificationService
            .Setup(s => s.SaveModificationReviewResponses(It.IsAny<ProjectModificationReviewRequest>()))
            .ReturnsAsync(new ServiceResponse<object>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await Sut.SubmitReviewOutcome();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(400);
    }

    [Theory, AutoData]
    public async Task SubmitReviewOutcome_ReturnsError_When_UpdateStatus_Fails
    (
        Guid modificationId,
        ReviewOutcomeViewModel model
    )
    {
        // Arrange
        model.ModificationDetails.ModificationId = modificationId.ToString();
        model.ReviewOutcome = ModificationStatus.Approved;

        SetupTempData(model);

        _modificationService
            .Setup(s => s.SaveModificationReviewResponses(It.IsAny<ProjectModificationReviewRequest>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.OK });

        _modificationService
            .Setup(s => s.UpdateModificationStatus(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.BadRequest });

        // Act
        var result = await Sut.SubmitReviewOutcome();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(400);
    }

    [Theory, AutoData]
    public async Task SubmitReviewOutcome_Redirects_When_Successful
    (
        Guid modificationId,
        ReviewOutcomeViewModel model
    )
    {
        // Arrange
        model.ModificationDetails.ModificationId = modificationId.ToString();
        model.ReviewOutcome = ModificationStatus.Approved;

        SetupTempData(model);

        _modificationService
            .Setup(s => s.SaveModificationReviewResponses(It.IsAny<ProjectModificationReviewRequest>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.OK });

        _modificationService
            .Setup(s => s.UpdateModificationStatus(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.SubmitReviewOutcome();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.ReviewOutcomeSubmitted));
    }

    [Fact]
    public void ReviewOutcomeSubmitted_ClearsTempData_And_ReturnsView()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.ReviewOutcomeSubmitted();

        // Assert
        Sut.TempData.Count.ShouldBe(0);
        result.ShouldBeOfType<ViewResult>();
    }

    // ----------------------------
    // Helpers
    // ----------------------------

    private void SetupTempData(ReviewOutcomeViewModel model)
    {
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationsDetails] = JsonSerializer.Serialize(model)
        };
    }
}