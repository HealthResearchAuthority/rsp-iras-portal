using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;

namespace Rsp.IrasPortal.Services;

public class ApplicationsService(IApplicationsServiceClient applicationsClient) : IApplicationsService
{
    public Task<IrasApplication> GetApplication(int id)
    {
        return applicationsClient.GetApplication(id);
    }

    public Task<IEnumerable<IrasApplication>> GetApplications()
    {
        return applicationsClient.GetApplications();
    }

    public Task<IrasApplication> CreateApplication(IrasApplication irasApplication)
    {
        return applicationsClient.CreateApplication(irasApplication);
    }

    public Task<IrasApplication> UpdateApplication(int id, IrasApplication irasApplication)
    {
        return applicationsClient.UpdateApplication(id, irasApplication);
    }
}