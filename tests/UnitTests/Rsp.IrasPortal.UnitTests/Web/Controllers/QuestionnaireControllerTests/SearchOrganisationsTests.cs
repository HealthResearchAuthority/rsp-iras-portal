using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class SearchOrganisationsTests : TestServiceBase<QuestionnaireController>
{
    [Theory, AutoData]
    public async Task SearchOrganisations_ReturnsViewWithModelError_WhenSearchTextIsNullOrTooShort
    (
        List<QuestionSectionsResponse> questionSectionsResponse
    )
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new() { Index = 0, QuestionId = "Q1" }
        };

        var application = new IrasApplicationResponse
        {
            ApplicationId = "App1",
            IrasId = 123
        };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "A",
            SponsorOrgSearchText = "ab",
            Questions = questions
        };

        var session = new Mock<ISession>();
        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) },
            { SessionKeys.Application, JsonSerializer.SerializeToUtf8Bytes(application) }
        };

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }
                value = null;
                return false;
            });

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        var responseQuestionSectionNull = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisations(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationSearchResponse
                {
                    Organisations = []
                }
            });

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await Sut.SearchOrganisations(model, null, null);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");
        Sut.ModelState.ContainsKey("sponsor_org_search").ShouldBeTrue();
        Sut.ModelState["sponsor_org_search"]!.Errors.Count.ShouldBe(1);
        ((QuestionnaireViewModel)viewResult.Model!).SponsorOrganisation.ShouldBeNullOrEmpty();
    }

    [Theory, AutoData]
    public async Task SearchOrganisations_ReturnsServiceError_WhenRtsServiceFails
    (
        List<QuestionSectionsResponse> questionSectionsResponse
    )
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new() { Index = 0, QuestionId = "Q1" }
        };

        var application = new IrasApplicationResponse
        {
            ApplicationId = "App1",
            IrasId = 123
        };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "A",
            SponsorOrgSearchText = "TestOrg",
            Questions = questions
        };

        var session = new Mock<ISession>();
        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) },
            { SessionKeys.Application, JsonSerializer.SerializeToUtf8Bytes(application) }
        };

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }
                value = null;
                return false;
            });

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        var responseQuestionSectionNull = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
        };

        Mocker.GetMock<IQuestionSetService>()
           .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisations(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.SearchOrganisations(model, null, null);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var problem = viewResult.Model.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData(10)]
    public async Task SearchOrganisations_ReturnsViewWithSponsorOrganisations_WhenSuccessful
    (
        int? pageSize,
        List<QuestionSectionsResponse> questionSectionsResponse
    )
    {
        // Arrange

        var questions = new List<QuestionViewModel>
        {
            new() { Index = 0, QuestionId = "Q1" }
        };

        var application = new IrasApplicationResponse
        {
            ApplicationId = "App1",
            IrasId = 123
        };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "A",
            SponsorOrgSearchText = "TestOrg",
            Questions = questions
        };

        var session = new Mock<ISession>();
        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) },
            { SessionKeys.Application, JsonSerializer.SerializeToUtf8Bytes(application) }
        };

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        var responseQuestionSectionNull = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }
                value = null;
                return false;
            });

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        var orgResponse = new OrganisationSearchResponse
        {
            Organisations =
            [
                new OrganisationDto { Id = "Org1" }
            ]
        };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisations(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = orgResponse
            });

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisations(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = orgResponse
            });

        // Act
        var result = await Sut.SearchOrganisations(model, null, pageSize);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");
        ((QuestionnaireViewModel)viewResult.Model!).SponsorOrganisation.ShouldBeNullOrEmpty();
        Sut.TempData[TempDataKeys.SponsorOrgSearched].ShouldBe("searched:true");
        Sut.TempData[TempDataKeys.ApplicationId].ShouldBe(application.ApplicationId);
        Sut.TempData[TempDataKeys.IrasId].ShouldBe(application.IrasId);
        Sut.TempData.ContainsKey(TempDataKeys.SponsorOrganisations).ShouldBeTrue();
    }
}