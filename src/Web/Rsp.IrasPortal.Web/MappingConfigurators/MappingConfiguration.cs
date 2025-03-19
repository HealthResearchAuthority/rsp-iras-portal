using Mapster;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.MappingConfigurators;

public static class MappingConfiguration
{
    public static void Configure()
    {
        TypeAdapterConfig<UserViewModel, UpdateUserRequest>
            .NewConfig()
            .Map(dest => dest.Country, src => src.Country != null ? string.Join(',', src.Country) : null);

        TypeAdapterConfig<UserViewModel, CreateUserRequest>
            .NewConfig()
            .Map(dest => dest.Country, src => src.Country != null ? string.Join(',', src.Country) : null);
    }
}