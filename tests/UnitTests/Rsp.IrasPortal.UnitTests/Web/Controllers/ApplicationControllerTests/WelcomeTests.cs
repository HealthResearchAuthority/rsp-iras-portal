using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests
{
    public class WelcomeTests : TestServiceBase<ApplicationController>
    {
        private readonly Mock<ISession> session = new();

        [Fact]
        public async Task Welcome_ReturnsViewResult_WithIndexViewName()
        {
            var httpContext = new DefaultHttpContext
            {
                Session = session.Object
            };

            httpContext.Items[ContextItemKeys.RespondentId] = "RespondentId1";

            Sut.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await Sut.Welcome();

            // Assert
            var viewResult = result.ShouldBeOfType<ViewResult>();
            viewResult.ViewName.ShouldBe("Index");
        }
    }
}