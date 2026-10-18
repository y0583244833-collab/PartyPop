namespace PartyPop.Core.Settings;

public class FileStorageSettings
{
    public const string SectionName = "FileStorageSettings";

    /// <summary>התיקייה הפיזית. ממולאת ב-Program.cs מ-WebRootPath.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>תת-התיקייה והנתיב היחסי ב-URL.</summary>
    public string ImagesFolder { get; set; } = "images";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}
