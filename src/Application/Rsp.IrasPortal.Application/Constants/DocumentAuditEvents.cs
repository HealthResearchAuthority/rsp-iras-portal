namespace Rsp.Portal.Application.Constants;

public static class DocumentAuditEvents
{
    public const string UploadSuccessful = "Document upload successful";
    public const string UploadFailedUnsupported = "Document upload failed due to unsupported document";
    public const string UploadFailedFileSize = "Document upload failed due to exceeding maximum file size";
    public const string UploadFailedDuplicate = "Document upload failed due to duplicate documents";
    public const string DocumentDetailsCompleted = "Document details completed";
}