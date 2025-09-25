namespace Rsp.IrasPortal.Application.Constants;

public static class FileConstants
{
    public static readonly IReadOnlyCollection<string> AllowedExtensions =
        [
            ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".mpeg", ".mpg", ".webm",
            ".png", ".gif", ".bmp", ".svg", ".jpg", ".jpeg",
            ".doc", ".docx", ".dot", ".dotx", ".pdf", ".rtf", ".odt", ".ofd", ".xps",
            ".xls", ".xlsx", ".csv",
            ".ppt", ".pptx",
            ".txt", ".xml", ".html", ".htm",
            ".vcf", ".eml", ".msg"
        ];
}