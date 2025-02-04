using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class CreateApplicationTests : TestServiceBase<ApplicationController>
{
    [Theory, AutoData]
    public async Task CreateApplication_ValidModel_ReturnsNewApplicationView(ApplicationInfoViewModel model)
    {
        // Arrange
        Mocker
            .GetMock<IValidator<ApplicationInfoViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<ApplicationInfoViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        var httpContext = new DefaultHttpContext
        {
            Session = Mock.Of<ISession>(),
            Items = new Dictionary<object, object?>
            {
                { ContextItemKeys.RespondentId, "test-respondent-id" },
                { ContextItemKeys.Email, "test@example.com" },
                { ContextItemKeys.FirstName, "John" },
                { ContextItemKeys.LastName, "Doe"  }
            },
            User = new ClaimsPrincipal
            (
                new ClaimsIdentity
                (
                    [new(ClaimTypes.Role, "TestRole")]
                )
            )
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var createdApplication = new IrasApplicationResponse
        {
            ApplicationId = "test-app-id",
            Title = model.Name!,
            Description = model.Description!
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.CreateApplication(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { Content = createdApplication, StatusCode = HttpStatusCode.Created });

        // Act
        var result = await Sut.CreateApplication(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("NewApplication");
        viewResult.Model.ShouldBe(createdApplication);

        // Verify
        Mocker
            .GetMock<IApplicationsService>()
            .Verify
            (
                s => s
                    .CreateApplication(It.Is<IrasApplicationRequest>
                    (
                        r =>
                            r.Title == model.Name &&
                            r.Description == model.Description &&
                            r.Respondent.RespondentId == "test-respondent-id")
                    ),
                Times.Once
            );
    }

    [Fact]
    public async Task CreateApplication_InvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var model = new ApplicationInfoViewModel
        {
            Name = "",
            Description = "Test Description"
        };

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Name", "Name is required")
        });

        Mocker.GetMock<IValidator<ApplicationInfoViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<ApplicationInfoViewModel>>(), default))
            .ReturnsAsync(validationResult);

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.CreateApplication(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ApplicationInfo");
        var returnedModel = viewResult.Model.ShouldBeOfType<ValueTuple<ApplicationInfoViewModel, string>>();
        returnedModel.Item1.ShouldBe(model);
        returnedModel.Item2.ShouldBe("create");

        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState["Name"]
            .ShouldNotBeNull()
            .Errors[0]
            .ErrorMessage
            .ShouldBe("Name is required");
    }
}