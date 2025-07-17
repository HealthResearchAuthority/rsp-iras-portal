using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class GetSpecificChangeByIdTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void GetSpecificChangesByAreaId_ReturnsSpecificChanges_WhenValidAreaOfChangeId()
    {
        // Arrange
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

        var sessionMock = new Mock<ISession>();
        sessionMock
            .Setup(s => s.Keys)
            .Returns([SessionKeys.AreaOfChanges]);
        sessionMock
            .Setup(s => s.TryGetValue(SessionKeys.AreaOfChanges, out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (key != SessionKeys.AreaOfChanges)
                {
                    value = null;
                    return false;
                }

                value = JsonSerializer.SerializeToUtf8Bytes(modificationResponse);
                return true;
            });

        var httpContext = new DefaultHttpContext
        {
            Session = sessionMock.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = Sut.GetSpecificChangesByAreaId(1);

        // Assert
        var jsonResult = result.ShouldBeOfType<JsonResult>();
        var selectList = jsonResult.Value.ShouldBeOfType<List<SelectListItem>>();

        selectList.Count.ShouldBe(3); // Includes default 'Select' option
        selectList[1].Text.ShouldBe("Specific Area 1");
        selectList[2].Text.ShouldBe("Specific Area 2");
    }

    [Fact]
    public void GetSpecificChangesByAreaId_ReturnsBadRequest_WhenSessionIsMissing()
    {
        // Arrange
        var sessionMock = new Mock<ISession>();
        sessionMock
            .Setup(s => s.Keys)
            .Returns([SessionKeys.AreaOfChanges]);

        var httpContext = new DefaultHttpContext
        {
            Session = sessionMock.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = Sut.GetSpecificChangesByAreaId(123);

        // Assert
        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe("Area of changes not available.");
    }
}