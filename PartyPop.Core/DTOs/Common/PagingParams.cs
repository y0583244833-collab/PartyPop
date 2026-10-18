using System.ComponentModel.DataAnnotations;

namespace PartyPop.Core.DTOs.Common;

public abstract class PagingParams
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    /// <summary>asc / desc</summary>
    [RegularExpression("^(?i)(asc|desc)$")]
    public string SortDir { get; set; } = "asc";
}
