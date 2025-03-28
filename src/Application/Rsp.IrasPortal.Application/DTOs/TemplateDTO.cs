namespace Rsp.IrasPortal.Application.DTOs;

public class ConvertDateTime
{
    public static string GetFormattedDate(DateTime date)
    {
        return $"{AddDaySuffix(date.Day)} {date:MMMM yyyy}";
    }

    public static string AddDaySuffix(int day)
    {
        if (day >= 11 && day <= 13) return day + "th";

        return day + (day % 10 == 1 ? "st" :
                      day % 10 == 2 ? "nd" :
                      day % 10 == 3 ? "rd" : "th");
    }
}

public record TemplateDTO
{
    public required ProjectInfoDTO ProjectInfo { get; set; }

    public string? Category { get; set; }
    public DateTime DateSubmitted { get; set; }
    public string FormattedDate => $"{DateSubmitted.Day}{GetDaySuffix(DateSubmitted.Day)} {DateSubmitted:MMMM yyyy}";
    public string? DateLeftOnTheClock { get; set; }

    public string? Status { get; set; }
    private string GetDaySuffix(int day)
    {
        return (day % 10 == 1 && day != 11) ? "st" :
               (day % 10 == 2 && day != 12) ? "nd" :
               (day % 10 == 3 && day != 13) ? "rd" : "th";
    }
}