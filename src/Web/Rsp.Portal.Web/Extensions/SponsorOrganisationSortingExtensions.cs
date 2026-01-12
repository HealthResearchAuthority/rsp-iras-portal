using System.Diagnostics.CodeAnalysis;
using Rsp.Portal.Application.DTOs;

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
}