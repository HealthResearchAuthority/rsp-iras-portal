using Microsoft.AspNetCore.Mvc.ViewComponents;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Components;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Components;

public class BackNavigationTests : TestServiceBase<BackNavigation>
{
    private readonly Mock<ICmsQuestionsetService> _cmsQuestionsetService;

    public BackNavigationTests()
    {
        _cmsQuestionsetService = Mocker.GetMock<ICmsQuestionsetService>();
    }

    private static NavigationDto CreateNavigationDto(QuestionSectionsResponse? previous = null) => new()
    {
        PreviousSection = previous
    };

    [Fact]
    public async Task InvokeAsync_Should_Return_AreaOfChange_When_No_Previous_Section()
    {
        // Arrange
        var navigation = CreateNavigationDto();

        // Act
        var result = await Sut.InvokeAsync(navigation, "spec-1", "proj-1");

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        view.ViewName.ShouldBe("BackNavigation");
        var model = view.ViewData.Model.ShouldBeOfType<ValueTuple<string, string, Dictionary<string, string>>>();
        model.Item1.ShouldBe("pmc:areaofchange");
        model.Item2.ShouldBe("Back");
        model.Item3.ShouldBeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_PreviousSection_Route_When_PreviousSection_Exists()
    {
        // Arrange
        var previousSection = new QuestionSectionsResponse
        {
            QuestionCategoryId = "A",
            SectionId = "SEC1",
            StaticViewName = "section-one",
            SectionName = "Section One",
            Sequence = 1,
            IsMandatory = true
        };
        var navigation = CreateNavigationDto(previousSection);

        // Act
        var result = await Sut.InvokeAsync(navigation, "spec-1", "proj-1");

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        var model = view.ViewData.Model.ShouldBeOfType<ValueTuple<string, string, Dictionary<string, string>>>();
        model.Item1.ShouldBe("pmc:section-one");
        model.Item3.ShouldContainKey("projectRecordId");
        model.Item3["sectionId"].ShouldBe("SEC1");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_Review_Page_Route_When_ReviewInProgress()
    {
        // Arrange
        var navigation = CreateNavigationDto();

        // Act
        var result = await Sut.InvokeAsync(navigation, "spec-1", "proj-1", reviewInProgress: true);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        var model = view.ViewData.Model.ShouldBeOfType<ValueTuple<string, string, Dictionary<string, string>>>();
        model.Item1.ShouldBe("pmc:reviewchanges");
        model.Item3["projectRecordId"].ShouldBe("proj-1");
    }

    [Fact]
    public async Task InvokeAsync_Should_Resolve_From_Review_Fallback_To_Last_Mandatory_Section_With_Missing_Answers()
    {
        // Arrange
        var questionnaire = new QuestionnaireViewModel
        {
            Questions = new List<QuestionViewModel>
            {
                new() { QuestionId = "Q1", SectionId = "S1", DataType = "Text" },
                new() { QuestionId = "Q2", SectionId = "S2", DataType = "Text", AnswerText = "Answered" },
                new() { QuestionId = "Q3", SectionId = "S3", DataType = "Text" }
            }
        };

        var cmsResponse = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CmsQuestionSetResponse
            {
                Sections = new List<SectionModel>
                {
                    new() { Id = "S1", CategoryId = "A", StaticViewName = "s1", Sequence = 1, IsMandatory = true },
                    new() { Id = "S2", CategoryId = "A", StaticViewName = "s2", Sequence = 2, IsMandatory = true },
                    new() { Id = "S3", CategoryId = "A", StaticViewName = "s3", Sequence = 3, IsMandatory = true }
                }
            }
        };

        _cmsQuestionsetService
            .Setup(s => s.GetModificationsJourney("spec-1"))
            .ReturnsAsync(cmsResponse);

        var navigation = CreateNavigationDto();

        // Act
        var result = await Sut.InvokeAsync(navigation, "spec-1", "proj-1", questionnaire, backFromReview: true, reviewInProgress: false);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        var model = view.ViewData.Model.ShouldBeOfType<ValueTuple<string, string, Dictionary<string, string>>>();
        // Expect route for S2 because S3 is last so loop stops at S3, but S1 has missing answers so break there first
        model.Item1.ShouldBe("pmc:s1");
    }

    [Fact]
    public async Task InvokeAsync_Should_Resolve_From_Review_Last_Section_When_All_Mandatory_Answered()
    {
        // Arrange
        var questionnaire = new QuestionnaireViewModel
        {
            Questions = new List<QuestionViewModel>
            {
                new() { QuestionId = "Q1", SectionId = "S1", DataType = "Text", AnswerText = "A" },
                new() { QuestionId = "Q2", SectionId = "S2", DataType = "Text", AnswerText = "B" }
            }
        };

        var cmsResponse = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CmsQuestionSetResponse
            {
                Sections = new List<SectionModel>
                {
                    new() { Id = "S1", CategoryId = "A", StaticViewName = "s1", Sequence = 1, IsMandatory = true },
                    new() { Id = "S2", CategoryId = "A", StaticViewName = "s2", Sequence = 2, IsMandatory = true }
                }
            }
        };

        _cmsQuestionsetService
            .Setup(s => s.GetModificationsJourney("spec-1"))
            .ReturnsAsync(cmsResponse);

        var navigation = CreateNavigationDto();

        // Act
        var result = await Sut.InvokeAsync(navigation, "spec-1", "proj-1", questionnaire, backFromReview: true, reviewInProgress: false);

        // Assert
        var view = result.ShouldBeOfType<ViewViewComponentResult>();
        var model = view.ViewData.Model.ShouldBeOfType<ValueTuple<string, string, Dictionary<string, string>>>();
        model.Item1.ShouldBe("pmc:s2");
    }
}