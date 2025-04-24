using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests
{
    public class ProjectOverview : TestServiceBase<ApplicationController>
    {
        [Fact]
        public void StartNewApplication_UsesTempData_AndReturnsViewResult()
        {
            // Arrange
            var tempDataProvider = new Mock<ITempDataProvider>();
            var tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object)
            {
                [TempDataKeys.ShortProjectTitle] = "Test Project",
                [TempDataKeys.CategoryId] = "123",
                [TempDataKeys.ApplicationId] = "456"
            };

            Sut.TempData = tempData;

            // Act
            var result = Sut.ProjectOverview();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProjectOverviewModel>(viewResult.Model);

            model.ProjectTitle.ShouldBe("Test Project");
            model.CategoryId.ShouldBe("123");
            model.ApplicationId.ShouldBe("456");
        }
    }
}