using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class AreaOfChangeTests : TestServiceBase<ProjectModificationController>
{
    [Theory, AutoData]
    public async Task AreaOfChange_RedirectsToAreaOfChange_WhenSuccessful
    (
        string projectRecordId,
        int irasId
    )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        tempData[TempDataKeys.IrasId] = irasId;

        // Mock GetRespondentFromContext extension
        var respondent = new { GivenName = "John", FamilyName = "Doe" };

        var modificationResponse = new List<GetAreaOfChangesResponse>
        {
            new GetAreaOfChangesResponse
            {
                Id = 1,
                Name = "Test Area of Change",
                ModificationSpecificAreaOfChanges = new List<ModificationSpecificAreaOfChangeDto>
                {
                    new ModificationSpecificAreaOfChangeDto
                    {
                        Id = 1,
                        Name = "Specific Area 1",
                        JourneyType = "specific area 1",
                        ModificationAreaOfChangeId = 1
                    },
                    new ModificationSpecificAreaOfChangeDto
                    {
                        Id = 2,
                        Name = "Specific Area 2",
                        JourneyType = "specific area 2",
                        ModificationAreaOfChangeId = 1
                    }
                }
            }
        };

        tempData[TempDataKeys.AreaOfChanges] = JsonSerializer.Serialize(modificationResponse);

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { ["Respondent"] = respondent }
            }
        };

        var serviceResponse = new ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetAreaOfChanges())
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.AreaOfChange();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
    }
}