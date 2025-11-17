using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface IModificationRankingService
{
    // Computes and persists a single change's ranking
    Task UpdateChangeRanking(Guid modificationChangeId, string projectRecordId);

    // Recomputes all changes’ rankings and updates modification overall ranking
    Task UpdateOverallRanking(Guid projectModificationId, string projectRecordId);

    // Pure calculation if a caller just needs the computed ranking
    Task<RankingOfChangeResponse> CalculateChangeRanking
    (
        string projectRecordId,
        string specificAreaOfChangeId,
        bool showApplicabilityQuestions,
        Guid modificationChangeId
    );
}