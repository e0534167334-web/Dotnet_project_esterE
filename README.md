# טיפת חלב – מערכת לקביעת תורים (Baby Clinic API)

<!-- שלב 10 – README והגשה -->

מערכת Web API ב-.NET 8 לניהול תורים בתחנת טיפת חלב. צוות התחנה (Admin) פותח **משבצות תור** אצל אחות בשעה מסוימת, ומסמן לאילו סוגי ביקור כל משבצת פתוחה (חיסון, שקילה ומדידה וכו'). הורים (Parent) נרשמים, מוסיפים את הילדים שלהם, מחפשים משבצות פנויות (סינון ו-pagination) וקובעים או מבטלים תורים. השרת בנוי בארכיטקטורת שכבות (Core / Data / Service / API) מול PostgreSQL. ההזדהות היא עם JWT ושני roles, והלוגים נכתבים עם NLog ו-CorrelationId.

הלקוח (React) נמצא ב-repository נפרד: `Baby_Clinic_Client`.

---

## המשאב המוגבל והתחרות עליו

**המשאב:** משבצת תור (`Slot`). לכל משבצת אפשר לקבוע תור אחד בלבד.

**איפה נוצרת התחרות:** שני הורים רואים את אותה משבצת פנויה ולוחצים על "קביעת תור" כמעט באותו רגע. כל בקשה טוענת את המשבצת, רואה שהיא `Available`, משנה אותה ל-`Booked` ושומרת. בלי הגנה, שתי הבקשות היו מצליחות ושני הורים היו מקבלים אישור לאותו תור.

**איך המערכת מגיבה (Optimistic Concurrency):**
1. **Token:** ל-`Slot` יש שדה `Version` מסוג `uint`. ב-Fluent API הוא מוגדר `IsRowVersion()`, ו-Npgsql ממפה אותו לעמודת המערכת `xmin` של PostgreSQL. הערך הזה משתנה בכל עדכון של השורה (`SlotConfiguration.cs`).
2. **בדיקה בזמן השמירה:** EF מוסיף לפקודת ה-UPDATE את התנאי `WHERE xmin = <הערך שנטען>`. הבקשה הראשונה מצליחה ו-`xmin` משתנה. אצל הבקשה השנייה אף שורה לא מתעדכנת, ו-EF זורק `DbUpdateConcurrencyException`.
3. **תרגום החריגה:** ה-repository בשכבת Data תופס את `DbUpdateConcurrencyException` וזורק במקומה `ConcurrencyConflictException` שמוגדרת ב-Core. כך שכבת ה-Service לא תלויה ב-EF (`Repository.cs`).
4. **409 + לוג:** `AppointmentService` תופס את החריגה, כותב שורת **Warning** ללוג (עם ה-CorrelationId של הבקשה) ומחזיר `Result` מסוג Conflict. ה-Controller מתרגם אותו ל-**HTTP 409** עם הודעה ברורה. ה-409 מתועד ב-Swagger.
5. **בלקוח:** מוצג חלון שמסביר שהורה אחר תפס את המשבצת, ורשימת המשבצות מתרעננת מיד.

**ההוכחה:** הקובץ `Baby_Clinic.Tests/Concurrency/SlotConcurrencyTests.cs` פותח שני מופעי `AppDbContext` נפרדים, ושניהם טוענים את אותה משבצת ומסמנים אותה כתפוסה. השמירה הראשונה מצליחה, והשנייה זורקת `DbUpdateConcurrencyException`. הבדיקה רצה מול PostgreSQL אמיתי, כי InMemory ו-SQLite לא תומכים ב-`xmin`.

**בדיקה ידנית:** שתי בקשות `POST /api/appointments` לאותה משבצת, בהפרש של רגע. אחת מחזירה `201`, השנייה `409`, ובלוג מופיעה שורה כזו:

```
2026-10-21 08:18:40.5804|WARN |019a5e70-9dfd-48c0-8db0-7ab246e7eeee|AppointmentService|Booking conflict: slot 2 is already booked (child 3).
```

---

## הרצה מקומית

### דרישות
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL מותקן ורץ, עם משתמש שמורשה ליצור בסיס נתונים (למשל `postgres`)

### 1. Connection string (בלי סיסמה)
בקובץ `Baby_Clinic/appsettings.Development.json` יש Host, Port, Database ו-Username בלבד. אם המשתמש שלך ב-PostgreSQL הוא לא `postgres`, צריך לעדכן שם את `Username`.

### 2. סודות ב-User Secrets
אף סוד לא נמצא בקוד או ב-repository. מריצים מתיקיית ה-solution:

```bash
# סיסמת מסד הנתונים של המשתמש שב-appsettings.Development.json
dotnet user-secrets set "Database:Password" "<הסיסמה>" --project Baby_Clinic

# מפתח החתימה של ה-JWT: לפחות 32 תווים (HS256). השרת לא יעלה עם מפתח קצר יותר
dotnet user-secrets set "Jwt:Key" "<מחרוזת אקראית ארוכה, 32 תווים ומעלה>" --project Baby_Clinic
```

### 3. הרצה
```bash
dotnet run --project Baby_Clinic --launch-profile http
```
בעליית השרת ה-migrations וה-seed רצים **אוטומטית**: בסיס הנתונים נוצר, ונוספים סוגי ביקור, משתמשי דמו, ילד ומשבצות לשבוע הקרוב. אין צורך להריץ `dotnet ef`.

Swagger: http://localhost:5013/swagger. להתחבר דרך `POST /api/tokens`, להעתיק את ה-token וללחוץ על **Authorize**.

### 4. הרצת הבדיקות
בדיקת התחרות משתמשת **בבסיס נתונים נפרד** (שם הבסיס חייב לכלול `test`). הבסיס נוצר בתחילת הריצה ונמחק בסופה. ה-connection string המלא נשמר ב-User Secrets של פרויקט הבדיקות, או במשתנה הסביבה `ConnectionStrings__TestDb`:

```bash
dotnet user-secrets set "ConnectionStrings:TestDb" "Host=localhost;Port=5432;Database=baby_clinic_tests;Username=postgres;Password=<הסיסמה>" --project Baby_Clinic.Tests

dotnet test
```

| קבוצה | קובץ | מה נבדק |
|---|---|---|
| תחרות (שלב 7) | `Concurrency/SlotConcurrencyTests.cs` | שני DbContext על אותה משבצת: הראשון שומר, השני נכשל. תרגום החריגה ב-repository |
| שירות התורים | `Services/AppointmentServiceTests.cs` | הצלחה, משבצת תפוסה, התנגשות (+Warning), לא נמצא, עבר, מכסה, ילד של הורה אחר, ביטול של הורה אחר |
| שירות המשבצות | `Services/SlotServiceTests.cs` | יצירה, מחיקת משבצת תפוסה, העברת pagination ל-repository |
| שירות ההרשמה | `Services/AuthServiceTests.cs` | מייל כפול, סיסמה שגויה |

### משתמשי דמו

| Role | מייל | סיסמה |
|---|---|---|
| Admin (צוות התחנה) | `admin@babyclinic.local` | `Admin123!` |
| Parent (הורה, עם ילדה בשם נועה) | `parent@babyclinic.local` | `Parent123!` |

---

## Endpoints

| Method | נתיב | הרשאה | תיאור | סטטוסים |
|---|---|---|---|---|
| POST | `/api/users` | ללא | הרשמה (תמיד Parent) | 201, 400, 409 |
| POST | `/api/tokens` | ללא | התחברות, מחזיר JWT | 200, 400, 401 |
| GET | `/api/slots` | מחובר | חיפוש: `from`, `to`, `visitTypeId`, `availableOnly`, `page`, `pageSize` | 200, 400, 401 |
| GET | `/api/slots/{id}` | מחובר | משבצת בודדת | 200, 401, 404 |
| POST | `/api/slots` | Admin | יצירת משבצת | 201, 400, 401, 403 |
| PUT | `/api/slots/{id}` | Admin | עדכון משבצת | 204, 400, 401, 403, 404, 409 |
| DELETE | `/api/slots/{id}` | Admin | מחיקה (רק משבצת בלי תורים) | 204, 401, 403, 404, 409 |
| GET | `/api/visit-types` | מחובר | סוגי הביקור | 200, 401 |
| GET | `/api/children` | Parent | הילדים שלי | 200, 401, 403 |
| POST | `/api/children` | Parent | הוספת ילד | 201, 400, 401, 403 |
| GET | `/api/appointments` | מחובר | Parent: התורים שלו. Admin: כל התורים | 200, 401 |
| POST | `/api/appointments` | Parent | קביעת תור | 201, 400, 401, 403, 404, **409** |
| PATCH | `/api/appointments/{id}` | מחובר | ביטול (`{"status":"Cancelled"}`) | 204, 400, 401, 403, 404, 409 |

שגיאות לא צפויות מחזירות 500 עם JSON אחיד ו-`correlationId`, בלי stack trace. כל תשובה כוללת header בשם `X-Correlation-ID`.

---

## מבנה הפרויקט

| פרויקט | תפקיד | תלויות |
|---|---|---|
| `Baby_Clinic.Core` | Entities, Enums, DTOs, Interfaces, `Result`, חריגות, `JwtSettings` | אין |
| `Baby_Clinic.Data` | `AppDbContext`, Fluent API, 2 migrations, repositories, seed | Core, Npgsql EF |
| `Baby_Clinic.Service` | לוגיקה עסקית, AutoMapper, JWT, hash לסיסמאות | Core |
| `Baby_Clinic` (API) | Controllers דקים, Middleware (שגיאות, CorrelationId), NLog, CORS, Swagger | Core, Service, Data (לרישום בלבד) |
| `Baby_Clinic.Tests` | xUnit + Moq, ובדיקת התחרות מול PostgreSQL | Core, Data, Service |

## פרסום ב-Render (רשות)
`render.yaml` ו-`Dockerfile` מגדירים שירות web ובסיס PostgreSQL. כל push ל-`main` מפעיל deploy אוטומטי. `Jwt__Key` נוצר ב-Render, וה-connection string מגיע מבסיס הנתונים.
