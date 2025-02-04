using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests
{
    public class StartNewApplicationTests : TestServiceBase<ApplicationController>
    {
        [Fact]
        public void StartNewApplication_ClearsSession_AndReturnsViewResult()
        {
            // Arrange
            var mockSession = new Mock<ISession>();

            var httpContext = new DefaultHttpContext
            {
                Session = mockSession.Object
            };

            Sut.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = Sut.StartNewApplication();

            // Assert
            var viewResult = result.ShouldBeOfType<ViewResult>();
            viewResult.ViewName.ShouldBe("ApplicationInfo");
            var model = viewResult.Model.ShouldBeOfType<ValueTuple<ApplicationInfoViewModel, string>>();
            model.Item1.ShouldBeOfType<ApplicationInfoViewModel>();
            model.Item2.ShouldBe("create");

            // Verify session is cleared
            httpContext.Session.Keys.ShouldBeEmpty();

            mockSession.Verify(s => s.Remove(SessionKeys.Application), Times.Once);
        }
    }
}