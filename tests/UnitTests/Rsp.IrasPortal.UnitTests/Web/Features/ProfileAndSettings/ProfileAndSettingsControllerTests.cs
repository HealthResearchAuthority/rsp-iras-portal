using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Features.ProfileAndSettings.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.ProfileAndSettings;

public class ProfileAndSettingsControllerTests : TestServiceBase<ProfileAndSettingsController>
{
    private readonly DefaultHttpContext _http;

    public ProfileAndSettingsControllerTests()
    {
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        Sut.TempData = tempData;
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory, AutoData]
    public void Index_Returns_NotFoundStatus_When_User_Not_Found(ClaimsPrincipal user)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.NotFound,
        };
        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponse);
        _http.User = user;

        // Act
        var result = Sut.Index()?.Result;

        // Assert
        var viewResult = result.ShouldBeOfType<StatusCodeResult>();
        viewResult.StatusCode.ShouldBe(404);
    }

    [Theory, AutoData]
    public void Index_Returns_View_When_User_Found(ClaimsPrincipal user, UserResponse userResponse)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };
        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponse);
        _http.User = user;

        // Act
        var result = Sut.Index()?.Result;

        // Assert
        Mocker.GetMock<IUserManagementService>()
           .Verify(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldNotBeNull();
        viewResult.Model.ShouldBeOfType<UserViewModel>();
    }

    [Theory, AutoData]
    public void EditProfile_Returns_NotFoundStatus_When_User_Not_Found(ClaimsPrincipal user)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.NotFound,
        };
        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponse);
        _http.User = user;

        // Act
        var result = Sut.EditProfile()?.Result;

        // Assert
        var viewResult = result.ShouldBeOfType<StatusCodeResult>();
        viewResult.StatusCode.ShouldBe(404);
    }

    [Theory, AutoData]
    public void EditProfile_Returns_View_When_User_Found(ClaimsPrincipal user, UserResponse userResponse)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };
        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponse);
        _http.User = user;

        // Act
        var result = Sut.EditProfile()?.Result;

        // Assert
        Mocker.GetMock<IUserManagementService>()
           .Verify(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldNotBeNull();
        viewResult.Model.ShouldBeOfType<UserViewModel>();
    }

    [Theory, AutoData]
    public void SaveProfile_Returns_View_When_Success(UserViewModel viewModel)
    {
        // Arrange
        var serviceResponse = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };
        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.UpdateUser(It.IsAny<UpdateUserRequest>()))
           .ReturnsAsync(serviceResponse);

        Mocker
            .GetMock<IValidator<UserViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<UserViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = Sut.SaveProfile(viewModel)?.Result;

        // Assert
        Mocker.GetMock<IUserManagementService>()
           .Verify(s => s.UpdateUser(It.IsAny<UpdateUserRequest>()), Times.Once);

        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        viewResult.ActionName.ShouldBe("Index");
    }

    [Theory, AutoData]
    public void SaveProfile_Returns_Error_When_Model_Invalid(UserViewModel viewModel)
    {
        // arrange
        // set values to null to trigger invalid response
        viewModel.GivenName = null;
        viewModel.FamilyName = null;

        Mocker
            .GetMock<IValidator<UserViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<UserViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
            {
                new("GivenName", "Required"),
                new("FamilyName", "Required")
            }));

        // Act
        var result = Sut.SaveProfile(viewModel)?.Result;

        // Assert
        Mocker.GetMock<IUserManagementService>()
           .Verify(s => s.UpdateUser(It.IsAny<UpdateUserRequest>()), Times.Never);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("EditProfileView");
    }

    [Theory, AutoData]
    public void SaveProfile_Returns_Error_When_Save_Unsuccessful(UserViewModel viewModel)
    {
        // arrange
        var serviceResponse = new ServiceResponse
        {
            StatusCode = HttpStatusCode.BadRequest
        };
        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.UpdateUser(It.IsAny<UpdateUserRequest>()))
           .ReturnsAsync(serviceResponse);

        Mocker
            .GetMock<IValidator<UserViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<UserViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = Sut.SaveProfile(viewModel)?.Result;

        // Assert
        Mocker.GetMock<IUserManagementService>()
           .Verify(s => s.UpdateUser(It.IsAny<UpdateUserRequest>()), Times.Once);

        var viewResult = result.ShouldBeOfType<StatusCodeResult>();
        viewResult.StatusCode.ShouldBe(400);
    }
}