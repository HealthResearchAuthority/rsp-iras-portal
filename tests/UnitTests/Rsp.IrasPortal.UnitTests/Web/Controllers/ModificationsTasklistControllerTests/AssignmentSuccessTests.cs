using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;

namespace Rsp.Portal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class AssignmentSuccessTests : TestServiceBase<ModificationsTasklistController>
{
    [Theory, AutoData]
    public async Task AssignmentSuccess_WhenCalled_ReturnsViewResult
    (
        string reviewerId,
        UserResponse userResponse
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()); ;
        Sut.TempData.TryAdd(TempDataKeys.ModificationTasklistReviewerId, reviewerId);

        var serviceResposne = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync(serviceResposne);

        // Act
        var result = await Sut.AssignmentSuccess();

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }
}