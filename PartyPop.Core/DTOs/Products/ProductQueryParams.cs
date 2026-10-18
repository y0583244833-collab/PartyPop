using System.ComponentModel.DataAnnotations;
using PartyPop.Core.DTOs.Common;
using PartyPop.Core.Enums;

namespace PartyPop.Core.DTOs.Products;

public class ProductQueryParams : PagingParams
{
    public int? CategoryId { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    public decimal? MaxPrice { get; set; }

    /// <summary>תקציב למשמרת (מחיר המוצר במשמרת שנבחרה).</summary>
    [Range(typeof(decimal), "0", "99999999")]
    public decimal? Budget { get; set; }

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    /// <summary>date + shift יחד: רק מוצרים זמינים.</summary>
    public DateOnly? Date { get; set; }
    public ShiftType? Shift { get; set; }

    /// <summary>מנהל בלבד: להציג גם מוצרים מושבתים (Inactive). ללקוח מתעלמים מזה.</summary>
    public bool IncludeInactive { get; set; }

    /// <summary>price / name</summary>
    [RegularExpression("^(?i)(price|name)$")]
    public string SortBy { get; set; } = "name";
}
