using FluentValidation;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Validators;

public class ProjectClosuresModelValidator : AbstractValidator<ProjectClosuresModel>
{
    public ProjectClosuresModelValidator()
    {
        RuleFor(x => x)
            .Custom((model, context) =>
            {
                // 1) Missing parts (your bespoke messages)
                var dayEmpty = string.IsNullOrWhiteSpace(model.ActualClosureDateDay);
                var monthEmpty = string.IsNullOrWhiteSpace(model.ActualClosureDateMonth);
                var yearEmpty = string.IsNullOrWhiteSpace(model.ActualClosureDateYear);

                if (dayEmpty && monthEmpty && yearEmpty)
                {
                    context.AddFailure("ActualClosureDate", "Enter the project closure date");
                    return;
                }

                if (dayEmpty || monthEmpty || yearEmpty)
                {
                    var missing = new List<string>();
                    if (dayEmpty) missing.Add("day");
                    if (monthEmpty) missing.Add("month");
                    if (yearEmpty) missing.Add("year");

                    var partsText = missing.Count switch
                    {
                        1 => missing[0],
                        2 => $"{missing[0]} and {missing[1]}",
                        _ => $"{string.Join(", ", missing.Take(missing.Count - 1))} and {missing.Last()}"
                    };

                    context.AddFailure("ActualClosureDate", $"Project closure must include a {partsText}");
                    return;
                }

                // 2) Must be a real date (e.g. 30 Feb)
                if (!int.TryParse(model.ActualClosureDateDay, out var day) ||
                    !int.TryParse(model.ActualClosureDateMonth, out var month) ||
                    !int.TryParse(model.ActualClosureDateYear, out var year) ||
                    year < 1900 || year > 2100 ||
                    month < 1 || month > 12 ||
                    day < 1 || day > DateTime.DaysInMonth(year, month))
                {
                    context.AddFailure("ActualClosureDate", "Project closure must be a real date");
                    return;
                }

                var closureDate = new DateOnly(year, month, day);

                // 3) Must not be in the future
                if (closureDate > DateOnly.FromDateTime(DateTime.Today))
                {
                    context.AddFailure("ActualClosureDate", "Project closure must be today or in the past");
                }
            });
    }
}
