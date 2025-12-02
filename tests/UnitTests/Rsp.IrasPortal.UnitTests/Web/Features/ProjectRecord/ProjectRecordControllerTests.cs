using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Features.ProjectRecord.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.ProjectRecord;

public class ProjectRecordControllerTests : TestServiceBase<ProjectRecordController>
{
    private static DefaultHttpContext CreateHttpContextWithSession(out Mock<ISession> session)
    {
        session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };
        return httpContext;
    }

    [Fact]
    public async Task ProjectRecord_ReturnsServiceError_When_ProjectRecord_TempData_Missing()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(out var session);
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.ProjectRecord("section-1");

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ProjectRecord_ReturnsServiceError_When_QuestionSet_Has_No_Sections()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(out var session);
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        // Provide ProjectRecord in TempData
        var record = new ProjectRecordDto { IrasId = 1234, ShortProjectTitle = "Short", LongProjectTitle = "Long" };
        Sut.TempData[TempDataKeys.ProjectRecord] = JsonSerializer.Serialize(record);

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(ApiResponseFactory.Success(new CmsQuestionSetResponse { Sections = [] }));

        // Act
        var result = await Sut.ProjectRecord("section-1");

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ProjectRecord_Returns_View_With_Mapped_Model_When_Successful()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(out var session);
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var record = new ProjectRecordDto { IrasId = 5678, ShortProjectTitle = "Short T", LongProjectTitle = "Long T" };
        Sut.TempData[TempDataKeys.ProjectRecord] = JsonSerializer.Serialize(record);

        var section = new SectionModel { Id = "sec-1", SectionId = "s1" };
        var cmsResponse = new CmsQuestionSetResponse
        {
            Id = "qset-1",
            Version = "1.0",
            Sections = [section]
        };

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(ApiResponseFactory.Success(cmsResponse));

        // Act
        var result = await Sut.ProjectRecord("sec-1");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<ProjectRecordViewModel>();
        model.IrasId.ShouldBe(record.IrasId!.Value);
        model.ShortProjectTitle.ShouldBe(record.ShortProjectTitle);
        model.FullProjectTitle.ShouldBe(record.LongProjectTitle);
        model.SectionId.ShouldBe(section.Id);
    }

    [Fact]
    public async Task ConfirmProjectRecord_ReturnsServiceError_When_GetApplications_Fails()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(out var session);
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var model = new ProjectRecordViewModel
        {
            IrasId = 1111,
            ShortProjectTitle = "Short",
            FullProjectTitle = "Full",
            SectionId = "sec-1",
            Questions = []
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ConfirmProjectRecord(model);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ConfirmProjectRecord_Redirects_To_ProjectRecordExists_When_Duplicate_IrasId()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(out var session);
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        // Needed for GetRespondentFromContext
        httpContext.Items[ContextItemKeys.UserId] = "user-1";

        var model = new ProjectRecordViewModel
        {
            IrasId = 2222,
            ShortProjectTitle = "Short",
            FullProjectTitle = "Full",
            SectionId = "sec-1",
            Questions = []
        };

        Mocker
        .GetMock<IApplicationsService>()
        .Setup(s => s.GetApplications())
        .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = [
                new() { IrasId =2222, Id = "exists" }
            ]
        });

        // Act
        var result = await Sut.ConfirmProjectRecord(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("prc:projectrecordexists");
    }

    [Fact]
    public async Task ConfirmProjectRecord_ReturnsServiceError_When_CreateApplication_Fails()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(out var session);
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        httpContext.Items[ContextItemKeys.UserId] = "user-1";

        var model = new ProjectRecordViewModel
        {
            IrasId = 3333,
            ShortProjectTitle = "Short",
            FullProjectTitle = "Full",
            SectionId = "sec-1",
            Questions = []
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.CreateApplication(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ConfirmProjectRecord(model);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ConfirmProjectRecord_ReturnsServiceError_When_QuestionSections_Invalid()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(out var session);
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        httpContext.Items[ContextItemKeys.UserId] = "user-1";

        var model = new ProjectRecordViewModel
        {
            IrasId = 4444,
            ShortProjectTitle = "Short",
            FullProjectTitle = "Full",
            SectionId = "sec-1",
            Questions = [
                new() { QuestionId = "Q1", DataType = "Text", Index =0 }
            ]
        };

        var createdApp = new IrasApplicationResponse { Id = "abc", IrasId = 4444, ShortProjectTitle = "Short" };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.CreateApplication(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = createdApp
            });

        // Return OK but null content to trigger bad request handling
        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(s => s.GetQuestionSections())
            .ReturnsAsync(ApiResponseFactory.Success(Enumerable.Empty<QuestionSectionsResponse>()));

        // Act
        var result = await Sut.ConfirmProjectRecord(model);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ConfirmProjectRecord_Success_Redirects_To_Questionnaire_Resume_And_Sets_State()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(out var session);
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        // Required for GetRespondentFromContext and SaveProjectRecordAnswers
        httpContext.Items[ContextItemKeys.UserId] = "user-1";

        var model = new ProjectRecordViewModel
        {
            IrasId = 5555,
            ShortProjectTitle = "Short",
            FullProjectTitle = "Full",
            SectionId = "current-sec",
            Questions = [
                new() { QuestionId = "Q1", DataType = "Text", Index =0, AnswerText = "A1" }
            ]
        };

        var createdApp = new IrasApplicationResponse { Id = "new-id", IrasId = 5555, ShortProjectTitle = "Short" };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.CreateApplication(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = createdApp
            });

        var nextSection = new QuestionSectionsResponse
        {
            SectionId = "next-sec",
            QuestionCategoryId = "cat-1",
            SectionName = "Next"
        };

        var sections = new List<QuestionSectionsResponse>
        {
            new() { SectionId = model.SectionId, QuestionCategoryId = "cat-1", SectionName = "Current" },
            nextSection
        };

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(s => s.GetQuestionSections())
            .ReturnsAsync(ApiResponseFactory.Success(sections.AsEnumerable()));

        // Act
        var result = await Sut.ConfirmProjectRecord(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));
        redirect.ControllerName.ShouldBe("Questionnaire");
        redirect.RouteValues!["sectionId"].ShouldBe(nextSection.SectionId);
        redirect.RouteValues!["categoryId"].ShouldBe(nextSection.QuestionCategoryId);
        redirect.RouteValues!["projectRecordId"].ShouldBe(createdApp.Id);
        redirect.RouteValues!["ignorePreviousSection"].ShouldBe(true);

        // TempData should be set
        Sut.TempData[TempDataKeys.CategoryId].ShouldBe(nextSection.QuestionCategoryId);
        Sut.TempData[TempDataKeys.ProjectRecordId].ShouldBe(createdApp.Id);
        Sut.TempData[TempDataKeys.IrasId].ShouldBe(createdApp.IrasId);

        // Session should be set with created application
        session.Verify(s => s.Set(It.Is<string>(k => k == SessionKeys.ProjectRecord), It.IsAny<byte[]>()), Times.Once);
    }
}