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

    // Mock of audit trail records - having just Description property - that is required for tests
    public class AuditRecord
    {
        public string? Description { get; set; }
    }

    [Fact]
    public async Task Should_Ignore_Record_Without_Description_Property()
    {
        // Arrange
        var rtsMock = new Mock<IRtsService>();
        var records = new List<object>
        {
            new { SomethingElse = "no description" } // brak Description
        };

        // Act
        await SponsorOrganisationNameHelper
            .GetSponsorOrganisationsNameForAuditRecords(rtsMock.Object, records);

        // Assert – brak wyjątków, brak interakcji
        rtsMock.Verify(s => s.GetOrganisation(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Ignore_When_Description_Is_Null_Or_Empty()
    {
        // Arrange
        var rtsMock = new Mock<IRtsService>();
        var records = new List<object>
        {
            new AuditRecord { Description = null },
            new AuditRecord { Description = "" },
            new AuditRecord { Description = "   " }
        };

        // Act
        await SponsorOrganisationNameHelper
            .GetSponsorOrganisationsNameForAuditRecords(rtsMock.Object, records);

        // Assert
        rtsMock.Verify(s => s.GetOrganisation(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Ignore_When_Regex_Does_Not_Match()
    {
        // Arrange
        var rtsMock = new Mock<IRtsService>();
        var records = new List<object>
        {
            new AuditRecord { Description = "Random unrelated text" }
        };

        // Act
        await SponsorOrganisationNameHelper
            .GetSponsorOrganisationsNameForAuditRecords(rtsMock.Object, records);

        // Assert
        rtsMock.Verify(s => s.GetOrganisation(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Replace_Both_Ids_With_Names_When_Found()
    {
        // Arrange
        var rtsMock = new Mock<IRtsService>();

        rtsMock.Setup(s => s.GetOrganisation("111"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "OrgA" }
            });

        rtsMock.Setup(s => s.GetOrganisation("222"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "OrgB" }
            });

        var record = new AuditRecord
        {
            Description = "Primary sponsor organisation changed from '111' to '222'"
        };

        // Act
        await SponsorOrganisationNameHelper
            .GetSponsorOrganisationsNameForAuditRecords(rtsMock.Object, new[] { record });

        // Assert
        record.Description.ShouldBe(
            "Primary sponsor organisation changed from 'OrgA' to 'OrgB'");
    }

    [Fact]
    public async Task Should_Replace_Only_First_Id_When_Second_Not_Found()
    {
        // Arrange
        var rtsMock = new Mock<IRtsService>();

        rtsMock.Setup(s => s.GetOrganisation("111"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "OrgA" }
            });

        rtsMock.Setup(s => s.GetOrganisation("222"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        var record = new AuditRecord
        {
            Description = "Primary sponsor organisation changed from '111' to '222'"
        };

        // Act
        await SponsorOrganisationNameHelper
            .GetSponsorOrganisationsNameForAuditRecords(rtsMock.Object, new[] { record });

        // Assert
        record.Description.ShouldBe(
            "Primary sponsor organisation changed from 'OrgA' to '222'");
    }

    [Fact]
    public async Task Should_Handle_Multiple_Records()
    {
        // Arrange
        var rtsMock = new Mock<IRtsService>();

        rtsMock.Setup(s => s.GetOrganisation("111"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "OrgA" }
            });

        rtsMock.Setup(s => s.GetOrganisation("333"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "OrgC" }
            });

        var records = new[]
        {
            new AuditRecord { Description = "Primary sponsor organisation changed from '111' to '222'" },
            new AuditRecord { Description = "Primary sponsor organisation changed from '333' to '444'" }
        };

        // Act
        await SponsorOrganisationNameHelper
            .GetSponsorOrganisationsNameForAuditRecords(rtsMock.Object, records);

        // Assert
        records[0].Description.ShouldBe(
            "Primary sponsor organisation changed from 'OrgA' to '222'");

        records[1].Description.ShouldBe(
            "Primary sponsor organisation changed from 'OrgC' to '444'");
    }
}