using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SystemAdminControllerTests;

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