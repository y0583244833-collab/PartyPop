# שלב 3 – הרצה אצלך (מתיקיית PartyPop)

```powershell
# 1. כלי ה-Migrations (פעם אחת במחשב)
dotnet tool install --global dotnet-ef --version 8.0.11

# 2. Connection String ב-User Secrets (להחליף את הסיסמה בסיסמת ה-postgres שלך)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=partypop;Username=postgres;Password=YOUR_PASSWORD" --project PartyPop.API

# 3. Migration ראשון
dotnet ef migrations add InitialCreate --project PartyPop.Data --startup-project PartyPop.API

# 4. הרצה: המסד נוצר אוטומטית ו-Swagger נפתח
dotnet run --project PartyPop.API --launch-profile https
```
