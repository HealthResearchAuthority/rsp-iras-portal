using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class EditApplicationTests : TestServiceBase<ApplicationController>
{
    [Theory, AutoData]
    public async Task EditApplication_ValidId_ReturnsViewResultWithModel(IrasApplicationResponse irasApplication, string applicationId)
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { Content = irasApplication, StatusCode = HttpStatusCode.OK });

        var session = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.EditApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ApplicationInfo");
        var model = viewResult.Model.ShouldBeOfType<ValueTuple<ApplicationInfoViewModel, string>>();
        model.Item1.Name.ShouldBe(irasApplication.Title);
        model.Item1.Description.ShouldBe(irasApplication.Description);
        model.Item2.ShouldBe("edit");

        // Verify
        session.Verify(s => s.Set(SessionKeys.Application, It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task EditApplication_InvalidId_ReturnsNotFoundResult()
    {
        // Arrange
        const string applicationId = "invalid-id";
        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.NotFound });

        // Act
        var result = await Sut.EditApplication(applicationId);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Theory, AutoData]
    public async Task EditApplication_ServiceError_ReturnsErrorView(string applicationId)
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.InternalServerError });

        var session = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.EditApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
    }
}