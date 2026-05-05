using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Features.MemberManagement.Controllers;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.UnitTests;

namespace Rsp.IrasPortal.UnitTests.Web.Features.MemberManagement.RecMemberManagementControllerTests;

public class RecMemberAuditHistoryTests : TestServiceBase<RecMemberManagementController>
{
    public RecMemberAuditHistoryTests() : base()
    {
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Theory, AutoData]
    public async Task RecMemberAuditHistory_Should_Return_View_When_Service_Success
    (
        Guid recId,
        Guid userId,
        ReviewBodyDto reviewBody,
        UserResponse userResponse,
        ReviewBodyAuditTrailResponse auditTrailResponse

    )
    {
        // Arrange
        var reviewBodyService = Mocker.GetMock<IReviewBodyService>();
        reviewBodyService
            .Setup(rb => rb.GetReviewBodyById(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = reviewBody
            });

        auditTrailResponse.TotalCount = auditTrailResponse.Items.Count();

        reviewBodyService
            .Setup(rb => rb.ReviewBodyUserAuditTrail
            (
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(new ServiceResponse<ReviewBodyAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = auditTrailResponse
            });

        var userService = Mocker.GetMock<IUserManagementService>();
        userService
            .Setup(u => u.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = userResponse
            });

        // Act
        var result = await Sut.RecMemberAuditHistory(recId, userId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<RecMemberAuditHistoryViewModel>();
        model.ReviewBody.ShouldBe(reviewBody);
        model.AuditHistoryEntries.Count.ShouldBe(auditTrailResponse.TotalCount);
    }

    [Theory, AutoData]
    public async Task RecMemberAuditHistory_Should_Return_NotFound_When_GetReviewBodyById_Fails
    (
        Guid recId,
        Guid userId
    )
    {
        // Arrange
        var reviewBodyService = Mocker.GetMock<IReviewBodyService>();
        reviewBodyService
            .Setup(rb => rb.GetReviewBodyById(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            });

        // Act
        var result = await Sut.RecMemberAuditHistory(recId, userId);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }

    [Theory, AutoData]
    public async Task RecMemberAuditHistory_Should_Return_NotFound_When_ReviewBodyUserAuditTrail_Fails
    (
        Guid recId,
        Guid userId,
        ReviewBodyDto reviewBody,
        UserResponse userResponse
    )
    {
        // Arrange
        var reviewBodyService = Mocker.GetMock<IReviewBodyService>();
        reviewBodyService
            .Setup(rb => rb.GetReviewBodyById(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = reviewBody
            });

        reviewBodyService
            .Setup(rb => rb.ReviewBodyUserAuditTrail(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(new ServiceResponse<ReviewBodyAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            });

        var userService = Mocker.GetMock<IUserManagementService>();
        userService
            .Setup(u => u.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = userResponse
            });

        // Act
        var result = await Sut.RecMemberAuditHistory(recId, userId);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }

    [Theory, AutoData]
    public async Task RecMemberAuditHistory_Should_Return_NotFound_When_GetUser_Fails
    (
        Guid recId,
        Guid userId,
        ReviewBodyDto reviewBody,
        ReviewBodyAuditTrailResponse auditTrailResponse
    )
    {
        // Arrange
        var reviewBodyService = Mocker.GetMock<IReviewBodyService>();
        reviewBodyService
            .Setup(rb => rb.GetReviewBodyById(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = reviewBody
            });

        auditTrailResponse.TotalCount = auditTrailResponse.Items.Count();

        reviewBodyService
            .Setup(rb => rb.ReviewBodyUserAuditTrail(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(new ServiceResponse<ReviewBodyAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = auditTrailResponse
            });

        var userService = Mocker.GetMock<IUserManagementService>();
        userService
            .Setup(u => u.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            });

        // Act
        var result = await Sut.RecMemberAuditHistory(recId, userId);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }
}