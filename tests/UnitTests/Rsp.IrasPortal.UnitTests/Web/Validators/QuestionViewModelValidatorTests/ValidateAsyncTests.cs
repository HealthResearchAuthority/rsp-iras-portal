using FluentValidation;
using FluentValidation.TestHelper;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.QuestionViewModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<QuestionViewModelValidator>
{
    private static ValidationContext<QuestionViewModel> CreateValidationContext(QuestionViewModel model)
    {
        var context = new ValidationContext<QuestionViewModel>(model);
        context.RootContextData["questions"] = new List<QuestionViewModel> { model };
        return context;
    }

    [Theory]
    [InlineData("Date")]
    [InlineData("Email")]
    [InlineData("Text")]
    public async Task ValidateAsync_MandatoryQuestionWithEmptyAnswerText_ShouldHaveValidationError(string dataType)
    {
        // Arrange
        var question = new QuestionViewModel
        {
            IsMandatory = true,
            QuestionText = "Test Question",
            DataType = dataType,
            AnswerText = string.Empty,
            Heading = "Test Question",
            Section = "Test Section"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(e => e.AnswerText);
    }

    [Fact]
    public async Task ValidateAsync_MandatoryCheckboxQuestionWithNoSelectedAnswers_ShouldHaveValidationError()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q1",
            QuestionText = "Test Question",
            Heading = "Test Question",
            Section = "Test Section",
            IsMandatory = true,
            DataType = "Checkbox",
            Answers =
            [
                new AnswerViewModel { AnswerId = "A1", IsSelected = false },
                new AnswerViewModel { AnswerId = "A2", IsSelected = false }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(q => q.Answers)
            .WithErrorMessage($"Enter {question.QuestionText.ToLowerInvariant()}");
    }

    [Fact]
    public async Task ValidateAsync_MandatoryRadioButtonQuestionWithNoSelectedOption_ShouldFailValidation()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q1",
            QuestionText = "Test Question",
            Heading = "Test Question",
            Section = "Test Section",
            DataType = "Radio button",
            IsMandatory = true,
            SelectedOption = null,
            Answers =
            [
                new() { AnswerId = "A1", AnswerText = "Option 1" },
                new() { AnswerId = "A2", AnswerText = "Option 2" }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(q => q.SelectedOption)
            .WithErrorMessage($"Enter {question.QuestionText.ToLowerInvariant()}");
    }

    [Fact]
    public async Task Should_Validate_NonMandatory_Question_With_Applicable_Rules()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q1",
            IsMandatory = false,
            DataType = "Text",
            Heading = "Test Question",
            Section = "Test Section",
            Rules =
            [
                new() {
                    ParentQuestionId = "ParentQ",
                    Mode = "AND",
                    Conditions =
                    [
                        new ConditionDto
                        {
                            Operator = "IN",
                            ParentOptions = ["Option1"],
                            Mode = "AND"
                        }
                    ]
                }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_WithRegexRule_ShouldAddFailureForInvalidAnswer()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q1",
            Heading = "Test Question",
            Section = "Test Section",
            DataType = "Text",
            AnswerText = "InvalidEmail",
            IsMandatory = true,
            Rules =
            [
                new RuleDto
                {
                    Conditions =
                    [
                        new ConditionDto
                        {
                            Operator = "REGEX",
                            Value = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                            Description = "Invalid email format"
                        }
                    ]
                }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(q => q.AnswerText);
    }

    [Fact]
    public async Task ValidateAsync_QuestionWithLengthRule_ShouldValidateCorrectly()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q1",
            Heading = "Test Question",
            Section = "Test Section",
            DataType = "Text",
            IsMandatory = true,
            AnswerText = "Short",
            Rules =
            [
                new RuleDto
                {
                    Conditions =
                    [
                        new ConditionDto
                        {
                            Operator = "LENGTH",
                            Value = "5,10",
                            Description = "Answer must be between 5 and 10 characters"
                        }
                    ]
                }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Evaluate_Complex_Rule_With_Multiple_AND_OR_Conditions()
    {
        // Arrange
        var parentQuestions = new List<QuestionViewModel>
        {
            new()
            {
                QuestionId = "P1",
                DataType = "Radio button",
                SelectedOption = "Option1"
            },
            new()
            {
                QuestionId = "P2",
                DataType = "Checkbox",
                Answers =
                [
                    new() { AnswerId = "A1", IsSelected = true },
                    new() { AnswerId = "A2", IsSelected = false },
                    new() { AnswerId = "A3", IsSelected = true }
                ]
            },
            new()
            {
                QuestionId = "P3",
                DataType = "Boolean",
                SelectedOption = "Yes"
            }
        };

        var questionToValidate = new QuestionViewModel
        {
            QuestionId = "Q1",
            DataType = "Text",
            AnswerText = "Test Answer",
            IsMandatory = false,
            Rules =
            [
                new()
                {
                    Mode = "AND",
                    Sequence = 1,
                    ParentQuestionId = "P1",
                    Conditions =
                    [
                        new() { Mode = "OR", Operator = "IN", ParentOptions = ["Option1", "Option2"] }
                    ]
                },
                new()
                {
                    Mode = "AND",
                    Sequence = 2,
                    ParentQuestionId = "P2",
                    Conditions =
                    [
                        new() { Mode = "AND", Operator = "IN", ParentOptions = ["A1", "A3"], OptionType = "Exact" }
                    ]
                },
                new()
                {
                    Mode = "OR",
                    Sequence = 3,
                    ParentQuestionId = "P3",
                    Conditions =
                    [
                        new() { Mode = "AND", Operator = "IN", ParentOptions = ["No"] }
                    ]
                }
            ]
        };

        var context = new ValidationContext<QuestionViewModel>(questionToValidate);
        context.RootContextData["questions"] = parentQuestions.Concat([questionToValidate]).ToList();

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Handle_Parent_Question_Not_Found_In_Rules_Evaluation()
    {
        // Arrange
        var parentQuestions = new List<QuestionViewModel>
    {
        new()
        {
            QuestionId = "P1",
            DataType = "Radio button",
            SelectedOption = "Option1"
        }
    };

        var questionToValidate = new QuestionViewModel
        {
            QuestionId = "Q1",
            DataType = "Text",
            AnswerText = "Test Answer",
            IsMandatory = false,
            Rules =
        [
            new()
            {
                Mode = "AND",
                Sequence = 1,
                ParentQuestionId = "P1",
                Conditions =
                [
                    new() { Mode = "AND", Operator = "IN", ParentOptions = ["Option1"] }
                ]
            },
            new()
            {
                Mode = "AND",
                Sequence = 2,
                ParentQuestionId = "P2", // This parent question doesn't exist
                Conditions =
                [
                    new() { Mode = "AND", Operator = "IN", ParentOptions = ["SomeOption"] }
                ]
            }
        ]
        };

        var context = new ValidationContext<QuestionViewModel>(questionToValidate);
        context.RootContextData["questions"] = parentQuestions.Concat(new[] { questionToValidate }).ToList();

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Validate_Checkbox_Question_With_Single_Option_Type_Condition()
    {
        // Arrange
        var parentQuestion = new QuestionViewModel
        {
            QuestionId = "P1",
            DataType = "Checkbox",
            Answers =
        [
            new() { AnswerId = "A1", IsSelected = true },
            new() { AnswerId = "A2", IsSelected = false },
            new() { AnswerId = "A3", IsSelected = false }
        ]
        };

        var questionToValidate = new QuestionViewModel
        {
            QuestionId = "Q1",
            DataType = "Text",
            AnswerText = "Test Answer",
            IsMandatory = false,
            Rules =
        [
            new()
            {
                Mode = "AND",
                Sequence = 1,
                ParentQuestionId = "P1",
                Conditions = new List<ConditionDto>
                {
                    new()
                    {
                        Mode = "AND",
                        Operator = "IN",
                        ParentOptions = ["A1"],
                        OptionType = "Single"
                    }
                }
            }
        ]
        };

        var context = new ValidationContext<QuestionViewModel>(questionToValidate);
        context.RootContextData["questions"] = new List<QuestionViewModel> { parentQuestion, questionToValidate };

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Validate_Checkbox_Question_With_Exact_Option_Type_Condition()
    {
        // Arrange
        var parentQuestion = new QuestionViewModel
        {
            QuestionId = "P1",
            DataType = "Checkbox",
            Answers = new List<AnswerViewModel>
        {
            new() { AnswerId = "A1", IsSelected = true },
            new() { AnswerId = "A2", IsSelected = true },
            new() { AnswerId = "A3", IsSelected = false },
            new() { AnswerId = "A4", IsSelected = false }
        }
        };

        var questionToValidate = new QuestionViewModel
        {
            QuestionId = "Q1",
            DataType = "Text",
            AnswerText = "Test Answer",
            IsMandatory = false,
            Rules = new List<RuleDto>
        {
            new()
            {
                Mode = "AND",
                Sequence = 1,
                ParentQuestionId = "P1",
                Conditions = new List<ConditionDto>
                {
                    new()
                    {
                        Mode = "AND",
                        Operator = "IN",
                        ParentOptions = new List<string> { "A1", "A2" },
                        OptionType = "Exact"
                    }
                }
            }
        }
        };

        var context = new ValidationContext<QuestionViewModel>(questionToValidate);
        context.RootContextData["questions"] = new List<QuestionViewModel> { parentQuestion, questionToValidate };

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_WithDateRule_ShouldAddFailureForInvalidAnswer()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q1",
            Heading = "Planned end date",
            Section = "Project Details",
            DataType = "Date",
            AnswerText = "2025-03-16",
            IsMandatory = true,
            Rules =
            [
                new RuleDto
                {
                    Conditions =
                    [
                        new ConditionDto
                        {
                            Operator = "DATE",
                            Value = @"FORMAT:yyyy-MM-dd,FUTUREDATE",
                            Description = "Invalid date"
                        }
                    ]
                }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(q => q.AnswerText);
    }

    [Fact]
    public async Task ValidateAsync_WithFutureDateRuleAndNegate_ShouldAddFailureForFutureDate()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q4",
            Heading = "Planned end date",
            Section = "Project Details",
            DataType = "Date",
            AnswerText = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"),
            IsMandatory = true,
            Rules =
            [
                new RuleDto
            {
                Conditions =
                [
                    new ConditionDto
                    {
                        Operator = "DATE",
                        Value = @"FORMAT:yyyy-MM-dd,FUTUREDATE",
                        Description = "Date must NOT be in the future",
                        Negate = true
                    }
                ]
            }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(q => q.AnswerText)
            .WithErrorMessage("Date must NOT be in the future");
    }

    [Fact]
    public async Task ValidateAsync_WithPastDateRule_ShouldAddFailureForNonPastDate()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q2",
            Heading = "Start date",
            Section = "Project Details",
            DataType = "Date",
            AnswerText = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
            IsMandatory = true,
            Rules =
            [
                new RuleDto
            {
                Conditions =
                [
                    new ConditionDto
                    {
                        Operator = "DATE",
                        Value = @"FORMAT:yyyy-MM-dd,PASTDATE",
                        Description = "Date must be in the past"
                    }
                ]
            }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(q => q.AnswerText)
            .WithErrorMessage("Date must be in the past");
    }

    [Fact]
    public async Task ValidateAsync_WithPastDateRuleAndNegate_ShouldAddFailureForPastDate()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q3",
            Heading = "Start date",
            Section = "Project Details",
            DataType = "Date",
            AnswerText = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
            IsMandatory = true,
            Rules =
            [
                new RuleDto
            {
                Conditions =
                [
                    new ConditionDto
                    {
                        Operator = "DATE",
                        Value = @"FORMAT:yyyy-MM-dd,PASTDATE",
                        Description = "Date must NOT be in the past",
                        Negate = true
                    }
                ]
            }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(q => q.AnswerText)
            .WithErrorMessage("Date must NOT be in the past");
    }

    [Fact]
    public async Task ValidateAsync_WithPastDateRuleAndNegate_ShouldPassForTodayDate()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q3",
            Heading = "Start date",
            Section = "Project Details",
            DataType = "Date",
            AnswerText = DateTime.Now.ToString("yyyy-MM-dd"),
            IsMandatory = true,
            Rules =
            [
                new RuleDto
            {
                Conditions =
                [
                    new ConditionDto
                    {
                        Operator = "DATE",
                        Value = @"FORMAT:yyyy-MM-dd,PASTDATE",
                        Description = "Date must NOT be in the past",
                        Negate = true
                    }
                ]
            }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldNotHaveValidationErrorFor(q => q.AnswerText);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingDatePartRule_ShouldAddFailureForIncompleteDateParts()
    {
        // Arrange
        var question = new QuestionViewModel
        {
            QuestionId = "Q3",
            Heading = "Sponsor document date",
            Section = "Documents",
            DataType = "Date",
            AnswerText = "",
            Day = "03",
            Month = "",
            Year = "",
            IsMandatory = true,
            Rules =
            [
                new RuleDto
            {
                Conditions =
                [
                    new ConditionDto
                    {
                        Operator = "DATE",
                        Value = @"MISSINGDATEPART",
                        Description = ""
                    }
                ]
            }
            ]
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(question));

        // Assert
        result
            .ShouldHaveValidationErrorFor(q => q.AnswerText)
            .WithErrorMessage("Date must include a year");
    }
}