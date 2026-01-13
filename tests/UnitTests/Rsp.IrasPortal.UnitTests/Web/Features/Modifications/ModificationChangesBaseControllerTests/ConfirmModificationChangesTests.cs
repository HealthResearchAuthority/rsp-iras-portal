using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services;
using Rsp.Portal.Web.Features.Modifications;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ModificationChangesBaseControllerTests;

public class ConfirmModificationChangesTests : TestServiceBase<ModificationChangesBaseController>
{
    [Fact]
    public async Task ConfirmModificationChanges_Invokes_Ranking_And_Redirects()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.IrasId] = "IRAS",
            [TempDataKeys.ShortProjectTitle] = "Short",
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid()
        };

        Mocker.GetMock<IModificationRankingService>()
            .Setup(s => s.UpdateChangeRanking(It.IsAny<Guid>(), "PR1"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        Mocker.GetMock<IModificationRankingService>()
            .Setup(s => s.UpdateOverallRanking(It.IsAny<Guid>(), "PR1"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await Sut.ConfirmModificationChanges();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:modificationdetails");
        Mocker.GetMock<IModificationRankingService>().Verify();
    }
}
