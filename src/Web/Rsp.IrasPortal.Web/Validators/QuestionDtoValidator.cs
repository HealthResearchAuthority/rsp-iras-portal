using FluentValidation;
using FluentValidation.Results;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionDtoValidator : AbstractValidator<QuestionDto>
{
    private List<QuestionDto> _questionDtos = [];

    protected override bool PreValidate(ValidationContext<QuestionDto> context, ValidationResult result)
    {
        _questionDtos = (context.RootContextData["questionDtos"] as List<QuestionDto>) ?? [];

        return base.PreValidate(context, result);
    }

    public QuestionDtoValidator()
    {
        RuleFor(x => x.QuestionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage($"{ModuleColumns.QuestionId} column must contain a value")
            .Must(q => q.StartsWith("IQT") || q.StartsWith("IQA") || q.StartsWith("IQG"))
            .WithMessage("Question ID must start with 'IQT', 'IQA', or 'IQG'");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage(q => $"Question {q.QuestionId}: '{ModuleColumns.Category}' column must contain a value");

        RuleFor(x => x.QuestionText)
            .NotEmpty()
            .WithMessage(q => $"Question {q.QuestionId}: '{ModuleColumns.QuestionText}' column must contain a value");

        RuleFor(x => x.QuestionType)
            .NotEmpty()
            .WithMessage(q => $"Question {q.QuestionId}: '{ModuleColumns.QuestionType}' column must contain a value");

        // Question / guidance (IQA / IQG) specific rules
        When(x => x.QuestionId.StartsWith("IQA") || x.QuestionId.StartsWith("IQG"), () =>
        {
            RuleFor(x => x.SectionId)
                .NotEmpty()
                .WithMessage(q => $"Question {q.QuestionId}: '{ModuleColumns.Section}' column must contain a value");

            RuleFor(x => x.Sequence)
                .NotEmpty()
                .GreaterThan(0)
                .WithMessage(q => $"Question {q.QuestionId}: '{ModuleColumns.Sequence}' column must contain an integer greater than 0");

            RuleFor(x => x.Heading)
                .NotEmpty()
                .WithMessage(q => $"Question {q.QuestionId}: '{ModuleColumns.Heading}' column must contain a value");

            RuleFor(x => x.DataType)
                .NotEmpty()
                .WithMessage(q => $"Question {q.QuestionId}: '{ModuleColumns.DataType}' column must contain a value");
        });

        // Custom validation rules
        RuleFor(x => x)
            .Custom((question, context) =>
            {
                // Checking for duplicate QuestionIds
                if (_questionDtos.Count(q => q.QuestionId == question.QuestionId) > 1)
                {
                    context.AddFailure("QuestionId", $"Duplicate Question ID detected: {question.QuestionId}");
                }
            });
    }
}