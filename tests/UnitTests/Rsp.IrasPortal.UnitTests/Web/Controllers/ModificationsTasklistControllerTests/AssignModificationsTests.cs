using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Requests.UserManagement;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;

namespace Rsp.Portal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class AssignModificationsTests : TestServiceBase<ModificationsTasklistController>
{
    [Fact]
    public async Task AssignModifications_WhenNoIdsSelected_AddsModelErrorAndRedirects()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        Sut.TempData = tempData;

        // Act
        var result = await Sut.AssignModifications(new List<string>());

        // Assert
        Sut.ModelState.ContainsKey(ModificationsTasklist.ModificationToAssignNotSelected).ShouldBeTrue();
        tempData.ContainsKey(TempDataKeys.ModelState).ShouldBeTrue();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Theory, AutoData]
    public async Task AssignModifications_GET_ModificationServiceFails_AddsModelErrorAndRedirects
    (
        List<string> modificationIds
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.BadRequest
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsByIds(modificationIds))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.AssignModifications(modificationIds);

        // Assert
        Sut.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
        Sut.TempData.ContainsKey(TempDataKeys.ModelState).ShouldBeTrue();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Theory, AutoData]
    public async Task AssignModifications_GET_UserServiceFails_AddsModelErrorAndRedirects
    (
        List<string> modificationIds,
        GetModificationsResponse modificationsResponse
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        var modificationServiceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationsResponse
        };

        var userServiceResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.BadRequest
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsByIds(modificationIds))
            .ReturnsAsync(modificationServiceResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsers
            (
                It.IsAny<SearchUserRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(userServiceResponse);

        // Act
        var result = await Sut.AssignModifications(modificationIds);

        // Assert
        Sut.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
        Sut.TempData.ContainsKey(TempDataKeys.ModelState).ShouldBeTrue();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Theory, AutoData]
    public async Task AssignModifications_GET_ReviewBodyServiceFails_AddsModelErrorAndRedirects
    (
        List<string> modificationIds,
        GetModificationsResponse modificationsResponse,
        UsersResponse userResponse
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        var modificationServiceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationsResponse
        };

        var userServiceResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        var reviewBodyServiceResponse = new ServiceResponse<AllReviewBodiesResponse>
        {
            StatusCode = HttpStatusCode.BadRequest,
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsByIds(modificationIds))
            .ReturnsAsync(modificationServiceResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsers
            (
                It.IsAny<SearchUserRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(userServiceResponse);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies
            (
                It.IsAny<ReviewBodySearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(reviewBodyServiceResponse);

        // Act
        var result = await Sut.AssignModifications(modificationIds);

        // Assert
        Sut.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
        Sut.TempData.ContainsKey(TempDataKeys.ModelState).ShouldBeTrue();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Theory, AutoData]
    public async Task AssignModifications_GET_SuccessfulRetrieval_ReturnsViewWithViewModel
    (
        List<string> modificationIds,
        GetModificationsResponse modificationsResponse,
        UsersResponse userResponse,
        AllReviewBodiesResponse reviewBodiesResponse,
        List<ReviewBodyUserDto> reviewBodyUsersResponse
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        var modificationServiceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationsResponse
        };

        var userServiceResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        var reviewBodyServiceResponse = new ServiceResponse<AllReviewBodiesResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodiesResponse
        };

        var reviewBodyUsersServiceResponse = new ServiceResponse<List<ReviewBodyUserDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodyUsersResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsByIds(modificationIds))
            .ReturnsAsync(modificationServiceResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsers
            (
                It.IsAny<SearchUserRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(userServiceResponse);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies
            (
                It.IsAny<ReviewBodySearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(reviewBodyServiceResponse);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetUserReviewBodiesByReviewBodyIds
            (
                It.IsAny<List<Guid>>()
            ))
            .ReturnsAsync(reviewBodyUsersServiceResponse);

        // Act
        var result = await Sut.AssignModifications(modificationIds);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
    }

    [Theory, AutoData]
    public async Task AssignModifications_POST_NoModificationsSelected_AddsModelErrorAndRedirects
    (
        string reviewerId
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()); ;

        // Act
        var result = await Sut.AssignModifications([], reviewerId);

        // Assert
        Sut.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
        Sut.TempData.ContainsKey(TempDataKeys.ModelState).ShouldBeTrue();
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Theory, AutoData]
    public async Task AssignModifications_POST_ModificationsServiceFails_AddsModelErrorAndRedirects
    (
        List<string> modificationIds,
        UserResponse userResponse,
        string reviewerId
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse> { StatusCode = HttpStatusCode.OK, Content = userResponse });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.AssignModificationsToReviewer(modificationIds, reviewerId, userResponse.User.Email, It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.BadRequest });

        // Act
        var result = await Sut.AssignModifications(modificationIds, reviewerId);

        // Assert
        Sut.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
        Sut.TempData.ContainsKey(TempDataKeys.ModelState).ShouldBeTrue();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Theory, AutoData]
    public async Task AssignModifications_POST_UserServiceFails_AddsModelErrorAndRedirects
    (
        List<string> modificationIds,
        string reviewerId
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse> { StatusCode = HttpStatusCode.BadRequest });

        // Act
        var result = await Sut.AssignModifications(modificationIds, reviewerId);

        // Assert
        Sut.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
        Sut.TempData.ContainsKey(TempDataKeys.ModelState).ShouldBeTrue();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Theory, AutoData]
    public async Task AssignModifications_SuccessfulAssignment_StoresReviewerIdAndRedirects
    (
        List<string> modificationIds,
        UserResponse userResponse,
        string reviewerId
    )
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse> { StatusCode = HttpStatusCode.OK, Content = userResponse });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.AssignModificationsToReviewer(modificationIds, reviewerId, userResponse.User.Email, $"{userResponse.User.GivenName} {userResponse.User.FamilyName}"))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.AssignModifications(modificationIds, reviewerId);

        // Assert
        Sut.TempData.ContainsKey(TempDataKeys.ModificationTasklistReviewerId).ShouldBeTrue();
        Sut.TempData[TempDataKeys.ModificationTasklistReviewerId].ShouldBe(reviewerId);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.AssignmentSuccess));
    }
}