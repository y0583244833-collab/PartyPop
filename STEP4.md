# שלב 4 – מה לעשות אצלך

## 1. לחלץ את step4-API.zip לתיקיית partypop (לאשר החלפה)

## 2. להוסיף ל-PartyPop.API.csproj, לפני `</Project>`:
```xml
<ItemGroup>
  <Content Update="nlog.config" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```
(ב-Visual Studio: לחיצה ימנית על PartyPop.API → Edit Project File)

## 3. להוסיף בסוף הקובץ `.gitignore` שבתיקייה הראשית:
```
# NLog
logs/
# תמונות שהועלו
PartyPop.API/wwwroot/images/
```

## 4. להריץ
```powershell
dotnet run --project PartyPop.API --launch-profile https
```

## 5. בדיקה
- בטרמינל מופיעות שורות בפורמט: `תאריך | INFO | - | Program | PartyPop API started`
- לפתוח https://localhost:7151/swagger (יש כפתור Authorize למעלה – ישמש משלב 5)
- בתיקייה `PartyPop.API\logs` נוצר קובץ `partypop-2026-10-18.log`
- בקובץ יש שורות `HTTP GET /swagger/index.html started` ו-`finished 200`, עם מזהה (Guid) בעמודה השלישית
