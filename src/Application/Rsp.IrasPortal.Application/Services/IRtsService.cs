using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
///     RTS Service Interface. Marked as IInterceptable to enable
///     the start/end logging for all methods.
/// </summary>
public interface IRtsService : IInterceptable
{
    /// <summary>
    ///     Gets all possible organisations for the search term
    /// </summary>
    /// <param name="searchTerm">Search term for the lookup</param>
    /// <returns></returns>
    Task<ServiceResponse<IEnumerable<RtsSearchByNameDto>>> SearchByName(string searchTerm);
}