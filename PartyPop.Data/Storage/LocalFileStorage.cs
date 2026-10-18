using Microsoft.Extensions.Options;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Settings;

namespace PartyPop.Data.Storage;

/// <summary>
/// שומר תמונות בתיקייה wwwroot/images בשרת. במסד נשמר רק הנתיב היחסי.
/// בעתיד אפשר להחליף למימוש ענן בלי לגעת ב-Service.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly FileStorageSettings _settings;

    public LocalFileStorage(IOptions<FileStorageSettings> settings) => _settings = settings.Value;

    private string ImagesDirectory => Path.Combine(_settings.RootPath, _settings.ImagesFolder);

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct)
    {
        Directory.CreateDirectory(ImagesDirectory);

        // שם ייחודי (Guid): לא דורסים קובץ קיים, ולא משתמשים בשם שהמשתמש שלח
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(ImagesDirectory, fileName);

        await using (var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(file, ct);
        }

        return $"/{_settings.ImagesFolder}/{fileName}";
    }

    public void Delete(string relativeUrl)
    {
        // רק שם הקובץ, כדי שנתיב כמו ../../ לא ימחק קבצים מחוץ לתיקייה
        var fileName = Path.GetFileName(relativeUrl);
        if (string.IsNullOrEmpty(fileName))
            return;

        var fullPath = Path.Combine(ImagesDirectory, fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
