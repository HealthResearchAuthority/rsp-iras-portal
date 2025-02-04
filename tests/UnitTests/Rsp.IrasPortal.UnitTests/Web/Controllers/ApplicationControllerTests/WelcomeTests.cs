using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests
{
    public class WelcomeTests : TestServiceBase<ApplicationController>
    {
        [Fact]
        public void Welcome_ReturnsViewResult_WithIndexViewName()
        {
            // Act
            var result = Sut.Welcome();

            // Assert
            var viewResult = result.ShouldBeOfType<ViewResult>();
            viewResult.ViewName.ShouldBe("Index");
        }
    }
}