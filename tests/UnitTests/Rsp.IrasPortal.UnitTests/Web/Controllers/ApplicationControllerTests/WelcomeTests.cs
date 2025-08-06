using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class WelcomeTests : TestServiceBase<ApplicationController>
{
    private readonly Mock<ISession> session = new();

    [Fact]
    public async Task Welcome_ReturnsViewResult_WithMappedResearchApplications()
    {
        // Arrange
        var respondentId = "RespondentId1";

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };
        httpContext.Items[ContextItemKeys.RespondentId] = respondentId;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var mockDate = DateTime.UtcNow;

        var mockApplications = new List<IrasApplicationResponse>
        {
            new()
            {
                IrasId = 123,
                Id = "App1",
                CreatedDate = mockDate,
                Status = "Created"
            }
        };

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = "IQA0002", AnswerText = "My Study Title" },
        };

        var appResponse = new ServiceResponse<IEnumerable<IrasApplicationResponse>>
        {
            Content = mockApplications
        };

        var answerResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            Content = answers
        };

        var apiRespondent = new ApiResponse<IEnumerable<RespondentAnswerDto>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            answerResponse.Content,
            new RefitSettings()
        );

        Mocker
            .GetMock<IApplicationsService>()
            .Setup
            (
                s => s.GetPaginatedApplicationsByRespondent
                (
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                )
            )
            .ReturnsAsync(new ServiceResponse<PaginatedResponse<IrasApplicationResponse>>
            {
                Content = new PaginatedResponse<IrasApplicationResponse>
                {
                    Items = mockApplications,
                    TotalCount = mockApplications.Count
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup
            (x => x.GetRespondentAnswers
                (
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(apiRespondent.ToServiceResponse());

        // Act
        var result = await Sut.Welcome();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");

        var viewModel = viewResult.Model.ShouldBeOfType<ApplicationsViewModel>();
        viewModel.Applications.Count().ShouldBe(1);

        var item = viewModel.Applications.First();
        item.IrasId.ShouldBe(123);
        item.Id.ShouldBe("App1");
        item.Title.ShouldBe("My Study Title");
        item.CreatedDate.ShouldBe(mockDate);
        item.Status.ShouldBe("Created");
    }
}