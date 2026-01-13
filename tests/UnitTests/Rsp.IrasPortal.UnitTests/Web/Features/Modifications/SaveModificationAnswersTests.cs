using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications;

public class SaveModificationAnswersTests : TestServiceBase<ModificationsController>
{
    private readonly Mock<IRespondentService> _respondentService;

    public SaveModificationAnswersTests()
    {
        _respondentService = Mocker.GetMock<IRespondentService>();

        var ctx = new DefaultHttpContext();
        ctx.Items[ContextItemKeys.UserId] = "RESP1";
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1"
        };
    }

    [Fact]
    public async Task SaveModificationAnswers_Should_Return_Error_View_When_Service_Fails()
    {
        // Arrange
        _respondentService
            .Setup(s => s.SaveModificationChangeAnswers(It.IsAny<ProjectModificationChangeAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.BadRequest, Error = "fail" });

        // Act
        var result = await Sut.SaveModificationAnswers([], "pmc:any");

        // Assert
        result
            .ShouldBeOfType<StatusCodeResult>()
            .StatusCode
            .ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SaveModificationAnswers_Should_Redirect_To_PostApproval_When_RouteName_Is_PostApproval()
    {
        // Arrange
        _respondentService
            .Setup(s => s.SaveModificationChangeAnswers(It.IsAny<ProjectModificationChangeAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.SaveModificationAnswers([], "pov:postapproval");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
    }

    [Fact]
    public async Task SaveModificationAnswers_Should_Redirect_To_Route_When_Success()
    {
        // Arrange
        _respondentService
            .Setup(s => s.SaveModificationChangeAnswers(It.IsAny<ProjectModificationChangeAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.SaveModificationAnswers([], "pmc:next");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:next");
    }
}