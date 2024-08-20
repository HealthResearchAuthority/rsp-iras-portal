using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionSetValidator : AbstractValidator<QuestionnaireViewModel>
{
    public QuestionSetValidator()
    {
        RuleForEach(x => x.Answers)
            .ChildRules(question =>
            {
                question
                    .RuleFor(q => !q.IsMandatory)
                    .Cascade(CascadeMode.Stop);

                question.When(q => q.QuestionType == "Text", () =>
                {
                    question
                        .RuleFor(ans => ans.AnswerText)
                        .NotEmpty()
                        .WithMessage(ans => $"Please provide an answer for question {ans.Heading} under {ans.Section} section");
                });

                question.When(q => q.QuestionType == "Look-up list" && q.DataType == "Checkbox", () =>
                {
                    question
                        .RuleFor(ans => ans.SelectedAnswers)
                        .Must(ans => ans.Any(a => a.IsSelected))
                        .WithMessage(ans => $"Please select at least one option for question {ans.Heading} under {ans.Section} section");
                });

                question.When(q => (q.QuestionType == "Look-up list" && q.DataType == "Radio button") || q.DataType == "Boolean", () =>
                {
                    question
                        .RuleFor(ans => ans.SelectedAnswers)
                        .Must(ans => ans.Count(a => a.IsSelected) == 1)
                        .WithMessage(ans => $"Please select one option for question {ans.Heading} under {ans.Section} section");
                });
            });
    }
}