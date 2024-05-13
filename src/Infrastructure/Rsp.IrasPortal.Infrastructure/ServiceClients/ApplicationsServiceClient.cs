using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Infrastructure.HttpClients;

namespace Rsp.IrasPortal.Infrastructure.ServiceClients;

public class ApplicationsServiceClient(IApplicationsHttpClient client) : IApplicationsServiceClient
{
    public async Task<IrasApplication> GetApplication(int id)
    {
        return await client.GetApplication(id);
    }

    public async Task<IEnumerable<IrasApplication>> GetApplications()
    {
        return await client.GetApplications();
    }

    public async Task<IrasApplication> CreateApplication(IrasApplication irasApplication)
    {
        var application = await client.CreateApplication(irasApplication);

        return await GetApplication(application.Id);
    }

    public async Task<IrasApplication> UpdateApplication(int id, IrasApplication irasApplication)
    {
        await client.UpdateApplication(id, irasApplication);

        return await GetApplication(id);
    }
}