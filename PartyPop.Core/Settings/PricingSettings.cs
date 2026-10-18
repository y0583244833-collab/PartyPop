namespace PartyPop.Core.Settings;

public class PricingSettings
{
    public const string SectionName = "PricingSettings";

    /// <summary>הנחה ליום שלם, באחוזים.</summary>
    public decimal FullDayDiscountPercent { get; set; } = 5;
}
