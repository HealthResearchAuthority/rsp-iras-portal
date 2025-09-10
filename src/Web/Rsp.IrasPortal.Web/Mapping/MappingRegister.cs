using Mapster;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Mapping;

public class MappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
           .NewConfig<UserViewModel, UpdateUserRequest>()
           .Map(dest => dest.Country, src => src.Country != null ? string.Join(',', src.Country) : null);

        config
            .NewConfig<UserViewModel, CreateUserRequest>()
            .Map(dest => dest.Country, src => src.Country != null ? string.Join(',', src.Country) : null);

        config
            .NewConfig<SectionModel, QuestionSectionsResponse>()
            .Map(dest => dest.QuestionCategoryId, src => src.CategoryId)
            .Map(dest => dest.SectionId, src => src.Id);
    }
}