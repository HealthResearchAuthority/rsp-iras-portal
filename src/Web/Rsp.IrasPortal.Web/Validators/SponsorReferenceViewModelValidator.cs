using FluentValidation;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class SponsorReferenceViewModelValidator : AbstractValidator<SponsorReferenceViewModel>
{
    private const int MaxCharactersCount = 3200;

    private string modelValidationMessage = "The date should be a valid date and not in future";

    // Default property name if not overridden by context
    private string modelValidationPropertyName = "SponsorModificationDate";

    public SponsorReferenceViewModelValidator()
    {
        RuleFor(model => model.SponsorModificationDate)
           .Cascade(CascadeMode.Stop)
           .Custom((date, context) =>
           {
               // if date is null - all fields - Day, Month, Year should be empty
               if (!date.Date.HasValue && (date.Date is not null || date.Month is not null || date.Year is not null))
               {
                   AddFailures(context);
               }
           })
           .Custom((date, context) =>
           {
               // date must not be in future
               if (date.Date is not null && date.Date.Value > DateTime.Now.Date)
               {
                   AddFailures(context);
               }
           });

        RuleFor(x => x.MainChangesDescription)
            .Must(text => GetCharactersCount(text) > 0)
                .WithMessage("Enter a job description");

        RuleFor(x => x.MainChangesDescription)
            .Must(text => GetCharactersCount(text) <= MaxCharactersCount)
            .WithMessage($"Job description must be {MaxCharactersCount} characters or less");

        RuleFor(x => x.MainChangesDescription)
            .Custom((text, context) =>
            {
                var characterCount = GetCharactersCount(text);
                if (characterCount > MaxCharactersCount)
                {
                    var excessCharacters = characterCount - MaxCharactersCount;
                    context.AddFailure("_DescriptionExcessCharactersCount", $"You have {excessCharacters} character{(excessCharacters == 1 ? "" : "s")} too many");
                }
            });
    }

    private static int GetCharactersCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }
        return text.Length;
    }

    private void AddFailures(ValidationContext<SponsorReferenceViewModel> context)
    {
        if (context.RootContextData.TryGetValue(ValidationKeys.ValidationMessage, out object? message) && message is string validationMessage)
        {
            modelValidationMessage = validationMessage;
        }

        if (context.RootContextData.TryGetValue(ValidationKeys.PropertyName, out object? propertyName) && propertyName is string validationProperty)
        {
            modelValidationPropertyName = validationProperty;
        }

        context.AddFailure(modelValidationPropertyName, modelValidationMessage);
    }
}