using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;
using Claim = System.Security.Claims.Claim;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests
{
    public class EditSponsorOrganisationUserTests : TestServiceBase<SponsorOrganisationsController>
    {
        private readonly DefaultHttpContext _http;

        public EditSponsorOrganisationUserTests()
        {
            _http = new DefaultHttpContext { Session = new InMemorySession() };
            Sut.ControllerContext = new ControllerContext { HttpContext = _http };
            Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
        }

        private void StubBuildModelPipeline(string rtsId, Guid userGuid, SponsorOrganisationUserDto sponsorOrganisationUserDto)
        {
            var orgName = "Acme Research Ltd";
            var userId = userGuid.ToString();

            Mocker.GetMock<ISponsorOrganisationService>()
                .Setup(s => s.GetUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = sponsorOrganisationUserDto
                });

            Mocker.GetMock<IUserManagementService>()
                .Setup(x => x.GetUser(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(new ServiceResponse<UserResponse>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new UserResponse
                    {
                        User = new User(
                            userId,
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            orgName,
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                });

            Mocker.GetMock<ISponsorOrganisationService>()
                .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
                .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new AllSponsorOrganisationsResponse
                    {
                        SponsorOrganisations = new List<SponsorOrganisationDto>
                        {
                            new() { IsActive = true, CreatedDate = new DateTime(2024, 5, 1) }
                        }
                    }
                });

            Mocker.GetMock<IRtsService>()
                .Setup(s => s.GetOrganisation(rtsId))
                .ReturnsAsync(new ServiceResponse<OrganisationDto>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new OrganisationDto { Id = rtsId, Name = orgName }
                });
        }

        [Theory]
        [AutoData]
        public async Task EditSponsorOrganisationUser_Get_WithoutTempDataError_ReturnsView_WithoutModelStateError(
            SponsorOrganisationUserDto sponsorOrganisationUserDto)
        {
            // Arrange
            const string rtsId = "87765";
            var userGuid = Guid.NewGuid();

            StubBuildModelPipeline(rtsId, userGuid, sponsorOrganisationUserDto);

            // Act
            var result = await Sut.EditSponsorOrganisationUser(null, rtsId, userGuid);

            // Assert
            var view = result.ShouldBeOfType<ViewResult>();
            view.Model.ShouldNotBeNull();
            Sut.ModelState.ContainsKey("IsAuthoriser").ShouldBeFalse();
        }

        [Theory]
        [AutoData]
        public async Task EditSponsorOrganisationUser_Get_WithTempDataError_ReturnsView_AndDoesNotAddModelStateError(SponsorOrganisationUserDto sponsorOrganisationUserDto)
        {
            // Arrange
            const string rtsId = "87765";
            var userGuid = Guid.NewGuid();

            StubBuildModelPipeline(rtsId, userGuid, sponsorOrganisationUserDto);

            var errorMessage = "Select 'Yes' for the Authoriser if the user has the Organisation Administrator role.";
            Sut.TempData["AuthorizerValidationError"] = errorMessage;

            // Act
            var result = await Sut.EditSponsorOrganisationUser(null, rtsId, userGuid);

            // Assert
            var view = result.ShouldBeOfType<ViewResult>();
            view.Model.ShouldNotBeNull();

            Sut.ModelState.IsValid.ShouldBeTrue();
            Sut.ModelState.ContainsKey("IsAuthoriser").ShouldBeFalse();
        }

        [Theory]
        [AutoData]
        public async Task SubmitEditSponsorOrganisationUser_Post_Rsp6809_OrgAdminAndNotAuthoriser_ForcesAuthoriserTrue_UpdatesProfileAndRoles_SetsBanner_AndRedirectsToView(
            SponsorOrganisationUserModel model)
        {
            // Arrange
            _http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, Roles.SystemAdministrator) }, "TestAuth"));
            Sut.ControllerContext = new ControllerContext { HttpContext = _http };

            var tempDataProvider = new Mock<ITempDataProvider>();
            Sut.TempData = new TempDataDictionary(_http, tempDataProvider.Object);

            model.SponsorOrganisationUser ??= new SponsorOrganisationUserDto();
            model.SponsorOrganisationUser.RtsId = "87765";
            model.SponsorOrganisationUser.UserId = Guid.NewGuid();
            model.SponsorOrganisationUser.Email = "test.test@example.com";
            model.SponsorOrganisationUser.SponsorRole = Roles.OrganisationAdministrator;
            model.SponsorOrganisationUser.IsAuthoriser = false;

            StubBuildModelPipeline(model.SponsorOrganisationUser.RtsId, model.SponsorOrganisationUser.UserId, model.SponsorOrganisationUser);

            var validatorMock = Mocker.GetMock<IValidator<SponsorOrganisationUserModel>>();

            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<SponsorOrganisationUserModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            SponsorOrganisationUserDto? passedUpdateDto = null;
            Mocker.GetMock<ISponsorOrganisationService>()
                .Setup(s => s.UpdateSponsorOrganisationUser(It.IsAny<SponsorOrganisationUserDto>()))
                .Callback<SponsorOrganisationUserDto>(dto => passedUpdateDto = dto)
                .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new SponsorOrganisationUserDto()
                });

            Mocker.GetMock<IUserManagementService>()
                .Setup(u => u.UpdateRoles(
                    It.Is<string>(e => e == model.SponsorOrganisationUser.Email),
                    It.IsAny<string?>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ServiceResponse
                {
                    StatusCode = HttpStatusCode.OK
                });

            // Act
            var result = await Sut.SubmitEditSponsorOrganisationUser(model);

            // Assert
            var redirect = result.ShouldBeOfType<RedirectToActionResult>();
            redirect.ActionName.ShouldBe(nameof(SponsorOrganisationsController.ViewSponsorOrganisationUser));
            redirect.RouteValues!["rtsId"].ShouldBe(model.SponsorOrganisationUser.RtsId);
            redirect.RouteValues!["userId"].ShouldBe(model.SponsorOrganisationUser.UserId);

            Sut.TempData.ContainsKey(TempDataKeys.ShowNotificationBanner).ShouldBeTrue();
            ((bool)Sut.TempData[TempDataKeys.ShowNotificationBanner]!).ShouldBeTrue();

            Sut.TempData.ContainsKey("AuthorizerValidationError").ShouldBeFalse();

            passedUpdateDto.ShouldNotBeNull();
            passedUpdateDto!.RtsId.ShouldBe(model.SponsorOrganisationUser.RtsId);
            passedUpdateDto.UserId.ShouldBe(model.SponsorOrganisationUser.UserId);
            passedUpdateDto.SponsorRole.ShouldBe(model.SponsorOrganisationUser.SponsorRole);
            passedUpdateDto.IsAuthoriser.ShouldBeTrue();

            Mocker.GetMock<IUserManagementService>()
                .Verify(u => u.UpdateRoles(
                        model.SponsorOrganisationUser.Email!,
                        It.IsAny<string?>(),
                        It.IsAny<string>()),
                    Times.Once);
        }

        [Fact]
        public async Task Should_Return_EditView_WithModelStateErrors_And_BuiltModelParts_When_ValidationFails()
        {
            // Arrange
            _http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, Roles.SystemAdministrator) }, "TestAuth"));
            Sut.ControllerContext = new ControllerContext { HttpContext = _http };
            var tempDataProvider = new Mock<ITempDataProvider>();
            Sut.TempData = new TempDataDictionary(_http, tempDataProvider.Object);

            var model = new SponsorOrganisationUserModel
            {
                SponsorOrganisationUser = new SponsorOrganisationUserDto
                {
                    RtsId = "87765",
                    UserId = Guid.NewGuid(),
                    Email = "test@example.com",
                    SponsorRole = Roles.Sponsor,
                    IsAuthoriser = false
                }
            };

            StubBuildModelPipeline(model.SponsorOrganisationUser.RtsId, model.SponsorOrganisationUser.UserId, model.SponsorOrganisationUser);

            var errorMessage = "Select 'Yes' for the Authoriser if the user has the Organisation Administrator role.";
            Mocker.GetMock<IValidator<SponsorOrganisationUserModel>>()
                .Setup(v => v.ValidateAsync(It.IsAny<SponsorOrganisationUserModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                new ValidationFailure("SponsorOrganisationUser.IsAuthoriser", errorMessage)
                }));

            // Act
            var result = await Sut.SubmitEditSponsorOrganisationUser(model);

            // Assert
            var view = result.ShouldBeOfType<ViewResult>();
            view.ViewName.ShouldBe(nameof(SponsorOrganisationsController.EditSponsorOrganisationUser));
            view.Model.ShouldBeOfType<SponsorOrganisationUserModel>();

            Sut.ModelState.ContainsKey("SponsorOrganisationUser.IsAuthoriser").ShouldBeTrue();
            var entry = Sut.ModelState["SponsorOrganisationUser.IsAuthoriser"];
            entry!.Errors.Single().ErrorMessage.ShouldBe(errorMessage);

            var returned = (SponsorOrganisationUserModel)view.Model!;
            returned.User.ShouldNotBeNull();
            returned.SponsorOrganisation.ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Return_ServiceError_When_UpdateProfile_Fails()
        {
            // Arrange
            _http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, Roles.SystemAdministrator) }, "TestAuth"));
            Sut.ControllerContext = new ControllerContext { HttpContext = _http };
            var tempDataProvider = new Mock<ITempDataProvider>();
            Sut.TempData = new TempDataDictionary(_http, tempDataProvider.Object);

            // Arrange
            var model = new SponsorOrganisationUserModel
            {
                SponsorOrganisationUser = new SponsorOrganisationUserDto
                {
                    RtsId = "87765",
                    UserId = Guid.NewGuid(),
                    Email = "test@example.com",
                    SponsorRole = Roles.Sponsor,
                    IsAuthoriser = false
                }
            };

            StubBuildModelPipeline(model.SponsorOrganisationUser.RtsId, model.SponsorOrganisationUser.UserId, model.SponsorOrganisationUser);

            Mocker.GetMock<IValidator<SponsorOrganisationUserModel>>()
                .Setup(v => v.ValidateAsync(It.IsAny<SponsorOrganisationUserModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            Mocker.GetMock<ISponsorOrganisationService>()
                .Setup(s => s.UpdateSponsorOrganisationUser(It.IsAny<SponsorOrganisationUserDto>()))
                .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = null
                });

            // Act
            var result = await Sut.SubmitEditSponsorOrganisationUser(model);

            // Assert
            var statusResult = result as IStatusCodeActionResult;
            statusResult.ShouldNotBeNull();
            statusResult!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);

            Mocker.GetMock<IUserManagementService>()
                .Verify(u => u.UpdateRoles(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        private async Task Should_Return_ServiceError_When_UpdateRoles_Fails()
        {
            // Arrange
            _http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, Roles.SystemAdministrator) }, "TestAuth"));
            Sut.ControllerContext = new ControllerContext { HttpContext = _http };
            var tempDataProvider = new Mock<ITempDataProvider>();
            Sut.TempData = new TempDataDictionary(_http, tempDataProvider.Object);

            // Arrange
            var model = new SponsorOrganisationUserModel
            {
                SponsorOrganisationUser = new SponsorOrganisationUserDto
                {
                    RtsId = "87765",
                    UserId = Guid.NewGuid(),
                    Email = "test@example.com",
                    SponsorRole = Roles.Sponsor,
                    IsAuthoriser = false
                }
            };

            StubBuildModelPipeline(model.SponsorOrganisationUser.RtsId, model.SponsorOrganisationUser.UserId, model.SponsorOrganisationUser);

            Mocker.GetMock<IValidator<SponsorOrganisationUserModel>>()
                .Setup(v => v.ValidateAsync(It.IsAny<SponsorOrganisationUserModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            Mocker.GetMock<ISponsorOrganisationService>()
                .Setup(s => s.UpdateSponsorOrganisationUser(It.IsAny<SponsorOrganisationUserDto>()))
                .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new SponsorOrganisationUserDto()
                });

            Mocker.GetMock<IUserManagementService>()
                .Setup(u => u.UpdateRoles(
                    It.Is<string>(e => e == model.SponsorOrganisationUser.Email),
                    It.IsAny<string?>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ServiceResponse
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            // Act
            var result = await Sut.SubmitEditSponsorOrganisationUser(model);

            // Assert
            var statusResult = result as IStatusCodeActionResult;
            statusResult.ShouldNotBeNull();
            statusResult!.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        }
    }
}