namespace Rsp.IrasPortal.Web.Models;

public class DocumentSummaryItem
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; } // In bytes
    public string DisplaySize => $"{Math.Round((double)FileSize / 1024, 1)}KB";
}