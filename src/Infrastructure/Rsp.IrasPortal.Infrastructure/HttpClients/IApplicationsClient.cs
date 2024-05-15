using Refit;
using Rsp.IrasPortal.Domain.Entities;

namespace Rsp.IrasPortal.Infrastructure.HttpClients;

public interface IApplicationsHttpClient
{
    [Get("/applications")]
    public Task<IrasApplication> GetApplication(int id);

    [Get("/applications/all")]
    public Task<IEnumerable<IrasApplication>> GetApplications();

    [Post("/applications")]
    public Task<IrasApplication> CreateApplication(IrasApplication irasApplication);

    [Post("/applications/update")]
    public Task<IrasApplication> UpdateApplication(int id, IrasApplication irasApplication);
}