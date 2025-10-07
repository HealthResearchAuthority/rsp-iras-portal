using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Features.Modifications.Helpers;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Helpers;

public class ModificationHelpersTests
{
    [Fact]
    public void UpdateWithAnswers_Should_Set_SelectedOption_And_AnswerText_For_SingleChoice()
    {
        // Arrange
        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
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
            ]
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
        questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

        // Assert
        var q1 = questionnaire.Questions.Single(q => q.QuestionId == "Q1");
        q1.SelectedOption.ShouldBe("A");
        q1.AnswerText.ShouldBe("Free text value");
        // For single choice path IsSelected flags should remain false (helper only toggles for Multiple)
        q1.Answers.All(a => !a.IsSelected).ShouldBeTrue();
    }

    [Fact]
    public void UpdateWithAnswers_Should_Set_IsSelected_For_MultipleChoice_Answers()
    {
        // Arrange
        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
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
            ]
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
        questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

        // Assert
        var q2 = questionnaire.Questions.Single(q => q.QuestionId == "Q2");
        q2.Answers.Single(a => a.AnswerId == "A").IsSelected.ShouldBeTrue();
        q2.Answers.Single(a => a.AnswerId == "C").IsSelected.ShouldBeTrue();
        q2.Answers.Single(a => a.AnswerId == "B").IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void UpdateWithAnswers_Should_Ignore_RespondentAnswer_When_Question_Not_Found()
    {
        // Arrange
        var questionnaire = new QuestionnaireViewModel
        {
            Questions = [new() { QuestionId = "Q3", DataType = "Text" }]
        };

        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = "Q-UNKNOWN", SelectedOption = "X", OptionType = "Single", AnswerText = "Should be ignored" }
        };

        // Act (should not throw)
        questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

        // Assert - existing question untouched
        var q3 = questionnaire.Questions.Single(q => q.QuestionId == "Q3");
        q3.SelectedOption.ShouldBeNull();
        q3.AnswerText.ShouldBeNull();
    }

    [Fact]
    public void UpdateWithAnswers_Should_Handle_Mixture_Of_Single_And_Multiple()
    {
        // Arrange

        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
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
            ]
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
        questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

        // Assert
        var q4 = questionnaire.Questions.Single(q => q.QuestionId == "Q4");
        q4.Answers.Single(a => a.AnswerId == "2").IsSelected.ShouldBeTrue();
        q4.Answers.Single(a => a.AnswerId == "1").IsSelected.ShouldBeFalse();

        var q5 = questionnaire.Questions.Single(q => q.QuestionId == "Q5");
        q5.SelectedOption.ShouldBe("Y");
        q5.AnswerText.ShouldBe("Yes");
    }

    [Fact]
    public void ApplyRespondentAnswers_Updates_Questionnaire_WithAnswers()
    {
        // Arrange
        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
                new() { QuestionId = "S1", Category = "C1", ShowAnswerOn = "ReviewAllChanges", IsMandatory = false, IsOptional = false }, // surfacing
                new() { QuestionId = "Q2", Category = "C1", IsMandatory = false, IsOptional = false, AnswerText = null, Answers = [] }, // conditional unanswered
                new() { QuestionId = "Q3", Category = "C1", IsMandatory = true, IsOptional = false }, // mandatory stays
                new() { QuestionId = "Q4", Category = "C1", IsMandatory = false, IsOptional = false } // will get answer
            ]
        };

        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = "Q4", CategoryId = "C1", AnswerText = "Answered" }
        };

        // Act
        questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

        // Q4 should remain and have the answer applied
        var q4 = questionnaire.Questions.Single(q => q.QuestionId == "Q4");
        q4.AnswerText.ShouldBe("Answered");
    }

    [Fact]
    public void ShowSurfacingQuestion_Should_Remove_Surfacing_Question_And_Set_SpecificChangeAnswer_When_ActionName_Matches()
    {
        // Arrange
        var surfacingQuestion = new QuestionViewModel
        {
            QuestionId = "S1",
            ShowAnswerOn = "ReviewAllChanges",
            AnswerText = "Surfacing answer",
            DataType = "Text", // Ensure required property is set
            QuestionText = "Surfacing question?",
            Answers = []
        };
        var questions = new List<QuestionViewModel>
        {
            surfacingQuestion,
            new() { QuestionId = "Q2", DataType = "Text", QuestionText = "Q2?", Answers = [] }
        };
        var modificationChange = new ModificationChangeModel();

        // Act
        ModificationHelpers.ShowSurfacingQuestion(questions, modificationChange, "ReviewAllChanges");

        // Assert
        questions.Any(q => q.QuestionId == "S1").ShouldBeFalse();
        modificationChange.SpecificChangeAnswer.ShouldBe("Surfacing answer");
    }

    [Fact]
    public void ShowSurfacingQuestion_Should_Not_Remove_Or_Set_When_ActionName_Does_Not_Match()
    {
        // Arrange
        var surfacingQuestion = new QuestionViewModel
        {
            QuestionId = "S1",
            ShowAnswerOn = "ReviewAllChanges",
            AnswerText = "Surfacing answer"
        };
        var questions = new List<QuestionViewModel>
        {
            surfacingQuestion,
            new() { QuestionId = "Q2" }
        };
        var modificationChange = new ModificationChangeModel();

        // Act
        ModificationHelpers.ShowSurfacingQuestion(questions, modificationChange, "OtherAction");

        // Assert
        questions.Any(q => q.QuestionId == "S1").ShouldBeTrue();
        modificationChange.SpecificChangeAnswer.ShouldBeNull();
    }

    [Fact]
    public void ShowSurfacingQuestion_Should_Do_Nothing_When_No_Surfacing_Question()
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new() { QuestionId = "Q1" }
        };
        var modificationChange = new ModificationChangeModel();

        // Act
        ModificationHelpers.ShowSurfacingQuestion(questions, modificationChange, "ReviewAllChanges");

        // Assert
        questions.Count.ShouldBe(1);
        modificationChange.SpecificChangeAnswer.ShouldBeNull();
    }

    [Fact]
    public void GetRankingOfChangeRequest_Should_Map_All_Fields_Correctly_When_All_Questions_Present_And_Selected()
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new()
            {
                QuestionId = "Q1",
                NhsInvolvment = "NHS",
                Answers = [ new() { AnswerId = "A", AnswerText = "NHS", IsSelected = true } ]
            },
            new()
            {
                QuestionId = "Q2",
                NonNhsInvolvment = "Non-NHS",
                Answers = [ new() { AnswerId = "B", AnswerText = "Non-NHS", IsSelected = true } ]
            },
            new()
            {
                QuestionId = "Q3",
                AffectedOrganisations = true,
                SelectedOption = "C",
                Answers = [ new() { AnswerId = "C", AnswerText = "Org1" } ]
            },
            new()
            {
                QuestionId = "Q4",
                RequireAdditionalResources = true,
                SelectedOption = "D",
                Answers = [ new() { AnswerId = "D", AnswerText = "Yes" } ]
            }
        };

        // Act
        var result = ModificationHelpers.GetRankingOfChangeRequest("areaId", true, questions);

        // Assert
        result.ShouldNotBeNull();
        result.SpecificAreaOfChangeId.ShouldBe("areaId");
        result.Applicability.ShouldBe("Yes");
        result.IsNHSInvolved.ShouldBeTrue();
        result.IsNonNHSInvolved.ShouldBeTrue();
        result.NhsOrganisationsAffected.ShouldBe("Org1");
        result.NhsResourceImplicaitons.ShouldBeTrue();
    }

    [Fact]
    public void GetRankingOfChangeRequest_Should_Handle_Missing_Questions_And_Selections()
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new() { QuestionId = "Q1", NhsInvolvment = "NHS", Answers = [ new() { AnswerId = "A", AnswerText = "NHS", IsSelected = false } ] },
            new() { QuestionId = "Q2", NonNhsInvolvment = "Non-NHS", Answers = [ new() { AnswerId = "B", AnswerText = "Non-NHS", IsSelected = false } ] },
            new() { QuestionId = "Q3", AffectedOrganisations = true, SelectedOption = null, Answers = [ new() { AnswerId = "C", AnswerText = "Org1" } ] },
            new() { QuestionId = "Q4", RequireAdditionalResources = true, SelectedOption = null, Answers = [ new() { AnswerId = "D", AnswerText = "No" } ] }
        };

        // Act
        var result = ModificationHelpers.GetRankingOfChangeRequest("areaId", false, questions);

        // Assert
        result.ShouldNotBeNull();
        result.SpecificAreaOfChangeId.ShouldBe("areaId");
        result.Applicability.ShouldBe("No");
        result.IsNHSInvolved.ShouldBeFalse();
        result.IsNonNHSInvolved.ShouldBeFalse();
        result.NhsOrganisationsAffected.ShouldBeNull();
        result.NhsResourceImplicaitons.ShouldBeFalse();
    }

    [Fact]
    public void GetRankingOfChangeRequest_Should_Return_Defaults_When_Questions_List_Empty()
    {
        // Arrange
        var questions = new List<QuestionViewModel>();

        // Act
        var result = ModificationHelpers.GetRankingOfChangeRequest("areaId", false, questions);

        // Assert
        result.ShouldNotBeNull();
        result.SpecificAreaOfChangeId.ShouldBe("areaId");
        result.Applicability.ShouldBe("No");
        result.IsNHSInvolved.ShouldBeFalse();
        result.IsNonNHSInvolved.ShouldBeFalse();
        result.NhsOrganisationsAffected.ShouldBeNull();
        result.NhsResourceImplicaitons.ShouldBeFalse();
    }
}