namespace Rsp.IrasPortal.Application.Constants;

public static class FileConstants
{
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Video
        ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".mpeg", ".mpg", ".webm",

        // Images
        ".png", ".gif", ".bmp", ".svg", ".jpg", ".jpeg",

        // Documents
        ".doc", ".docx", ".dot", ".dotx", ".pdf", ".rtf", ".odt", ".ofd", ".xps",

        // Spreadsheets
        ".xls", ".xlsx", ".csv",

        // Presentations
        ".ppt", ".pptx",

        // Text / Data
        ".txt", ".xml", ".html", ".htm",

        // Contact / Email
        ".vcf", ".eml", ".msg"
    };
}