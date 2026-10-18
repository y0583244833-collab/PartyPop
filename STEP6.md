# שלב 6 – קטלוג: קטגוריות ומוצרים

## מה נוסף

| שכבה | קובץ | תפקיד |
|---|---|---|
| Core | `IAvailabilityCalculator` | נוסף `FullDayFactor` (2 × (1 − 5%) = 1.9) |
| Data | `CategoryRepository` | גישה לטבלת Categories |
| | `ProductRepository` | הקטלוג: סינון, מיון ודפדוף **בתוך השאילתה** |
| | `UnitOfWork` | שם קטגוריה כפול במקביל → 409 |
| Service | `AvailabilityCalculator` | מחיר יום שלם (וזמינות, לשלב 8) |
| | `CategoryService` | רשימה, יצירה, עדכון, מחיקה (409 אם יש מוצרים) |
| | `ProductService` | קטלוג ומוצר בודד. שאר הפעולות בשלבים 7–8 |
| | `CatalogProfile` | AutoMapper: ‏Category ו-Product ל-DTO |
| API | `CategoriesController` | `GET` לכולם; `POST/PUT/DELETE` למנהל בלבד |
| | `ProductsController` | `GET /api/products`, `GET /api/products/{id}` |

## נקודות להסביר למורה
- **הכול בשאילתה אחת:** `Where` → `Count` → `OrderBy` → `Skip/Take` → `ToListAsync`. לא טוענים את כל הטבלה לזיכרון.
- **`AsNoTracking`** בקריאה בלבד: מהיר יותר, EF לא עוקב אחרי השינויים.
- **`ThenBy(Id)`**: סדר יציב, כדי שמוצר לא יופיע בשני עמודים.
- **`pageSize` עד 50** (`[Range]`), אחרת 400.
- **מחיר יום שלם לא נשמר במסד**, הוא מחושב ב-Service מ-`PricePerShift` ומ-`appsettings`.
- **סינון מחיר/תקציב לפי משמרת:** אם `shift=FullDay`, משווים למחיר יום שלם.
- **חיפוש** עם `ILIKE` (לא תלוי באותיות גדולות), ותווים `%` ו-`_` נחשבים תו רגיל.
- **הקטלוג מציג רק מוצרים פעילים** (`Active`).
- **`date` בלי `shift` → 400.** סינון לפי זמינות בתאריך ייכנס בשלב 8.
- **אין שינוי במבנה המסד**, ולכן אין Migration בשלב הזה.

## הוראות
1. לחלץ את `step6-Catalog.zip` לתיקיית `partypop` (לאשר החלפה).
2. להריץ:
```powershell
Get-Process PartyPop.API -ErrorAction SilentlyContinue | Stop-Process
dotnet run --project PartyPop.API --launch-profile https
```
3. לבדוק ב-Swagger:

| # | בקשה | צפוי |
|---|---|---|
| 1 | `GET /api/categories` | 200, ארבע קטגוריות |
| 2 | `GET /api/products` | 200, ‏`totalCount: 10`, עמוד ראשון עם 10 מוצרים |
| 3 | `GET /api/products?categoryId=2&sortBy=price&sortDir=desc` | שלושת המתנפחים, מהיקר לזול |
| 4 | `GET /api/products?pageSize=3&page=2` | 3 מוצרים, ‏`totalPages: 4` |
| 5 | `GET /api/products?search=מכונת` | שלוש המכונות |
| 6 | `GET /api/products?shift=FullDay&budget=500` | 4 מוצרים (מחיר יום שלם עד 500) |
| 7 | `GET /api/products?pageSize=100` | 400 |
| 8 | `GET /api/products/999` | 404 |
| 9 | `POST /api/categories` **בלי** Authorize | 401 |
| 10 | התחברות כלקוח, ואז `POST /api/categories` | 403 |
| 11 | התחברות כמנהל, ‏`POST /api/categories` עם הגוף למטה | 201 |
| 12 | שוב אותו `POST` | 409 |
| 13 | `DELETE /api/categories/1` (יש בה מוצרים) | 409 |
| 14 | `DELETE /api/categories/{id}` של החדשה | 204 |

התחברות (להעתיק כמו שזה):
```json
{ "email": "admin@partypop.local", "password": "Admin123!" }
```
```json
{ "email": "customer@partypop.local", "password": "Customer123!" }
```
גוף לסעיף 11:
```json
{ "name": "בועות סבון", "description": "מכונות בועות לאירועים" }
```

4. לשמור ב-Git:
```powershell
git add .
git commit -m "Step 6: catalog - categories and products with filter, sort, paging"
git push
```
