using EmptyFiles;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests
{
    public class StartNewRecordTests : TestServiceBase<ApplicationController>
    {
        [Fact]
        public async Task StartNewRecord_ClearsSession_AndRedirectToActionAsync()
        {
            // Arrange
            var mockSession = new Mock<ISession>();

            var httpContext = new DefaultHttpContext
            {
                Session = mockSession.Object
            };

            var createdApplication = new IrasApplicationResponse
            {
                ApplicationId = "test-app-id",
                Title = "Name",
                Description = "Description"
            };

            Mocker
                .GetMock<IApplicationsService>()
                .Setup(s => s.CreateApplication(It.IsAny<IrasApplicationRequest>()))
                .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { Content = createdApplication, StatusCode = HttpStatusCode.Created });
            
            Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            Sut.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var questionCategories = new ApiResponse<IEnumerable<CategoryDto>>
            (
                new HttpResponseMessage(HttpStatusCode.OK),
                new List<CategoryDto>
                {
                    new()
                    {
                        CategoryId = "A",
                        CategoryName = "Test"
                    }
                },
                new RefitSettings()
            );

            Mocker.GetMock<IQuestionSetService>()
                .Setup(q => q.GetQuestionCategories()).ReturnsAsync(questionCategories.ToServiceResponse());

            // Act
            var result = await Sut.StartProjectRecord();

            // Assert
            var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
            redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));
        }
    }
}