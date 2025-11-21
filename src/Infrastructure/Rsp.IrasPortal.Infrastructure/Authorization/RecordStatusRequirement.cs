using Microsoft.AspNetCore.Authorization;

namespace Rsp.IrasPortal.Infrastructure.Authorization;

/// <summary>
/// Requirement that validates if a user can access a record based on its status and their roles
/// </summary>
public class RecordStatusRequirement : IAuthorizationRequirement
{
    public string EntityType { get; }
    public string Status { get; }

    public RecordStatusRequirement(string entityType, string status)
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }
}
