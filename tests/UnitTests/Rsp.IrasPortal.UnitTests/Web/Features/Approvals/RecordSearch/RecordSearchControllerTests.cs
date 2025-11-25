using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Features.Approvals.RecordSearch.Controllers;
using Rsp.IrasPortal.Web.Features.Approvals.RecordSearch.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Approvals.RecordSearch;

public class RecordSearchControllerTests : TestServiceBase<RecordSearchController>
{
    [Fact]
    public void Index_Returns_View()
    {
        // arrange

        // act
        var result = Sut.Index();

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBeNull();
    }

    [Fact]
    public async Task ProjectRecord_Selection_Redirects_To_ProjectRecord_Search()
    {
        // arrange
        var viewModel = new RecordSearchNavigationModel
        {
            RecordType = SearchRecordTypes.ProjectRecord
        };

        Mocker
            .GetMock<IValidator<RecordSearchNavigationModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<RecordSearchNavigationModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // act
        var result = await Sut.Navigate(viewModel);

        // assert
        var viewResult = result.ShouldBeOfType<RedirectToRouteResult>();
        viewResult.RouteName.ShouldBe("projectrecordsearch");
    }

    [Fact]
    public async Task ModificationRecord_Selection_Redirects_To_ProjectRecord_Search()
    {
        // arrange
        var viewModel = new RecordSearchNavigationModel
        {
            RecordType = SearchRecordTypes.ModificationRecord
        };
        Mocker
            .GetMock<IValidator<RecordSearchNavigationModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<RecordSearchNavigationModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // act
        var result = await Sut.Navigate(viewModel);

        // assert
        var viewResult = result.ShouldBeOfType<RedirectToRouteResult>();
        viewResult.RouteName.ShouldBe("approvals:index");
    }

    [Fact]
    public async Task Invalid_SearchRecord_Selection_Redirects_To_ProjectRecord_Search()
    {
        // arrange
        var viewModel = new RecordSearchNavigationModel
        {
            RecordType = string.Empty
        };
        Mocker
            .GetMock<IValidator<RecordSearchNavigationModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<RecordSearchNavigationModel>>(), default))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
            {
                new("RecordType", "Required"),
            }));

        // act
        var result = await Sut.Navigate(viewModel);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");
    }
}