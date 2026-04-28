using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ConfirmChangesTests : TestServiceBase<ReviewBodyController>
{
    [Theory]
    [AutoData]
    public async Task ConfirmChanges_WithValidModel_AndNoExistingRecId_ShouldReturnConfirmView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.EmailAddress = "valid.email@example.com";
        model.ResearchEthicsCommitteeId = "123";

        SetupValidValidation();

        Mocker.GetMock<IReviewBodyService>()
            .Setup(x => x.GetAllReviewBodies(
                It.Is<ReviewBodySearchRequest>(r =>
                    r.RecId == 123 &&
                    r.ReviewBodyType.Contains(ReviewBodyType.ResearchEthicsCommittee)),
                1,
                20,
                nameof(ReviewBodyDto.RegulatoryBodyName),
                SortDirections.Ascending))
            .ReturnsAsync(CreateReviewBodiesResponse(0));

        // Act
        var result = await Sut.ConfirmChanges(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ConfirmChanges");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmChanges_WithValidModel_AndExistingRecId_ShouldRedirectToRecIdAlreadyAdded(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.EmailAddress = "valid.email@example.com";
        model.ResearchEthicsCommitteeId = "123";
        model.ReviewBodyType = ReviewBodyType.ResearchEthicsCommittee;

        SetupValidValidation();

        Mocker.GetMock<IReviewBodyService>()
            .Setup(x => x.GetAllReviewBodies(
                It.Is<ReviewBodySearchRequest>(r =>
                    r.RecId == 123 &&
                    r.ReviewBodyType.Contains(ReviewBodyType.ResearchEthicsCommittee)),
                1,
                20,
                nameof(ReviewBodyDto.RegulatoryBodyName),
                SortDirections.Ascending))
            .ReturnsAsync(CreateReviewBodiesResponse(1));

        // Act
        var result = await Sut.ConfirmChanges(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("RecIdAlreadyAdded");
        redirectResult.RouteValues.ShouldNotBeNull();
    }

    [Theory]
    [AutoData]
    public async Task ConfirmChanges_WithInvalidModel_ShouldReturnCreateReviewBodyView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
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
                        PropertyName = "name",
                        ErrorMessage = "error"
                    }
                ]
            });

        // Act
        var result = await Sut.ConfirmChanges(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("CreateReviewBody");
        viewResult.Model.ShouldBeEquivalentTo(model);

        Sut.ModelState["name"]!.Errors.Single().ErrorMessage.ShouldBe("error");

        Mocker.GetMock<IReviewBodyService>()
            .Verify(x => x.GetAllReviewBodies(
                It.IsAny<ReviewBodySearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
                Times.Never);
    }

    private void SetupValidValidation()
    {
        Mocker.GetMock<IValidator<AddUpdateReviewBodyModel>>()
            .Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<AddUpdateReviewBodyModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static ServiceResponse<AllReviewBodiesResponse> CreateReviewBodiesResponse(int totalCount)
    {
        return new ServiceResponse<AllReviewBodiesResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllReviewBodiesResponse
            {
                TotalCount = totalCount
            }
        };
    }
}