﻿using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class CreateReviewBodyTests : TestServiceBase<ReviewBodyService>
{
    [Theory]
    [AutoData]
    public async Task CreateReviewBody_Should_Return_Failure_Response_When_Client_Returns_Failure(
        ReviewBodyDto reviewBodyDto)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyDto>>
        (
            apiResponse =>
                !apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.BadRequest
        );

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.CreateReviewBody(reviewBodyDto))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.CreateReviewBody(reviewBodyDto);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(c => c.CreateReviewBody(reviewBodyDto), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task CreateReviewBody_Should_Return_Success_Response_When_Client_Returns_Success(
        ReviewBodyDto reviewBodyDto)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyDto>>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.CreateReviewBody(reviewBodyDto))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.CreateReviewBody(reviewBodyDto);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.CreateReviewBody(reviewBodyDto), Times.Once());
    }
}