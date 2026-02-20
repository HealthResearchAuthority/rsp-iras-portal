using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.UnitTests;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ReviewAllChangesControllerTests;

public class WithdrawModificationsTests : TestServiceBase<ReviewAllChangesController>
{
    private readonly Mock<IProjectModificationsService> _modificationService;

    public WithdrawModificationsTests()
    {
        _modificationService = Mocker.GetMock<IProjectModificationsService>();
    }

    [Fact]
    public void WithdrawModification_Get_ReturnsError_When_NoTempData()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.WithdrawModification();

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);
    }

    [Theory]
    [AutoData]
    public void WithdrawModification_Get_ReturnsView_When_TempData_Exists
    (
        Guid modificationId,
        ReviewOutcomeViewModel model
    )
    {
        // Arrange
        model.ModificationDetails.ModificationId = modificationId.ToString();
        model.ReviewOutcome = null;
        SetupTempData(model);

        // Act
        var result = Sut.WithdrawModification();

        // Assert
        result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<ReviewOutcomeViewModel>();
    }

    [Theory]
    [AutoData]
    public async Task ConfirmWithdrawModification_Get_ReturnsError_When_NoTempData
    (
        string projectRecordId,
        Guid projectModificationId
    )
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.ConfirmWithdrawModification(projectRecordId, projectModificationId);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmWithdrawModification_Get_ReturnsView_When_TempData_Exists
    (
        string projectRecordId,
        Guid projectModificationId,
        ReviewOutcomeViewModel model
    )
    {
        // Arrange
        SetupTempData(model);

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.ConfirmWithdrawModification(projectRecordId, projectModificationId);

        // Assert
        result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<ReviewOutcomeViewModel>();
    }

    private void SetupTempData(ReviewOutcomeViewModel model)
    {
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationsDetails] = JsonSerializer.Serialize(model)
        };
    }
}