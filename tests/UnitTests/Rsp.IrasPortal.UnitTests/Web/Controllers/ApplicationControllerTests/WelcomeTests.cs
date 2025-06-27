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

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests
{
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

            var mockApplications = new List<IrasApplicationResponse>
            {
                new IrasApplicationResponse
                {
                    IrasId = 123,
                    ApplicationId = "App1"
                }
            };

            var answers = new List<RespondentAnswerDto>
            {
                new RespondentAnswerDto { QuestionId = "IQA0002", AnswerText = "My Study Title" },
                new RespondentAnswerDto { QuestionId = "IQA0312", AnswerText = "NIHR Sponsor" }
            };

            var appResponse = new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                Content = mockApplications
            };

            var answerResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                Content = answers
            };

            var apiResponse = new ApiResponse<IEnumerable<IrasApplicationResponse>>
            (
                new HttpResponseMessage(HttpStatusCode.OK),
                appResponse.Content,
                new RefitSettings()
            );

            var apiRespondent = new ApiResponse<IEnumerable<RespondentAnswerDto>>
            (
                new HttpResponseMessage(HttpStatusCode.OK),
                answerResponse.Content,
                new RefitSettings()
            );

            Mocker
                .GetMock<IApplicationsService>()
                .Setup(s => s.GetApplicationsByRespondent(It.IsAny<string>()))
                .ReturnsAsync(apiResponse.ToServiceResponse());

            Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(apiRespondent.ToServiceResponse());

            // Act
            var result = await Sut.Welcome();

            // Assert
            var viewResult = result.ShouldBeOfType<ViewResult>();
            viewResult.ViewName.ShouldBe("Index");

            var model = viewResult.Model.ShouldBeAssignableTo<List<ResearchApplicationSummaryModel>>();
            model.Count.ShouldBe(1);

            var item = model[0];
            item.IrasId.ShouldBe(123);
            item.ApplicatonId.ShouldBe("App1");
            item.Title.ShouldBe("My Study Title");
            item.PrimarySponsorOrganisation.ShouldBe("NIHR Sponsor");
        }
    }
}