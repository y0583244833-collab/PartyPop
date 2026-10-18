# PartyPop – שרת API להשכרת ציוד לאירועים

## תיאור המערכת

PartyPop היא מערכת להשכרת ציוד לאירועים, כמו טרמפולינות, מתנפחים ומכונות מזון. השרת בנוי ב-ASP.NET Core Web API (.NET 8) מול PostgreSQL.
כל אחד יכול לצפות בקטלוג ולסנן אותו, כולל סינון לפי זמינות בתאריך ובמשמרת (בוקר, צהריים-ערב או יום שלם). לקוח רשום מרכיב סל לתאריך אחד ומשמרת אחת ומאשר אותו להזמנה. אפשר לבטל הזמנה עד 48 שעות לפני תחילת המשמרת. מנהל מנהל קטגוריות ומוצרים וצופה בכל ההזמנות.

הלקוח (React) נמצא ב-repo נפרד: https://github.com/y0583244833-collab/PartyPop.Client

## המשאב המוגבל והטיפול בתחרות עליו

**המשאב:** לכל מוצר יש מספר יחידות מוגבל. הטבלה `ProductDayUsages` שומרת שורה אחת לכל מוצר ותאריך, ובה כמה יחידות כבר תפוסות בבוקר וכמה בצהריים-ערב. הזמנה של יום שלם תופסת את שתי המשמרות.

**איפה התחרות:** כששני לקוחות מאשרים באותו רגע הזמנה של היחידה האחרונה של אותו מוצר, באותו תאריך ובאותה משמרת. שתי הבקשות קוראות "יש יחידה פנויה", ובלי הגנה שתיהן היו מקבלות אישור.

**איך המערכת מגיבה (Optimistic Concurrency):**
- ל-`ProductDayUsage` יש concurrency token: עמודת המערכת `xmin` של PostgreSQL (מוגדרת דרך `uint Version` עם `IsRowVersion()`). EF Core מוסיף אותה לתנאי ה-`WHERE` של כל `UPDATE`.
- כל ה-Checkout רץ בטרנזקציה אחת (`OrderService.CheckoutAsync`): טעינת הסל, בדיקת הזמינות על השורות שנטענו, עדכון המונים, יצירת ההזמנה וריקון הסל, ושמירה אחת בסוף.
- אם מישהו שמר לפנינו, EF Core זורק `DbUpdateConcurrencyException`. ה-`UnitOfWork` מתרגם אותה ל-`ConcurrencyConflictException`, הטרנזקציה מתבטלת (הכול או כלום), נכתב לוג ברמת Warning, והלקוח מקבל **409 Conflict** עם הודעה ברורה: "חלק מהמוצרים נתפסו הרגע על ידי לקוח אחר. נא לנסות שוב."
- אם השורה של המוצר והתאריך עוד לא קיימת ושני לקוחות יוצרים אותה במקביל, אינדקס ייחודי על (ProductId, EventDate) עוצר את השני, וגם הוא מקבל 409.
- אותו מנגנון מגן גם על `Order` (למשל לקוח מבטל ומנהל מסמן "הושלם" באותו רגע) ועל `Cart`.

**ההוכחה:** ב-`PartyPop.IntegrationTests/ConcurrencyTests.cs` שני `DbContext` נפרדים קוראים את אותה שורה ושניהם משנים אותה. הראשון שומר בהצלחה, והשני מקבל חריגה.

## טכנולוגיות

‏.NET 8 · ‏ASP.NET Core Web API · ‏EF Core + ‏Npgsql (PostgreSQL) · ‏JWT · ‏AutoMapper · ‏NLog · ‏xUnit + ‏Moq · ‏Swagger

## מבנה ה-Solution

| פרויקט | תפקיד |
|---|---|
| `PartyPop.Core` | ישויות, DTOs, ממשקים, Enums וחריגות. בלי שום חבילה חיצונית |
| `PartyPop.Data` | ‏DbContext, ‏Fluent API, ‏Repositories, ‏Migrations, ‏Seed |
| `PartyPop.Service` | לוגיקה עסקית: זמינות, מחיר, סל, הזמנות, הרשאות |
| `PartyPop.API` | ‏Controllers, ‏Middleware (שגיאות + CorrelationId), ‏Program.cs |
| `PartyPop.Tests` | בדיקות יחידה ל-Service עם Moq |
| `PartyPop.IntegrationTests` | בדיקות concurrency מול PostgreSQL אמיתי |

## הרצה מקומית

### דרישות
- ‏.NET 8 SDK
- ‏PostgreSQL 16 (מקומי, פורט 5432)
- ‏`dotnet-ef` (רק אם רוצים להריץ migrations ידנית): `dotnet tool install --global dotnet-ef`

### 1. הגדרת סודות (User Secrets)
אין אף סוד בקוד או ב-repo. משורש ה-solution, ב-PowerShell:

```powershell
# חיבור למסד (החליפי את YOUR_PASSWORD בסיסמה של postgres אצלך)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=partypop;Username=postgres;Password=YOUR_PASSWORD" --project PartyPop.API

# מפתח JWT אקראי (64 תווים)
$key = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
dotnet user-secrets set "Jwt:Key" $key --project PartyPop.API

# משתמש מנהל
dotnet user-secrets set "SeedSettings:Admin:FirstName" "Admin" --project PartyPop.API
dotnet user-secrets set "SeedSettings:Admin:LastName" "PartyPop" --project PartyPop.API
dotnet user-secrets set "SeedSettings:Admin:Email" "admin@partypop.local" --project PartyPop.API
dotnet user-secrets set "SeedSettings:Admin:Password" "Admin123!" --project PartyPop.API
dotnet user-secrets set "SeedSettings:Admin:Phone" "0500000000" --project PartyPop.API

# לקוח דמו
dotnet user-secrets set "SeedSettings:DemoCustomer:FirstName" "Demo" --project PartyPop.API
dotnet user-secrets set "SeedSettings:DemoCustomer:LastName" "Customer" --project PartyPop.API
dotnet user-secrets set "SeedSettings:DemoCustomer:Email" "customer@partypop.local" --project PartyPop.API
dotnet user-secrets set "SeedSettings:DemoCustomer:Password" "Customer123!" --project PartyPop.API
dotnet user-secrets set "SeedSettings:DemoCustomer:Phone" "0501111111" --project PartyPop.API
```

### 2. הרצת השרת
```powershell
dotnet run --project PartyPop.API --launch-profile https
```
בעלייה בסביבת Development השרת מריץ את כל ה-Migrations, יוצר את המסד אם הוא לא קיים, ומכניס נתוני התחלה (קטגוריות, מוצרים, מנהל ולקוח דמו). **אין צורך בשום פעולה ידנית במסד.**

‏Swagger: https://localhost:7151/swagger

### 3. הרצת הבדיקות
```powershell
# בדיקות היחידה רצות בלי מסד
dotnet test PartyPop.Tests

# בדיקות האינטגרציה צריכות חיבור למסד בדיקות (נוצר ונמחק אוטומטית בכל הרצה)
dotnet user-secrets set "ConnectionStrings:TestDb" "Host=localhost;Port=5432;Database=partypop_test;Username=postgres;Password=YOUR_PASSWORD" --project PartyPop.IntegrationTests
dotnet test
```

## משתמשי דמו

| Role | אימייל | סיסמה |
|---|---|---|
| Admin | `admin@partypop.local` | `Admin123!` |
| Customer | `customer@partypop.local` | `Customer123!` |

התחברות: `POST /api/auth/tokens`. ב-Swagger לוחצים **Authorize** ומדביקים את ה-token.

## Endpoints עיקריים

| Method | נתיב | הרשאה | תיאור |
|---|---|---|---|
| POST | `/api/users` | כולם | הרשמה |
| GET | `/api/users/me` | מחובר | הפרופיל שלי |
| POST | `/api/auth/tokens` | כולם | התחברות, מחזיר JWT |
| GET | `/api/categories` | כולם | רשימת קטגוריות |
| POST / PUT / DELETE | `/api/categories/{id}` | Admin | ניהול קטגוריות |
| GET | `/api/products` | כולם | קטלוג: סינון, מיון, דפדוף, זמינות לפי תאריך ומשמרת |
| GET | `/api/products/{id}` | כולם | מוצר בודד |
| GET | `/api/products/{id}/availability` | כולם | זמינות מוצר בתאריך |
| POST / PUT / DELETE | `/api/products/{id}` | Admin | ניהול מוצרים (מחיקה רכה) |
| PATCH | `/api/products/{id}/out-of-service` | Admin | עדכון יחידות תקולות |
| PATCH | `/api/products/{id}/activate` | Admin | החזרת מוצר שנמחק |
| POST | `/api/products/{id}/image` | Admin | העלאת תמונה |
| GET / PUT | `/api/carts/me` | Customer | הסל שלי, קביעת תאריך ומשמרת |
| POST / PUT / DELETE | `/api/carts/me/items/{itemId}` | Customer | פריטים בסל |
| POST | `/api/orders` | Customer | אישור הזמנה מהסל (409 בהתנגשות) |
| GET | `/api/orders/me` | Customer | ההזמנות שלי |
| GET | `/api/orders` | Admin | כל ההזמנות: סינון, מיון, דפדוף |
| GET | `/api/orders/{id}` | הבעלים או Admin | הזמנה בודדת |
| PATCH | `/api/orders/{id}/status` | Customer (ביטול) / Admin | ביטול או סימון "הושלם" |

## שגיאות ולוגים

- כל שגיאה חוזרת כ-JSON אחיד: `{ status, message, correlationId }`. בשגיאת ולידציה (400) מתווסף `errors` לפי שדה, וב-409 של Checkout מתווספת רשימת `unavailableItems`.
- כל בקשה מקבלת `X-Correlation-Id`, שמופיע בכל שורות הלוג שלה.
- ‏NLog כותב לקונסול ולקובץ `PartyPop.API/logs/partypop-YYYY-MM-DD.log`: ‏Information לזרימה רגילה, Warning להתנגשות על המשאב, Error לחריגה. סיסמאות ו-tokens לא נכתבים ללוג.
