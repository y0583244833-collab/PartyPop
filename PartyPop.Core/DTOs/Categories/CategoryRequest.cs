using System.ComponentModel.DataAnnotations;

namespace PartyPop.Core.DTOs.Categories;

/// <summary>משמש גם ליצירה וגם לעדכון.</summary>
public class CategoryRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
