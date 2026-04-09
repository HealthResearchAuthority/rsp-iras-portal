using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Components;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ParticipatingOrganisations;

public class ParticipatingOrganisationsReviewTests : TestServiceBase<ParticipatingOrganisationsReview>
{
    [Fact]
    public async Task InvokeAsync_ForAddNewSites_ReturnsOrganisationsWithQuestions()
    {
        // Arrange
        var projectRecordId = "PR-1";
        var modificationChangeId = Guid.NewGuid();
        var participatingOrganisationId = Guid.NewGuid();

        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?reviseChange=true");
        var tempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        Sut.ViewComponentContext = new ViewComponentContext
        {
            ViewContext = new()
            {
                HttpContext = http,
                TempData = tempData
            }
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisations(modificationChangeId, projectRecordId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new ParticipatingOrganisationDto
                    {
                        Id = participatingOrganisationId,
                        OrganisationId = "ORG-1",
                        ProjectRecordId = projectRecordId,
                        ProjectModificationChangeId = modificationChangeId
                    }
                ]
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pom-participating-organisation-details", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet()
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisationAnswers(participatingOrganisationId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new ParticipatingOrganisationAnswerDto { QuestionId = "Q1", AnswerText = "answer" }]
            });

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation("ORG-1"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Id = "ORG-1", Name = "Org 1", Address = "Address", CountryName = "UK", Type = "NHS" }
            });

        // Act
        var result = await Sut.InvokeAsync(projectRecordId, modificationChangeId.ToString(), SpecificAreasOfChange.AddNewSites, showLinks: false);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        var model = view.ViewData.Model.ShouldBeOfType<List<OrganisationDetailsViewModel>>();
        model.Count.ShouldBe(1);
        model[0].OrganisationName.ShouldBe("Org 1");
        model[0].Questions.Count.ShouldBe(1);
        model[0].Questions[0].AnswerText.ShouldBe("answer");
        view.ViewData[ViewDataKeys.ShowParticipatingOrgsLinks].ShouldBe(false);
    }

    [Fact]
    public async Task InvokeAsync_ForNonSiteArea_ReturnsOrganisationsWithoutQuestionMapping()
    {
        // Arrange
        var projectRecordId = "PR-2";
        var modificationChangeId = Guid.NewGuid();

        var http = new DefaultHttpContext();
        Sut.ViewComponentContext = new ViewComponentContext
        {
            ViewContext = new()
            {
                HttpContext = http,
                TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
            }
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisations(modificationChangeId, projectRecordId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new ParticipatingOrganisationDto { Id = Guid.NewGuid(), OrganisationId = "ORG-2" }]
            });

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation("ORG-2"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Id = "ORG-2", Name = "Org 2", Address = "Address", CountryName = "UK", Type = "Other" }
            });

        // Act
        var result = await Sut.InvokeAsync(projectRecordId, modificationChangeId.ToString(), specificAreaOfChangeId: "other-area");

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        var model = view.ViewData.Model.ShouldBeOfType<List<OrganisationDetailsViewModel>>();
        model.Count.ShouldBe(1);
        model[0].OrganisationName.ShouldBe("Org 2");
        model[0].Questions.ShouldBeEmpty();
    }

    private static CmsQuestionSetResponse BuildQuestionSet()
    {
        return new CmsQuestionSetResponse
        {
            Sections =
            [
                new SectionModel
                {
                    Id = "SEC1",
                    SectionId = "SEC1",
                    CategoryId = "CAT1",
                    Questions =
                    [
                        new QuestionModel
                        {
                            Id = "Q1",
                            Name = "Question 1",
                            CategoryId = "CAT1",
                            Conformance = "Mandatory",
                            AnswerDataType = "Text",
                            QuestionFormat = "Text"
                        }
                    ]
                }
            ]
        };
    }
}