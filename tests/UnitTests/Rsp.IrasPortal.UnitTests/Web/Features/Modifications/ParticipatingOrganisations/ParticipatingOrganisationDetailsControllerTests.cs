using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ParticipatingOrganisations;

public class ParticipatingOrganisationDetailsControllerTests : TestServiceBase<ParticipatingOrganisationDetailsController>
{
    [Fact]
    public async Task ParticipatingOrganisationDetails_ReturnsView_WithMappedAnswers()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PR-1",
            [TempDataKeys.ShortProjectTitle] = "Study",
            [TempDataKeys.IrasId] = "IRAS-1",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-1",
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid()
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pom-participating-organisation-details", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet()
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisationAnswers(organisationId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new ParticipatingOrganisationAnswerDto
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = "Q1",
                        SelectedOption = "A1",
                        AnswerText = "answer"
                    }
                ]
            });

        // Act
        var result = await Sut.ParticipatingOrganisationDetails(organisationId, reviewAnswers: true, reviewAllChanges: true);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<OrganisationDetailsViewModel>();
        model.ReviewAnswers.ShouldBeTrue();
        model.ReviewAllChanges.ShouldBeTrue();
        model.Questions.Count.ShouldBe(1);
        model.Questions[0].AnswerText.ShouldBe("answer");
        model.Questions[0].Answers.Single().IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveOrganisationDetails_ReturnsView_WhenValidationFails()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pom-participating-organisation-details", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet()
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Q1", "Required")]));

        var model = new OrganisationDetailsViewModel
        {
            OrganisationId = Guid.NewGuid().ToString(),
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1" }]
        };

        // Act
        var result = await Sut.SaveOrganisationDetails(model);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe(nameof(ParticipatingOrganisationDetailsController.ParticipatingOrganisationDetails));

        Mocker.GetMock<IRespondentService>()
            .Verify(s => s.SaveModificationParticipatingOrganisationAnswers(It.IsAny<List<ParticipatingOrganisationAnswerDto>>()), Times.Never);
    }

    [Fact]
    public async Task SaveOrganisationDetailsForLater_WhenReviseAndAuthorise_RedirectsToSponsorWorkspace()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };

        var sponsorOrganisationUserId = Guid.NewGuid();

        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationStatus] = ModificationStatus.ReviseAndAuthorise,
            [TempDataKeys.RevisionSponsorOrganisationUserId] = sponsorOrganisationUserId,
            [TempDataKeys.RevisionRtsId] = "RTS-1"
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pom-participating-organisation-details", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet()
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationParticipatingOrganisationAnswers(It.IsAny<List<ParticipatingOrganisationAnswerDto>>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var model = new OrganisationDetailsViewModel
        {
            OrganisationId = Guid.NewGuid().ToString(),
            Questions = [new QuestionViewModel { Index = 0, QuestionId = "Q1", AnswerText = "value" }]
        };

        // Act
        var result = await Sut.SaveOrganisationDetailsForLater(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("sws:modifications");
        redirect.RouteValues!["sponsorOrganisationUserId"].ShouldBe(sponsorOrganisationUserId.ToString());
        redirect.RouteValues["rtsId"].ShouldBe("RTS-1");
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
                            QuestionFormat = "Radio button",
                            Answers =
                            [
                                new AnswerModel { Id = "A1", OptionName = "Option A" }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}