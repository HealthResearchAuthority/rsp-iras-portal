using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Helpers;

public class SponsorOrganisationNameHelperTests
{
    [Theory, AutoData]
    public async Task GetSponsorOrganisationNameFromQuestions_Should_Return_Name_When_AnswerText_Is_Present(string organisationId, string organisationName)
    {
        // Arrange
        var rtsServiceMock = new Mock<IRtsService>();
        var questions = new List<QuestionViewModel>
        {
            new QuestionViewModel
            {
                QuestionType = "rts:org_lookup",
                AnswerText = organisationId
            }
        };

        rtsServiceMock.Setup(s => s.GetOrganisation(organisationId))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = organisationName }
            });

        // Act
        var result = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromQuestions(rtsServiceMock.Object, questions);

        // Assert
        result.ShouldBe(organisationName);
    }

    [Theory, AutoData]
    public async Task GetSponsorOrganisationNameFromQuestions_Should_Return_Name_From_FirstAnswer_When_Flag_Is_True(string organisationId, string organisationName)
    {
        // Arrange
        var rtsServiceMock = new Mock<IRtsService>();
        var questions = new List<QuestionViewModel>
        {
            new QuestionViewModel
            {
                QuestionType = "rts:org_lookup",
                Answers = new List<AnswerViewModel>
                {
                    new AnswerViewModel { AnswerText = organisationId }
                }
            }
        };

        rtsServiceMock.Setup(s => s.GetOrganisation(organisationId))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = organisationName }
            });

        // Act
        var result = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromQuestions(rtsServiceMock.Object, questions, true);

        // Assert
        result.ShouldBe(organisationName);
    }

    [Fact]
    public async Task GetSponsorOrganisationNameFromQuestions_Should_Return_Null_When_No_Question_Found()
    {
        // Arrange
        var rtsServiceMock = new Mock<IRtsService>();
        var questions = new List<QuestionViewModel>(); // empty

        // Act
        var result = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromQuestions(rtsServiceMock.Object, questions);

        // Assert
        result.ShouldBeNull();
    }

    [Theory, AutoData]
    public async Task GetSponsorOrganisationNameFromOrganisationId_Should_Return_Name_When_Response_Is_Success(string organisationId, string organisationName)
    {
        // Arrange
        var rtsServiceMock = new Mock<IRtsService>();
        rtsServiceMock.Setup(s => s.GetOrganisation(organisationId))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = organisationName }
            });

        // Act
        var result = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromOrganisationId(rtsServiceMock.Object, organisationId);

        // Assert
        result.ShouldBe(organisationName);
    }

    [Fact]
    public async Task GetSponsorOrganisationNameFromOrganisationId_Should_Return_Null_When_Id_Is_Null()
    {
        // Arrange
        var rtsServiceMock = new Mock<IRtsService>();

        // Act
        var result = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromOrganisationId(rtsServiceMock.Object, null);

        // Assert
        result.ShouldBeNull();
    }

    [Theory, AutoData]
    public async Task GetSponsorOrganisationNameFromOrganisationId_Should_Return_Null_When_Response_Is_Not_Success(string organisationId, string organisationName)
    {
        // Arrange
        var rtsServiceMock = new Mock<IRtsService>();
        rtsServiceMock.Setup(s => s.GetOrganisation(organisationId))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.BadRequest,
            });

        // Act
        var result = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromOrganisationId(rtsServiceMock.Object, organisationId);

        // Assert
        result.ShouldBeNull();
    }
}