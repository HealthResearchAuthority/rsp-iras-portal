using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Web.Features.ProjectCollaborators.Models;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.ProjectCollaborators.Controllers;

namespace Rsp.Portal.UnitTests.Web.Features.ProjectCollaborators;

public class ProjectCollaboratorsControllerTests : TestServiceBase<ProjectCollaboratorsController>
{
    private const string ProjectRecordId = "project-123";

    public ProjectCollaboratorsControllerTests()
    {
        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }

    [Theory]
    [AutoData]
    public async Task SaveCollaborator_SavesCollaboratorAndRedirectsToProjectTeam(string userId)
    {
        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.SaveProjectCollaborator(It.IsAny<Rsp.IrasPortal.Application.DTOs.Requests.ProjectCollaboratorRequest>()))
            .ReturnsAsync(new ServiceResponse<Rsp.IrasPortal.Application.DTOs.Responses.ProjectCollaboratorResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new Rsp.IrasPortal.Application.DTOs.Responses.ProjectCollaboratorResponse
                {
                    UserId = userId,
                    ProjectRecordId = ProjectRecordId,
                    ProjectAccessLevel = "View"
                }
            });

        var result = await Sut.SaveCollaborator(new CollaboratorViewModel
        {
            ProjectRecordId = ProjectRecordId,
            UserId = userId,
            ProjectAccessLevel = "View"
        });

        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:projectteam");
        redirectResult.RouteValues!["ProjectRecordId"].ShouldBe(ProjectRecordId);

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Verify(s => s.SaveProjectCollaborator(It.Is<Rsp.IrasPortal.Application.DTOs.Requests.ProjectCollaboratorRequest>(r =>
                r.ProjectRecordId == ProjectRecordId &&
                r.UserId == userId &&
                r.ProjectAccessLevel == "View")), Times.Once);
    }

    [Fact]
    public async Task SelectCollaboratorAccess_ReturnsCollaboratorAccessView_WithModel()
    {
        var model = new CollaboratorViewModel
        {
            ProjectRecordId = ProjectRecordId,
            Email = "test@example.com",
            ProjectAccessLevel = "Edit"
        };

        var result = await Sut.SelectCollaboratorAccess(model);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("CollaboratorAccess");
        viewResult.Model.ShouldBe(model);
    }

    [Theory]
    [AutoData]
    public async Task SelectCollaboratorAccess_Post_UpdatesAccessAndRedirects(string collaboratorId)
    {
        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.UpdateCollaboratorAccess(It.IsAny<Rsp.IrasPortal.Application.DTOs.Requests.UpdateCollaboratorAccessRequest>()))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        var result = await Sut.UpdateCollaboratorAccess(new CollaboratorViewModel
        {
            Id = collaboratorId,
            ProjectRecordId = ProjectRecordId,
            ProjectAccessLevel = "View"
        });

        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:projectteam");
        redirectResult.RouteValues!["ProjectRecordId"].ShouldBe(ProjectRecordId);

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Verify(s => s.UpdateCollaboratorAccess(It.Is<Rsp.IrasPortal.Application.DTOs.Requests.UpdateCollaboratorAccessRequest>(r =>
                r.Id == collaboratorId &&
                r.ProjectAccessLevel == "View")), Times.Once);
    }

    [Fact]
    public async Task RemoveCollaborator_ReturnsDefaultView_WithModel()
    {
        var model = new CollaboratorViewModel
        {
            ProjectRecordId = ProjectRecordId,
            Email = "test@example.com"
        };

        var result = await Sut.RemoveCollaborator(model);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBeNull();
        viewResult.Model.ShouldBe(model);
    }

    [Theory]
    [AutoData]
    public async Task RemoveCollaborator_Post_RemovesCollaboratorAndRedirects(string collaboratorId)
    {
        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.RemoveProjectCollaborator(collaboratorId))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        var result = await Sut.ConfirmRemoveCollaborator(new CollaboratorViewModel
        {
            Id = collaboratorId,
            ProjectRecordId = ProjectRecordId,
            Self = false
        });

        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:projectteam");
        redirectResult.RouteValues!["ProjectRecordId"].ShouldBe(ProjectRecordId);

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Verify(s => s.RemoveProjectCollaborator(collaboratorId), Times.Once);
    }

    [Fact]
    public async Task SaveCollaborator_ReturnsCollaboratorAccessView_WhenProjectAccessLevelMissing()
    {
        var model = new CollaboratorViewModel
        {
            ProjectRecordId = ProjectRecordId,
            UserId = "user-1",
            ProjectAccessLevel = null
        };

        var result = await Sut.SaveCollaborator(model);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("CollaboratorAccess");
        viewResult.Model.ShouldBe(model);
        Sut.ModelState.ContainsKey(nameof(model.ProjectAccessLevel)).ShouldBeTrue();

        Mocker.GetMock<IProjectCollaboratorService>()
            .Verify(s => s.SaveProjectCollaborator(It.IsAny<Rsp.IrasPortal.Application.DTOs.Requests.ProjectCollaboratorRequest>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task SaveCollaborator_ReturnsServiceError_WhenSaveFails(string userId)
    {
        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.SaveProjectCollaborator(It.IsAny<Rsp.IrasPortal.Application.DTOs.Requests.ProjectCollaboratorRequest>()))
            .ReturnsAsync(new ServiceResponse<Rsp.IrasPortal.Application.DTOs.Responses.ProjectCollaboratorResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Error = "save failed"
            });

        var result = await Sut.SaveCollaborator(new CollaboratorViewModel
        {
            ProjectRecordId = ProjectRecordId,
            UserId = userId,
            ProjectAccessLevel = "View"
        });

        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmRemoveCollaborator_RedirectsToProjectOverview_WhenSelf(string collaboratorId)
    {
        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.RemoveProjectCollaborator(collaboratorId))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK
            });

        var result = await Sut.ConfirmRemoveCollaborator(new CollaboratorViewModel
        {
            Id = collaboratorId,
            ProjectRecordId = ProjectRecordId,
            Self = true
        });

        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("app:welcome");
    }
}