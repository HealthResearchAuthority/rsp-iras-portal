using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

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
            Id = "App1",
            IrasId = 123
        };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "A",
            SponsorOrgSearch = new() { SearchText = "ab" },
            Questions = questions
        };

        var session = new Mock<ISession>();
        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) },
            { SessionKeys.ProjectRecord, JsonSerializer.SerializeToUtf8Bytes(application) }
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

        session.Setup(s => s.Keys).Returns(sessionData.Keys);

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

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<List<string>?>(), It.IsAny<string>(), It.IsAny<string>()))
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

        context.Items[ContextItemKeys.UserId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };
        Sut.TempData[TempDataKeys.OrgSearchReturnUrl] = "/";

        // Act
        var result = await Sut.SearchOrganisations(model, null, null);

        // Assert
        var viewResult = result.ShouldBeOfType<RedirectResult>();
        Sut.ModelState.ContainsKey("sponsor_org_search").ShouldBeTrue();
        Sut.ModelState["sponsor_org_search"]!.Errors.Count.ShouldBe(1);

        model.SponsorOrgSearch.SelectedOrganisation.ShouldBeNullOrEmpty();
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
            Id = "App1",
            IrasId = 123
        };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "A",
            SponsorOrgSearch = new() { SearchText = "TestOrg" },
            Questions = questions
        };

        var session = new Mock<ISession>();
        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) },
            { SessionKeys.ProjectRecord, JsonSerializer.SerializeToUtf8Bytes(application) }
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

        session.Setup(s => s.Keys).Returns(sessionData.Keys);

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

        Mocker.GetMock<ICmsQuestionsetService>()
           .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<List<string>?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        Sut.TempData[TempDataKeys.OrgSearchReturnUrl] = "/";

        // Act
        var result = await Sut.SearchOrganisations(model, null, null);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
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
            Id = "App1",
            IrasId = 123
        };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "A",
            SponsorOrgSearch = new() { SearchText = "TestOrg" },
            Questions = questions
        };

        var session = new Mock<ISession>();
        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) },
            { SessionKeys.ProjectRecord, JsonSerializer.SerializeToUtf8Bytes(application) }
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

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<ICmsQuestionsetService>()
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

        session.Setup(s => s.Keys).Returns(sessionData.Keys);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.UserId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        var orgResponse = new OrganisationSearchResponse
        {
            Organisations =
            [
                new OrganisationDto { Id = "Org1" }
            ]
        };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<List<string>?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = orgResponse
            });

        Sut.TempData[TempDataKeys.OrgSearchReturnUrl] = "/";

        // Act
        var result = await Sut.SearchOrganisations(model, null, pageSize);

        // Assert
        var viewResult = result.ShouldBeOfType<RedirectResult>();
        model.SponsorOrgSearch.SelectedOrganisation.ShouldBeNullOrEmpty();
        Sut.TempData[TempDataKeys.SponsorOrgSearched].ShouldBe("searched:true");
        Sut.TempData.ContainsKey(TempDataKeys.SponsorOrganisations).ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task SearchOrganisations_FallsBackToCms_WhenSessionMissing_ProjectsJourney(
    List<QuestionSectionsResponse> questionSectionsResponse)
    {
        var currentStage = "A";

        var questionsFromCms = new List<QuestionViewModel>
        {
            new() { Index = 0, QuestionId = "Q_FROM_CMS_1" }
        };

        var application = new IrasApplicationResponse { Id = "App1", IrasId = 123 };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = currentStage,
            SponsorOrgSearch = new() { SearchText = "TestOrg" },
            Questions = new List<QuestionViewModel>()
        };

        var sessionData = new Dictionary<string, byte[]?>
        {
            { SessionKeys.ProjectRecord, JsonSerializer.SerializeToUtf8Bytes(application) }
        };

        var session = new Mock<ISession>();

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.TryGetValue(key, out var bytes))
                {
                    value = bytes;
                    return true;
                }
                value = null;
                return false;
            });

        session.Setup(s => s.Keys).Returns(sessionData.Keys);

        session
            .Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, val) =>
            {
                if (key == $"{SessionKeys.Questionnaire}:{currentStage}")
                    sessionData[key] = JsonSerializer.SerializeToUtf8Bytes(questionsFromCms);
                else
                    sessionData[key] = val;
            });

        var context = new DefaultHttpContext { Session = session.Object };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
        Sut.TempData[TempDataKeys.OrgSearchReturnUrl] = "/";

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
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSet(currentStage, It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse()
            });

        var orgResponse = new OrganisationSearchResponse
        {
            Organisations = [new OrganisationDto { Id = "Org1" }]
        };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(
                It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int?>(),
                It.IsAny<List<string>?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = orgResponse
            });

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        var result = await Sut.SearchOrganisations(model, null, pageSize: 5);

        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldBe("/");

        Mocker.GetMock<ICmsQuestionsetService>()
            .Verify(q => q.GetQuestionSet(currentStage, It.IsAny<string?>()), Times.Once);

        sessionData.ContainsKey($"{SessionKeys.Questionnaire}:{currentStage}").ShouldBeTrue();

        var savedBytes = sessionData[$"{SessionKeys.Questionnaire}:{currentStage}"];
        var savedQuestions = JsonSerializer.Deserialize<List<QuestionViewModel>>(savedBytes!)!;
        savedQuestions.Count.ShouldBe(1);
        savedQuestions[0].QuestionId.ShouldBe("Q_FROM_CMS_1");

        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisationsByName(
                model.SponsorOrgSearch.SearchText, It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int?>(),
                It.IsAny<List<string>?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);

        Sut.TempData[TempDataKeys.SponsorOrgSearched].ShouldBe("searched:true");
        Sut.TempData.ContainsKey(TempDataKeys.SponsorOrganisations).ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task SearchOrganisations_FallsBackToCms_WhenSessionMissing_ModificationJourney(
        List<QuestionSectionsResponse> questionSectionsResponse)
    {
        var currentStage = "A";
        var modificationId = "MOD-123";

        var questionsFromCms = new List<QuestionViewModel>
    {
        new() { Index = 0, QuestionId = "Q_FROM_CMS_MOD" }
    };

        var application = new IrasApplicationResponse { Id = "App1", IrasId = 123 };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = currentStage,
            SponsorOrgSearch = new() { SearchText = "TestOrg" },
            Questions = new List<QuestionViewModel>()
        };

        var sessionData = new Dictionary<string, byte[]?>();

        var session = new Mock<ISession>();

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.TryGetValue(key, out var bytes))
                {
                    value = bytes;
                    return true;
                }
                value = null;
                return false;
            });

        session.Setup(s => s.Keys).Returns(sessionData.Keys);

        session
            .Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, val) =>
            {
                if (key == $"{SessionKeys.Questionnaire}:{currentStage}")
                    sessionData[key] = JsonSerializer.SerializeToUtf8Bytes(questionsFromCms);
                else
                    sessionData[key] = val;
            });

        var context = new DefaultHttpContext { Session = session.Object };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
        Sut.TempData[TempDataKeys.OrgSearchReturnUrl] = "/";
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationId;

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

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
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetModificationQuestionSet(currentStage, It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse()
            });

        var orgResponse = new OrganisationSearchResponse
        {
            Organisations = [new OrganisationDto { Id = "Org1" }]
        };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(
                It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int?>(),
                It.IsAny<List<string>?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = orgResponse
            });

        var result = await Sut.SearchOrganisations(model, null, pageSize: 5);

        result.ShouldBeOfType<RedirectResult>();

        Mocker.GetMock<ICmsQuestionsetService>()
            .Verify(q => q.GetModificationQuestionSet(currentStage, It.IsAny<string?>()), Times.Once);
        Mocker.GetMock<ICmsQuestionsetService>()
            .Verify(q => q.GetQuestionSet(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);

        sessionData.ContainsKey($"{SessionKeys.Questionnaire}:{currentStage}").ShouldBeTrue();

        var savedBytes = sessionData[$"{SessionKeys.Questionnaire}:{currentStage}"];
        var savedQuestions = JsonSerializer.Deserialize<List<QuestionViewModel>>(savedBytes!)!;
        savedQuestions.Count.ShouldBe(1);
        savedQuestions[0].QuestionId.ShouldBe("Q_FROM_CMS_MOD");

        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisationsByName(
                model.SponsorOrgSearch.SearchText, It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int?>(),
                It.IsAny<List<string>?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);

        Sut.TempData[TempDataKeys.SponsorOrgSearched].ShouldBe("searched:true");
        Sut.TempData.ContainsKey(TempDataKeys.SponsorOrganisations).ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task SearchOrganisations_ShortSearchText_AfterCmsFallback_ReturnsRedirectWithModelError(
        List<QuestionSectionsResponse> questionSectionsResponse)
    {
        var currentStage = "A";

        var questionsFromCms = new List<QuestionViewModel>
    {
        new() { Index = 0, QuestionId = "Q_FROM_CMS_1" }
    };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = currentStage,
            SponsorOrgSearch = new() { SearchText = "ab" },
            Questions = new List<QuestionViewModel>()
        };

        var sessionData = new Dictionary<string, byte[]?>();

        var session = new Mock<ISession>();

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.TryGetValue(key, out var bytes))
                {
                    value = bytes;
                    return true;
                }
                value = null;
                return false;
            });

        session.Setup(s => s.Keys).Returns(sessionData.Keys);

        session
            .Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, val) =>
            {
                if (key == $"{SessionKeys.Questionnaire}:{currentStage}")
                    sessionData[key] = JsonSerializer.SerializeToUtf8Bytes(questionsFromCms);
                else
                    sessionData[key] = val;
            });

        var context = new DefaultHttpContext { Session = session.Object };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
        Sut.ControllerContext = new ControllerContext { HttpContext = context };
        Sut.TempData[TempDataKeys.OrgSearchReturnUrl] = "/";

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
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSet(currentStage, It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse()
            });

        var result = await Sut.SearchOrganisations(model, null, pageSize: 5);

        var redirect = result.ShouldBeOfType<RedirectResult>();
        Sut.ModelState.ContainsKey("sponsor_org_search").ShouldBeTrue();
        Sut.ModelState["sponsor_org_search"]!.Errors.Count.ShouldBe(1);

        sessionData.ContainsKey($"{SessionKeys.Questionnaire}:{currentStage}").ShouldBeTrue();
    }
}