using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services.Extensions;

namespace Rsp.Portal.Services;

public class ProjectCollaboratorService
(
    IProjectCollaboratorServiceClient projectCollaboratorServiceClient
) : IProjectCollaboratorService
{
    public async Task<ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>> GetProjectCollaborators(string projectRecordId)
    {
        var apiResponse = await projectCollaboratorServiceClient.GetProjectCollaborators(projectRecordId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<ProjectCollaboratorResponse>> SaveProjectCollaborator(ProjectCollaboratorRequest projectCollaboratorRequest)
    {
        var apiResponse = await projectCollaboratorServiceClient.SaveProjectCollaborator(projectCollaboratorRequest);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> UpdateCollaboratorAccess(UpdateCollaboratorAccessRequest updateCollaboratorAccessRequest)
    {
        var apiResponse = await projectCollaboratorServiceClient.UpdateCollaboratorAccess(updateCollaboratorAccessRequest);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse> RemoveProjectCollaborator(string projectCollaboratorId)
    {
        var apiResponse = await projectCollaboratorServiceClient.RemoveProjectCollaborator(projectCollaboratorId);

        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<CollaboratorProjectResponse>>> GetCollaboratorProjects(string userId)
    {
        var apiResponse = await projectCollaboratorServiceClient.GetCollaboratorProjects(userId);

        return apiResponse.ToServiceResponse();
    }
}