using System.Text.Json;
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

public class ParticipatingOrganisationsListControllerTests : TestServiceBase<ParticipatingOrganisationsListController>
{
    [Fact]
    public async Task ParticipatingOrganisationsList_SetsDetailsStatus_FromValidationResults()
    {
        // Arrange
        var firstOrg = Guid.NewGuid();
        var secondOrg = Guid.NewGuid();
        var organisations = new List<ParticipatingOrganisationModel>
        {
            new() { OrganisationId = firstOrg, Name = "Org A", Id = "A" },
            new() { OrganisationId = secondOrg, Name = "Org B", Id = "B" }
        };

        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SelectedParticipatingOrganisations] = JsonSerializer.Serialize(organisations)
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pom-participating-organisation-details", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = BuildQuestionSet()
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisationAnswers(firstOrg))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new ParticipatingOrganisationAnswerDto { QuestionId = "Q1", AnswerText = "value" }]
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationParticipatingOrganisationAnswers(secondOrg))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ParticipatingOrganisationAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .SetupSequence(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult())
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Q1", "Required")]));

        // Act
        var result = await Sut.ParticipatingOrganisationsList();

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<OrganisationsListViewModel>();
        model.Organisations.Count.ShouldBe(2);
        model.Organisations.Single(o => o.OrganisationId == firstOrg).DetailsStatus.ShouldBe("Complete");
        model.Organisations.Single(o => o.OrganisationId == secondOrg).DetailsStatus.ShouldBe("Incomplete");
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