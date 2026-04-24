using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Logging.Interceptors;
using Rsp.Portal.Application.Responses;

namespace Rsp.Portal.Application.Services;

public interface IProjectCollaboratorService : IInterceptable
{
    Task<ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>> GetProjectCollaborators(string projectRecordId);

    Task<ServiceResponse<ProjectCollaboratorResponse>> SaveProjectCollaborator(ProjectCollaboratorRequest projectCollaboratorRequest);

    Task<ServiceResponse> UpdateCollaboratorAccess(UpdateCollaboratorAccessRequest updateCollaboratorAccessRequest);

    Task<ServiceResponse> RemoveProjectCollaborator(string projectCollaboratorId);

    Task<ServiceResponse<IEnumerable<CollaboratorProjectResponse>>> GetCollaboratorProjects(string userId);
}