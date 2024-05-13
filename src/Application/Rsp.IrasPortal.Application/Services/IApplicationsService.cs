using Rsp.IrasPortal.Domain.Entities;

namespace Rsp.IrasPortal.Application.Services;

public interface IApplicationsService
{
    public Task<IrasApplication> GetApplication(int id);

    public Task<IEnumerable<IrasApplication>> GetApplications();

    public Task<IrasApplication> CreateApplication(IrasApplication irasApplication);

    public Task<IrasApplication> UpdateApplication(int id, IrasApplication irasApplication);
}