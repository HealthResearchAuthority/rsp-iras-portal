using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class WelcomeTests : TestServiceBase<ApplicationController>
{
    private readonly Mock<ISession> session = new();

    [Fact]
    public async Task Welcome_ReturnsViewResult_WithMappedResearchApplications()
    {
        // Arrange
        var respondentId = "RespondentId1";

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };
        httpContext.Items[ContextItemKeys.UserId] = respondentId;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var mockDate = DateTime.UtcNow;

        var mockApplications = new List<IrasApplicationResponse>
        {
            new()
            {
                IrasId = 123,
                Id = "App1",
                CreatedDate = mockDate,
                Status = "Created",
                ShortProjectTitle = "My Title"
            }
        };

        var appResponse = new ServiceResponse<IEnumerable<IrasApplicationResponse>>
        {
            Content = mockApplications
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup
            (
                s => s.GetPaginatedApplicationsByRespondent
                (
                    It.IsAny<string>(),
                    It.IsAny<ApplicationSearchRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()
                )
            )
            .ReturnsAsync(new ServiceResponse<PaginatedResponse<IrasApplicationResponse>>
            {
                Content = new PaginatedResponse<IrasApplicationResponse>
                {
                    Items = mockApplications,
                    TotalCount = mockApplications.Count
                }
            });

        // Act
        var result = await Sut.Welcome();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");

        var viewModel = viewResult.Model.ShouldBeOfType<ApplicationsViewModel>();
        viewModel.Applications.Count().ShouldBe(1);

        var item = viewModel.Applications.First();
        item.IrasId.ShouldBe(123);
        item.Id.ShouldBe("App1");
        item.Title.ShouldBe("My Title");
        item.CreatedDate.ShouldBe(mockDate);
        item.Status.ShouldBe("Created");
    }

    [Fact]
    public async Task Welcome_Sets_EmptySearchPerformed_False_When_SearchTitleTerm_Present()
    {
        // Arrange
        var respondentId = "RespondentId1";
        var httpContext = new DefaultHttpContext { Session = session.Object };
        httpContext.Items[ContextItemKeys.UserId] = respondentId;
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Session contains a search with a title term
        var search = new ApplicationSearchModel { SearchTitleTerm = "cancer" };
        var json = JsonSerializer.Serialize(search);
        var bytes = Encoding.UTF8.GetBytes(json);
        session.Setup(s => s.IsAvailable).Returns(true);
        session.Setup(s => s.TryGetValue(SessionKeys.ProjectRecordSearch, out bytes)).Returns(true);

        // Applications service returns empty list
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetPaginatedApplicationsByRespondent(
                It.IsAny<string>(), It.IsAny<ApplicationSearchRequest>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<PaginatedResponse<IrasApplicationResponse>>
            {
                Content = new PaginatedResponse<IrasApplicationResponse> { Items = [], TotalCount =0 }
            });

        // Act
        var result = await Sut.Welcome();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ApplicationsViewModel>();
        model.EmptySearchPerformed.ShouldBeFalse();
    }

    [Fact]
    public async Task Welcome_Sets_EmptySearchPerformed_False_When_Filters_Present()
    {
        // Arrange
        var respondentId = "RespondentId1";
        var httpContext = new DefaultHttpContext { Session = session.Object };
        httpContext.Items[ContextItemKeys.UserId] = respondentId;
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Session contains a search with filters (status)
        var search = new ApplicationSearchModel();
        search.Status.Add("Created");
        var json = JsonSerializer.Serialize(search);
        var bytes = Encoding.UTF8.GetBytes(json);
        session.Setup(s => s.IsAvailable).Returns(true);
        session.Setup(s => s.TryGetValue(SessionKeys.ProjectRecordSearch, out bytes)).Returns(true);

        // Applications service returns empty list
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetPaginatedApplicationsByRespondent(
                It.IsAny<string>(), It.IsAny<ApplicationSearchRequest>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<PaginatedResponse<IrasApplicationResponse>>
            {
                Content = new PaginatedResponse<IrasApplicationResponse> { Items = [], TotalCount =0 }
            });

        // Act
        var result = await Sut.Welcome();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ApplicationsViewModel>();
        model.EmptySearchPerformed.ShouldBeFalse();
    }
}