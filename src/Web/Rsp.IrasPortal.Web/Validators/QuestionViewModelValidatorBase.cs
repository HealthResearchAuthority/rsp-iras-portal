using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionViewModelValidatorBase : AbstractValidator<QuestionViewModel>
{
    protected void ConfigureLengthRule()
    {
        RuleFor(x => x.AnswerText)
            .Custom((answer, context) =>
            {
                var question = context.InstanceToValidate;

                var conditions = question.Rules.SelectMany(r => r.Conditions.Where(c => c.Operator is "LENGTH"));

                foreach (var condition in conditions)
                {
                    // if it's a length check, condition should have a value in the format min,max
                    // split the value to get the min max
                    var minmax = condition.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    // check if we have an array of length 2
                    if (minmax?.Length == 2)
                    {
                        // get the minmax values
                        if (!(int.TryParse(minmax[0], out var min) && int.TryParse(minmax[1], out var max)))
                        {
                            continue;
                        }

                        if (min <= 0 || max <= min)
                        {
                            continue;
                        }

                        // check if the length is within the bounds
                        if (!(question.AnswerText?.Replace("\r\n", "\n").Length >= min && question.AnswerText?.Replace("\r\n", "\n").Length <= max))
                        {
                            // by setting IsApplicable property
                            // it will display the Description of the condition
                            // for the property
                            condition.IsApplicable = true;
                            context.AddFailure(nameof(question.AnswerText), $"{condition.Description}");
                        }
                    }
                }
            });
    }

    protected void ConfigureRegExRule()
    {
        RuleFor(x => x.AnswerText)
            .Custom((answer, context) =>
            {
                var question = context.InstanceToValidate;

                var conditions = question.Rules.SelectMany(r => r.Conditions.Where(c => c.Operator is "REGEX"));

                foreach (var condition in conditions)
                {
                    // if it's a format check using regex, condition should have a regex expression
                    // see if the answertext matches with the expression

                    if (!Regex.IsMatch(answer ?? string.Empty, condition.Value ?? string.Empty, RegexOptions.Compiled, TimeSpan.FromSeconds(1)))
                    {
                        // by setting IsApplicable property
                        // it will display the Description of the condition
                        // for the property
                        condition.IsApplicable = true;
                        context.AddFailure(nameof(question.AnswerText), $"{condition.Description}");
                    }
                }
            });
    }

    protected void ConfigureDateRule()
    {
        RuleFor(x => x.AnswerText)
            .Custom((answer, context) =>
            {
                var question = context.InstanceToValidate;
                var conditions = question.Rules.SelectMany(r => r.Conditions.Where(c => c.Operator is "DATE"));
                foreach (var condition in conditions)
                {
                    var dateChecks = condition.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (dateChecks is null || dateChecks.Length == 0)
                    {
                        continue;
                    }

                    foreach (var dateCheck in dateChecks)
                    {
                        // TODO - Refactor conditions
                        if (dateCheck.Contains("MISSINGDATEPART"))
                        {
                            var missingParts = new List<string>();

                            if (string.IsNullOrWhiteSpace(question.Day))
                            {
                                missingParts.Add("day");
                            }
                            if (string.IsNullOrWhiteSpace(question.Month))
                            {
                                missingParts.Add("month");
                            }
                            if (string.IsNullOrWhiteSpace(question.Year))
                            {
                                missingParts.Add("year");
                            }

                            if (missingParts.Count == 3)
                            {
                                continue;
                            }
                            else if (missingParts.Count > 0)
                            {
                                condition.IsApplicable = true;

                                switch (missingParts.Count)
                                {
                                    case 2:
                                        context.AddFailure(nameof(question.AnswerText), $"Date must include a {missingParts[0]} and {missingParts[1]}");
                                        break;

                                    case 1:
                                        context.AddFailure(nameof(question.AnswerText), $"Date must include a {missingParts[0]}");
                                        break;
                                }

                                break;
                            }
                        }

                        if (dateCheck.Contains("FORMAT"))
                        {
                            var format = dateCheck.Split(':', StringSplitOptions.RemoveEmptyEntries)[1];
                            if (!DateTime.TryParseExact(answer, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                            {
                                // by setting IsApplicable property
                                // it will display the Description of the condition
                                // for the property
                                condition.IsApplicable = true;
                                context.AddFailure(nameof(question.AnswerText), $"{condition.Description}");
                                break; // Exit the loop immediately if FORMAT check fails
                            }
                        }

                        if (dateCheck.Contains("FUTUREDATE"))
                        {
                            if (!DateTime.TryParse(answer, CultureInfo.InvariantCulture, out var date))
                            {
                                continue;
                            }
                            if (date.Date <= DateTime.Now.Date)
                            {
                                // by setting IsApplicable property
                                // it will display the Description of the condition
                                // for the property
                                condition.IsApplicable = true;
                                context.AddFailure(nameof(question.AnswerText), $"{condition.Description}");
                            }
                        }

                        if (dateCheck.Contains("PASTDATE"))
                        {
                            if (!DateTime.TryParse(answer, CultureInfo.InvariantCulture, out var date))
                            {
                                continue;
                            }
                            if (date.Date > DateTime.Now.Date)
                            {
                                // by setting IsApplicable property
                                // it will display the Description of the condition
                                // for the property
                                condition.IsApplicable = true;
                                context.AddFailure(nameof(question.AnswerText), $"{condition.Description}");
                            }
                        }
                    }
                }
            });
    }

    protected static string GetValidationMessage(QuestionViewModel question)
    {
        var label = !string.IsNullOrWhiteSpace(question.ShortQuestionText)
            ? question.ShortQuestionText
            : question.QuestionText;

        var labelText = label.Contains("NHS / HSC", StringComparison.OrdinalIgnoreCase)
            ? label
            : label.ToLowerInvariant();

        return $"Enter {labelText}";
    }
}