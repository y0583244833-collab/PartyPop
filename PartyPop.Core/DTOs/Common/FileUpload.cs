namespace PartyPop.Core.DTOs.Common;

/// <summary>קובץ שהועלה, בלי IFormFile (שייך ל-ASP.NET). ה-Controller ממיר אליו.</summary>
public record FileUpload(Stream Content, string FileName, string ContentType, long Length);
