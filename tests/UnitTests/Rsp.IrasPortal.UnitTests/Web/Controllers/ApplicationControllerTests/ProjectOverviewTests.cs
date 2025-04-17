using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests
{
    public class ProjectOverview : TestServiceBase<ApplicationController>
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

            var projectTitle = "Test Project";
            var categoryId = "123";
            var applicationId = "456";

            // Act
            var result = Sut.ProjectOverview(projectTitle, categoryId, applicationId);


            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProjectOverviewModel>(viewResult.Model);

            Assert.Equal(projectTitle, model.ProjectTitle);
            Assert.Equal(categoryId, model.CategoryId);
            Assert.Equal(applicationId, model.ApplicationId);

         
        }
    }
}