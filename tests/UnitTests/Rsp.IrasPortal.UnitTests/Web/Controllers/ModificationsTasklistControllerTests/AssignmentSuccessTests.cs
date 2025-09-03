using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

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
            StatusCode = System.Net.HttpStatusCode.OK,
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