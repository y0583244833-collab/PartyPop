namespace PartyPop.Core.Settings;

public class ShiftSettings
{
    public const string SectionName = "ShiftSettings";

    public TimeOnly MorningStart { get; set; } = new(8, 0);
    public TimeOnly MorningEnd { get; set; } = new(14, 0);
    public TimeOnly AfternoonEveningStart { get; set; } = new(14, 0);
    public TimeOnly AfternoonEveningEnd { get; set; } = new(20, 0);
}
