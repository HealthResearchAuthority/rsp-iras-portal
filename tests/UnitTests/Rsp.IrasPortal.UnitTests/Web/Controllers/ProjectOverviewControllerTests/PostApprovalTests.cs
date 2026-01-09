using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers.ProjectOverview;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectOverviewControllerTests;

public class PostApprovalTests : TestServiceBase<ProjectOverviewController>
{
    [Theory]
    [AutoData]
    public async Task PostApproval_Returns_View_With_Pagination_And_Modifications_Mapped(ProjectClosuresResponse closuresResponse, UserResponse userResponse)
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        ctx.Session = new InMemorySession(); // ensure Session is available to controller
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR1"
        };

        // Project record + answers for GetProjectOverview
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new()
                {
                    ShortProjectTitle = "title",
                    FullProjectTitle = "long",
                    IrasId = 123
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = [] });

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = HttpStatusCode.OK, Content = new() { Sections = [] } });

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsForProject(It.IsAny<string>(), It.IsAny<ModificationSearchRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new() { TotalCount = 1, Modifications = [new ModificationsDto { Id = Guid.NewGuid().ToString(), ModificationId = "MOD1", ModificationType = "Type", ReviewType = "Review", Category = "A", Status = ModificationStatus.InDraft }] }
            });

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecordAuditTrail(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<ProjectRecordAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectRecordAuditTrailResponse
                {
                    Items = []
                }
            });
        Mocker
           .GetMock<IProjectClosuresService>()
           .Setup(s => s.GetProjectClosureById(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<ProjectClosuresResponse>
           {
               StatusCode = HttpStatusCode.OK,
               Content = closuresResponse
           });

        Mocker
           .GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), null))
           .ReturnsAsync(new ServiceResponse<UserResponse>
           {
               StatusCode = HttpStatusCode.OK,
               Content = userResponse
           });

        // Act
        var result = await Sut.PostApproval("PR1", backRoute: null);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<PostApprovalViewModel>();
        model.Modifications.Count().ShouldBe(1);
        model.Pagination.ShouldNotBeNull();
        model.Pagination.TotalCount.ShouldBe(1);
    }
}