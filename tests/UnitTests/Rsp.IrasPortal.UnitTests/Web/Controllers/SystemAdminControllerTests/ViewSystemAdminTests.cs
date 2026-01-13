using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Controllers;

namespace Rsp.Portal.UnitTests.Web.Controllers.SystemAdminControllerTests;

public class SystemAdminTests : TestServiceBase<SystemAdminController>

{
    [Fact]
    public void SystemAdmin_WithNoModel_ShouldReturnSystemAdminView()
    {
        // Act
        var result = Sut.Index();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");
    }
}