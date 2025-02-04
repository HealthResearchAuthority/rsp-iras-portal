using System.Net;
using AutoFixture.Xunit2;
using Moq;
using Refit;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Shouldly;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class RespondentServiceTests : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task SaveRespondentAnswers_Should_Return_Success_Response_When_Client_Returns_Success(RespondentAnswersRequest request)
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
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task SaveRespondentAnswers_Should_Return_Failure_Response_When_Client_Returns_Failure(RespondentAnswersRequest request)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.InternalServerError);

        Mocker
            .GetMock<IRespondentServiceClient>()
            .Setup(client => client.SaveRespondentAnswers(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveRespondentAnswers(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }
}
