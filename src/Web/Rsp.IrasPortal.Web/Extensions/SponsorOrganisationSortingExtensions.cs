using System.Diagnostics.CodeAnalysis;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Extensions;

[ExcludeFromCodeCoverage]
public static class SponsorOrganisationSortingExtensions
{
    public static IReadOnlyList<SponsorOrganisationDto> SortSponsorOrganisations(
        this IEnumerable<SponsorOrganisationDto> items,
        string? sortField,
        string? sortDirection,
        int pageNumber = 1,
        int pageSize = 20)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        static string CountriesKey(SponsorOrganisationDto x)
        {
            return x.Countries == null || !x.Countries.Any()
                ? string.Empty
                : string.Join(", ", x.Countries.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
        }

        var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        var sorted = sortField?.ToLowerInvariant() switch
        {
            "sponsororganisationname" => desc
                ? items.OrderByDescending(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => CountriesKey(x), StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => CountriesKey(x), StringComparer.OrdinalIgnoreCase),

            "countries" => desc
                ? items.OrderByDescending(x => CountriesKey(x), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => CountriesKey(x), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase),

            "isactive" => desc
                ? items.OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.IsActive)
                    .ThenBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase),

            _ => desc
                ? items.OrderByDescending(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(x => x.SponsorOrganisationName, StringComparer.OrdinalIgnoreCase)
        };

        // Pagination (with sane defaults)
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 20 : pageSize;

        var skip = (pageNumber - 1) * pageSize;

        return sorted.Skip(skip).Take(pageSize).ToList();
    }

    public static IEnumerable<UserViewModel> SortSponsorOrganisationUsers(
        IEnumerable<UserViewModel> users,
        IEnumerable<SponsorOrganisationUserDto>? sponsorOrganisationUserDtos,
        string? sortField,
        string? sortDirection,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var list = users as IList<UserViewModel> ?? users.ToList();

        // Build one lookup: UserId -> latest DTO for that user
        var latestByUserId =
            sponsorOrganisationUserDtos?
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Last())
            ?? new Dictionary<Guid, SponsorOrganisationUserDto>();

        // Helpers
        var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var field = sortField?.Trim().ToLowerInvariant() ?? string.Empty;

        IOrderedEnumerable<UserViewModel> ordered;

        // -------------------------
        // 1. PRIMARY BUBBLING SORTS (only when requested) -------------------------
        if (field == "status")
        {
            // ASC: Active first, then Disabled
            // DESC: Disabled first, then Active
            ordered = desc
                ? list.OrderBy(IsActive) // desc => Disabled first (false then true)
                : list.OrderByDescending(IsActive); // asc  => Active first
        }
        else if (field == "isauthoriser")
        {
            // ASC: No first, then Yes
            // DESC: Yes first, then No
            ordered = desc
                ? list.OrderByDescending(IsAuthoriser)
                : list.OrderBy(IsAuthoriser);
        }
        else
        {
            // No default bubbling by status/authoriser → simply keep identity ordering
            ordered = list.OrderBy(_ => 0);
        }

        // -------------------------
        // 2. SECONDARY: selected field (including sponsorrole) -------------------------
        if (field == "currentlogin")
        {
            ordered = desc ? ordered.ThenByDescending(LatestLogin) : ordered.ThenBy(LatestLogin);
        }
        else if (field == "sponsorrole")
        {
            ordered = desc
                ? ordered.ThenByDescending(SponsorRoleKey, StringComparer.OrdinalIgnoreCase)
                : ordered.ThenBy(SponsorRoleKey, StringComparer.OrdinalIgnoreCase);
        }
        else if (field != "status" && field != "isauthoriser")
        {
            Func<UserViewModel, string> key = field switch
            {
                "familyname" => x => x.FamilyName ?? string.Empty,
                "email" => x => x.Email ?? string.Empty,
                _ => x => x.GivenName ?? string.Empty
            };

            ordered = desc
                ? ordered.ThenByDescending(key, StringComparer.OrdinalIgnoreCase)
                : ordered.ThenBy(key, StringComparer.OrdinalIgnoreCase);
        }

        // -------------------------
        // 3. TERTIARY: tie-break -------------------------
        if (field != "currentlogin")
        {
            ordered = ordered.ThenByDescending(LatestLogin);
        }

        // Pagination
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, pageSize);

        var skip = (pageNumber - 1) * pageSize;
        return ordered.Skip(skip).Take(pageSize);

        // ------------------------- local helpers -------------------------
        static DateTime LatestLogin(UserViewModel x)
        {
            return ((x.CurrentLogin ?? DateTime.MinValue) > (x.LastLogin ?? DateTime.MinValue)
                ? x.CurrentLogin
                : x.LastLogin) ?? DateTime.MinValue;
        }

        bool TryGetLatestDto(UserViewModel u, out SponsorOrganisationUserDto dto)
        {
            dto = default!;
            return Guid.TryParse(u.Id?.Trim(), out var g) && latestByUserId.TryGetValue(g, out dto);
        }

        bool IsActive(UserViewModel u)
        {
            return TryGetLatestDto(u, out var dto) && dto.IsActive;
        }

        bool IsAuthoriser(UserViewModel u)
        {
            return TryGetLatestDto(u, out var dto) && dto.IsAuthoriser;
        }

        string SponsorRoleKey(UserViewModel u)
        {
            if (!TryGetLatestDto(u, out var dto))
            {
                return string.Empty;
            }

            // If SponsorRole is an enum, this will call .ToString() automatically. If it's already
            // a string, it stays a string.
            return dto.SponsorRole ?? string.Empty;
        }
    }

    public static IEnumerable<SponsorOrganisationAuditTrailDto> SortSponsorOrganisationAuditTrails(
        IEnumerable<SponsorOrganisationAuditTrailDto> items,
        string? sortField,
        string? sortDirection,
        string? sponsorOrganisationName,
        int pageNumber = 1,
        int pageSize = 20)
    {
        // 1) Transform descriptions: replace RtsId with sponsorOrganisationName (case-insensitive)
        var list = items
            .Select(x =>
            {
                var desc = x.Description ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(sponsorOrganisationName) && !string.IsNullOrWhiteSpace(x.RtsId))
                {
                    // Replace all occurrences of RtsId with sponsorOrganisationName
                    // (case-insensitive, no regex)
                    desc = desc.Replace(x.RtsId, sponsorOrganisationName!, StringComparison.OrdinalIgnoreCase);
                }

                return new SponsorOrganisationAuditTrailDto
                {
                    Id = x.Id,
                    RtsId = x.RtsId,
                    DateTimeStamp = x.DateTimeStamp,
                    Description = desc,
                    User = x.User
                };
            })
            .ToList();

        // 2) Sorting
        var descSort = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var field = sortField?.ToLowerInvariant();

        var sorted = field switch
        {
            "description" => descSort
                ? list.OrderByDescending(x => x.Description, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.Description, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.DateTimeStamp),

            "user" => descSort
                ? list.OrderByDescending(x => x.User, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.User, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.DateTimeStamp),

            // Allow common aliases for the timestamp
            "datetimestamp" => descSort
                ? list.OrderByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.DateTimeStamp),

            // Default = most recent first (typical for audit trails)
            _ => list.OrderByDescending(x => x.DateTimeStamp)
        };

        // 3) Pagination safety + apply
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        var skip = (pageNumber - 1) * pageSize;
        return sorted.Skip(skip).Take(pageSize);
    }
}