using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class IndexTests : TestServiceBase<ModificationsTasklistController>
{
    [Fact]
    public async Task Welcome_ReturnsViewResult_WithIndexViewName()
    {
        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }
}