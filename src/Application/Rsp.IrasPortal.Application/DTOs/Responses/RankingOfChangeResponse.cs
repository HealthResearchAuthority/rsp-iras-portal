namespace Rsp.Portal.Application.DTOs.Responses;

public class RankingOfChangeResponse
{
    public CategoryRank Categorisation { get; set; } = new();

    public ModificationRank ModificationType { get; set; } = new();

    public string ReviewType { get; set; } = null!;
}

public class CategoryRank
{
    public string Category { get; set; } = null!;

    public int Order { get; set; }
}

public class ModificationRank
{
    public string Substantiality { get; set; } = null!;

    public int Order { get; set; }
}