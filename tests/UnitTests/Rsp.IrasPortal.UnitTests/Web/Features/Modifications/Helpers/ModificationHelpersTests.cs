using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Features.Modifications.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Helpers;

public class ModificationHelpersTests
{
    [Fact]
    public void UpdateWithAnswers_Should_Set_SelectedOption_And_AnswerText_For_SingleChoice()
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new()
            {
                QuestionId = "Q1",
                DataType = "Radio button",
                Answers =
                [
                    new() { AnswerId = "A", AnswerText = "Option A" },
                    new() { AnswerId = "B", AnswerText = "Option B" }
                ]
            }
        };

        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new()
            {
                QuestionId = "Q1",
                SelectedOption = "A",
                OptionType = "Single",
                AnswerText = "Free text value"
            }
        };

        // Act
        ModificationHelpers.UpdateWithAnswers(respondentAnswers, questions);

        // Assert
        var q1 = questions.Single(q => q.QuestionId == "Q1");
        q1.SelectedOption.ShouldBe("A");
        q1.AnswerText.ShouldBe("Free text value");
        // For single choice path IsSelected flags should remain false (helper only toggles for Multiple)
        q1.Answers.All(a => !a.IsSelected).ShouldBeTrue();
    }

    [Fact]
    public void UpdateWithAnswers_Should_Set_IsSelected_For_MultipleChoice_Answers()
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new()
            {
                QuestionId = "Q2",
                DataType = "Checkbox",
                Answers =
                [
                    new() { AnswerId = "A", AnswerText = "Opt A" },
                    new() { AnswerId = "B", AnswerText = "Opt B" },
                    new() { AnswerId = "C", AnswerText = "Opt C" }
                ]
            }
        };

        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new()
            {
                QuestionId = "Q2",
                OptionType = "Multiple",
                Answers = ["A", "C"]
            }
        };

        // Act
        ModificationHelpers.UpdateWithAnswers(respondentAnswers, questions);

        // Assert
        var q2 = questions.Single(q => q.QuestionId == "Q2");
        q2.Answers.Single(a => a.AnswerId == "A").IsSelected.ShouldBeTrue();
        q2.Answers.Single(a => a.AnswerId == "C").IsSelected.ShouldBeTrue();
        q2.Answers.Single(a => a.AnswerId == "B").IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void UpdateWithAnswers_Should_Ignore_RespondentAnswer_When_Question_Not_Found()
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new() { QuestionId = "Q3", DataType = "Text" }
        };

        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = "Q-UNKNOWN", SelectedOption = "X", OptionType = "Single", AnswerText = "Should be ignored" }
        };

        // Act (should not throw)
        ModificationHelpers.UpdateWithAnswers(respondentAnswers, questions);

        // Assert - existing question untouched
        var q3 = questions.Single(q => q.QuestionId == "Q3");
        q3.SelectedOption.ShouldBeNull();
        q3.AnswerText.ShouldBeNull();
    }

    [Fact]
    public void UpdateWithAnswers_Should_Handle_Mixture_Of_Single_And_Multiple()
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new()
            {
                QuestionId = "Q4",
                DataType = "Checkbox",
                Answers =
                [
                    new() { AnswerId = "1", AnswerText = "One" },
                    new() { AnswerId = "2", AnswerText = "Two" }
                ]
            },
            new()
            {
                QuestionId = "Q5",
                DataType = "Radio button",
                Answers =
                [
                    new() { AnswerId = "Y", AnswerText = "Yes" },
                    new() { AnswerId = "N", AnswerText = "No" }
                ]
            }
        };

        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new()
            {
                QuestionId = "Q4",
                OptionType = "Multiple",
                Answers = ["2"]
            },
            new()
            {
                QuestionId = "Q5",
                OptionType = "Single",
                SelectedOption = "Y",
                AnswerText = "Yes"
            }
        };

        // Act
        ModificationHelpers.UpdateWithAnswers(respondentAnswers, questions);

        // Assert
        var q4 = questions.Single(q => q.QuestionId == "Q4");
        q4.Answers.Single(a => a.AnswerId == "2").IsSelected.ShouldBeTrue();
        q4.Answers.Single(a => a.AnswerId == "1").IsSelected.ShouldBeFalse();

        var q5 = questions.Single(q => q.QuestionId == "Q5");
        q5.SelectedOption.ShouldBe("Y");
        q5.AnswerText.ShouldBe("Yes");
    }
}