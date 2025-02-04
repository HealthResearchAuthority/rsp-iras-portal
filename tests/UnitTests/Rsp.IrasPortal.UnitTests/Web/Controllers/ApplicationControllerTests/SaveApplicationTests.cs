using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class SaveApplicationTests : TestServiceBase<ApplicationController>
{
    private readonly Mock<ISession> session = new();

    public SaveApplicationTests()
    {
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        httpContext.Items[ContextItemKeys.RespondentId] = "testRespondentId";
        httpContext.Items[ContextItemKeys.Email] = "test@example.com";
        httpContext.Items[ContextItemKeys.FirstName] = "Test";
        httpContext.Items[ContextItemKeys.LastName] = "User";
        httpContext.User = new ClaimsPrincipal
        (
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "testUser"),
                new Claim(ClaimTypes.Role, "TestRole"),
            ], "mock")
        );

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Theory, AutoData]
    public async Task SaveApplication_WhenValidationFails_ReturnsViewWithErrors
    (
        ApplicationInfoViewModel model,
        List<ValidationFailure> validationErrors
    )
    {
        // Arrange
        var validationResult = new ValidationResult(validationErrors);

        Mocker
            .GetMock<IValidator<ApplicationInfoViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<ApplicationInfoViewModel>>(), default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await Sut.SaveApplication(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ApplicationInfo");
        viewResult.Model.ShouldBe((model, "edit"));

        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ErrorCount.ShouldBe(validationErrors.Count);

        foreach (var error in validationErrors)
        {
            Sut
                .ModelState[error.PropertyName]
                .ShouldNotBeNull()
                .Errors[0].ErrorMessage
                .ShouldBe(error.ErrorMessage);
        }
    }

    [Theory, AutoData]
    public async Task SaveApplication_ShouldCorrectlyPopulateRespondentDtoFromHttpContextItems(ApplicationInfoViewModel model)
    {
        // Arrange
        var expectedRespondent = new RespondentDto
        {
            RespondentId = "testRespondentId",
            EmailAddress = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = "TestRole"
        };

        Mocker
            .GetMock<IValidator<ApplicationInfoViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<ApplicationInfoViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        await Sut.SaveApplication(model);

        // Assert
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.UpdateApplication(It.Is<IrasApplicationRequest>(r =>
                r.Respondent.RespondentId == expectedRespondent.RespondentId &&
                r.Respondent.EmailAddress == expectedRespondent.EmailAddress &&
                r.Respondent.FirstName == expectedRespondent.FirstName &&
                r.Respondent.LastName == expectedRespondent.LastName &&
                r.Respondent.Role == expectedRespondent.Role
            )), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveApplication_ShouldUseApplicationFromSession(ApplicationInfoViewModel model)
    {
        // Arrange
        var mockApplication = new IrasApplicationResponse
        {
            ApplicationId = "testApplicationId",
            CreatedBy = "Original Creator",
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        Mocker
            .GetMock<IValidator<ApplicationInfoViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<ApplicationInfoViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        session
            .Setup(s => s.TryGetValue(SessionKeys.Application, out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (key == SessionKeys.Application)
                {
                    value = JsonSerializer.SerializeToUtf8Bytes(mockApplication); // Assign the actual byte[] to the out parameter
                    return true;
                }
                else
                {
                    value = null; // Ensure the out parameter is always assigned
                    return false;
                }
            });

        // Act
        await Sut.SaveApplication(model);

        // Assert
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.UpdateApplication(It.Is<IrasApplicationRequest>
            (r =>
                r.ApplicationId == mockApplication.ApplicationId &&
                r.CreatedBy == mockApplication.CreatedBy &&
                r.StartDate == mockApplication.CreatedDate
            )), Times.Once);
    }
}