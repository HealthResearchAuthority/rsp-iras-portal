using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Web.Helpers;

namespace Rsp.IrasPortal.UnitTests.Web.Helpers;

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
                    GuidanceComponents = new List<ContentComponent>{ new ContentComponent { ContentType = "ContentType" } },
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
}