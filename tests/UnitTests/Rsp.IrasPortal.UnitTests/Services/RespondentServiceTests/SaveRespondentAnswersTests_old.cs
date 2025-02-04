using System.Net;
using AutoFixture.Xunit2;
using Moq;
using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Shouldly;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class SaveRespondentAnswersTests : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task SaveRespondentAnswers_ShouldReturnServiceResponse_WithValidAnswers(RespondentAnswersRequest request)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);

        Mocker
            .GetMock<IRespondentServiceClient>()
            .Setup(client => client.SaveRespondentAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveRespondentAnswers(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task SaveRespondentAnswers_ShouldReturnError_WhenAnswersAreNotValid(RespondentAnswersRequest request)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.BadRequest);

        Mocker
            .GetMock<IRespondentServiceClient>()
            .Setup(client => client.SaveRespondentAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveRespondentAnswers(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}