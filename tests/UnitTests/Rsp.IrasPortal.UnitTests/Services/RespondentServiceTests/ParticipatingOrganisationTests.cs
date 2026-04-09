using Rsp.IrasPortal.Application.DTOs;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.RespondentServiceTests;

public class ParticipatingOrganisationTests : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task GetModificationParticipatingOrganisations_DelegatesToClient_AndReturnsMappedResult(
        Guid modificationChangeId,
        string projectRecordId,
        List<ParticipatingOrganisationDto> apiResponseContent)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<ParticipatingOrganisationDto>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseContent,
            new(),
            null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationParticipatingOrganisations(modificationChangeId, projectRecordId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationParticipatingOrganisations(modificationChangeId, projectRecordId);

        // Assert
        respondentServiceClient.Verify(c => c.GetModificationParticipatingOrganisations(modificationChangeId, projectRecordId), Times.Once);
        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task GetModificationParticipatingOrganisationsBySpecificArea_DelegatesToClient_AndReturnsMappedResult(
        string projectRecordId,
        string specificArea,
        List<ParticipatingOrganisationDto> apiResponseContent)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<ParticipatingOrganisationDto>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseContent,
            new(),
            null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationParticipatingOrganisationsBySpecificArea(projectRecordId, specificArea))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationParticipatingOrganisationsBySpecificArea(projectRecordId, specificArea);

        // Assert
        respondentServiceClient.Verify(c => c.GetModificationParticipatingOrganisationsBySpecificArea(projectRecordId, specificArea), Times.Once);
        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task GetModificationParticipatingOrganisationAnswers_DelegatesToClient_AndReturnsMappedResult(
        Guid participatingOrganisationId,
        List<ParticipatingOrganisationAnswerDto> apiResponseContent)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<ParticipatingOrganisationAnswerDto>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            apiResponseContent,
            new(),
            null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationParticipatingOrganisationAnswers(participatingOrganisationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationParticipatingOrganisationAnswers(participatingOrganisationId);

        // Assert
        respondentServiceClient.Verify(c => c.GetModificationParticipatingOrganisationAnswers(participatingOrganisationId), Times.Once);
        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task SaveModificationParticipatingOrganisations_DelegatesToClient_AndReturnsMappedResult(
        List<ParticipatingOrganisationDto> request)
    {
        // Arrange
        var apiResponse = new ApiResponse<object>(
            new HttpResponseMessage(HttpStatusCode.OK),
            null,
            new(),
            null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.SaveModificationParticipatingOrganisations(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveModificationParticipatingOrganisations(request);

        // Assert
        respondentServiceClient.Verify(c => c.SaveModificationParticipatingOrganisations(request), Times.Once);
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task SaveModificationParticipatingOrganisationAnswers_DelegatesToClient_AndReturnsMappedResult(
        List<ParticipatingOrganisationAnswerDto> request)
    {
        // Arrange
        var apiResponse = new ApiResponse<object>(
            new HttpResponseMessage(HttpStatusCode.OK),
            null,
            new(),
            null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.SaveModificationParticipatingOrganisationAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveModificationParticipatingOrganisationAnswers(request);

        // Assert
        respondentServiceClient.Verify(c => c.SaveModificationParticipatingOrganisationAnswers(request), Times.Once);
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task DeleteModificationParticipatingOrganisation_DelegatesToClient_AndReturnsMappedResult(Guid participatingOrganisationId)
    {
        // Arrange
        var apiResponse = new ApiResponse<object>(
            new HttpResponseMessage(HttpStatusCode.OK),
            null,
            new(),
            null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.DeleteModificationParticipatingOrganisation(participatingOrganisationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteModificationParticipatingOrganisation(participatingOrganisationId);

        // Assert
        respondentServiceClient.Verify(c => c.DeleteModificationParticipatingOrganisation(participatingOrganisationId), Times.Once);
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}