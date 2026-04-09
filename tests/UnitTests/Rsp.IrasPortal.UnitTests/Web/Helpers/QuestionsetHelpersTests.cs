using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Helpers;

public class QuestionsetHelpersTests : TestServiceBase
{
    [Fact]
    public void QuestionTransformer_ShouldTransform_WithAnswersAndRules()
    {
        // Arrange
        var section = new SectionModel
        {
            Id = "section1",
            SectionName = "Test Section",
            Questions = new List<QuestionModel>
            {
                new QuestionModel
                {
                    Id = "q1",
                    Name = "Question 1",
                    Conformance = "Mandatory",
                    AnswerDataType = "string",
                    QuestionFormat = "Text",
                    CategoryId = "Cat1",
                    Version = "v1",
                    GuidanceComponents = [new() { ContentType = "ContentType" }],
                    Answers = new List<AnswerModel>
                    {
                        new AnswerModel { Id = "a1", OptionName = "Option 1" }
                    },
                    ValidationRules = new List<RuleModel>
                    {
                        new RuleModel
                        {
                            Conditions = new List<ConditionModel>
                            {
                                new ConditionModel
                                {
                                    ParentOptions = new List<AnswerModel>
                                    {
                                        new AnswerModel { Id = Guid.NewGuid().ToString() }
                                    }
                                }
                            },
                            ParentQuestion = new QuestionModel { Id = Guid.NewGuid().ToString() }
                        }
                    }
                }
            }
        };

        // Act
        var result = QuestionsetHelpers.QuestionTransformer(section).ToList();

        // Assert
        result.Count.ShouldBe(1);
        var first = result.First();
        first.IsMandatory.ShouldBeTrue();
        first.Heading.ShouldBe("1");
        first.Sequence.ShouldBe(1);
        first.Section.ShouldBe("Test Section");
        first.QuestionId.ShouldBe("q1");
        first.QuestionText.ShouldBe("Question 1");
        first.Answers.Count.ShouldBe(1);
        first.Answers.First().AnswerText.ShouldBe("Option 1");
        first.Rules.Count.ShouldBe(1);
        first.Rules.First().Conditions.Count().ShouldBe(1);
        first.Rules.First().Conditions.First().IsApplicable.ShouldBeTrue();
    }

    [Fact]
    public void QuestionTransformer_ShouldReturnEmptyList_WhenNoQuestions()
    {
        // Arrange
        var section = new SectionModel
        {
            Id = "section1",
            SectionName = "Empty Section",
            Questions = new List<QuestionModel>()
        };

        // Act
        var result = QuestionsetHelpers.QuestionTransformer(section);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void QuestionTransformer_ShouldHandle_NoAnswers()
    {
        // Arrange
        var section = new SectionModel
        {
            Id = "section1",
            SectionName = "No Answers Section",
            Questions = new List<QuestionModel>
            {
                new QuestionModel
                {
                    Id = "q1",
                    Name = "Question 1",
                    Conformance = "Optional",
                    Answers = null,
                    ValidationRules = null
                }
            }
        };

        // Act
        var result = QuestionsetHelpers.QuestionTransformer(section).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result.First().Answers.ShouldBeEmpty();
        result.First().Rules.ShouldBeEmpty();
    }

    [Fact]
    public void QuestionTransformer_ShouldHandle_NullValidationRules()
    {
        // Arrange
        var section = new SectionModel
        {
            Id = "section1",
            SectionName = "Null Rules Section",
            Questions = new List<QuestionModel>
            {
                new QuestionModel
                {
                    Id = "q1",
                    Name = "Question 1",
                    Conformance = "Mandatory",
                    Answers = new List<AnswerModel>
                    {
                        new AnswerModel { Id = "a1", OptionName = "Opt1" }
                    },
                    ValidationRules = null
                }
            }
        };

        // Act
        var result = QuestionsetHelpers.QuestionTransformer(section).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result.First().Rules.ShouldBeEmpty();
        result.First().Answers.Count.ShouldBe(1);
    }

    [Fact]
    public void MapQuestionWithAnswers_ShouldMapMatchingAnswerAndSelectedOptions()
    {
        // Arrange
        var answerId = Guid.NewGuid();
        var cmsQuestion = new QuestionViewModel
        {
            QuestionId = "Q1",
            VersionId = "v1",
            Category = "Cat1",
            SectionId = "Sec1",
            Section = "Section",
            Sequence = 1,
            Heading = "1",
            QuestionText = "Question 1",
            QuestionType = "Radio button",
            DataType = "Radio button",
            IsMandatory = true,
            IsOptional = false,
            ShortQuestionText = "Q1",
            Answers =
            [
                new AnswerViewModel { AnswerId = "A1", AnswerText = "Option 1", IsSelected = false },
                new AnswerViewModel { AnswerId = "A2", AnswerText = "Option 2", IsSelected = false }
            ]
        };

        var answers = new List<TestAnswer>
        {
            new()
            {
                Id = answerId,
                QuestionId = "Q1",
                AnswerText = "Some answer",
                SelectedOption = "A1"
            }
        };

        // Act
        var mapped = QuestionsetHelpers.MapQuestionWithAnswers
        (
            cmsQuestion,
            answers,
            index: 7,
            questionIdSelector: a => a.QuestionId,
            idSelector: a => a.Id,
            answerTextSelector: a => a.AnswerText,
            selectedOptionSelector: a => a.SelectedOption
        );

        // Assert
        mapped.Id.ShouldBe(answerId);
        mapped.Index.ShouldBe(7);
        mapped.QuestionId.ShouldBe("Q1");
        mapped.AnswerText.ShouldBe("Some answer");
        mapped.SelectedOption.ShouldBe("A1");
        mapped.Answers.Single(a => a.AnswerId == "A1").IsSelected.ShouldBeTrue();
        mapped.Answers.Single(a => a.AnswerId == "A2").IsSelected.ShouldBeFalse();
    }

    private sealed class TestAnswer
    {
        public Guid? Id { get; set; }

        public string? QuestionId { get; set; }

        public string? AnswerText { get; set; }

        public string? SelectedOption { get; set; }
    }
}