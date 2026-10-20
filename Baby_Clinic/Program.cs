using System.Text;
using System.Text.Json.Serialization;
using Baby_Clinic.Data;
using Baby_Clinic.Data.Seed;
using Baby_Clinic.Middleware;
using Baby_Clinic.Service;
using Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;

// logger לשלב העלייה, לפני שה-DI קיים
var bootstrapLogger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog במקום ה-providers המובנים; בקוד ממשיכים לכתוב רק דרך ILogger
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.

    // enums נשלחים ומתקבלים כטקסט ("Cancelled") ולא כמספר
    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // כפתור Authorize שמקבל את ה-token מ-POST /api/tokens
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "הדביקו כאן את ה-token בלבד (בלי המילה Bearer)"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // DbContext + repositories (ה-connection string מהקונפיגורציה, הסיסמה מ-User Secrets)
    builder.Services.AddDataLayer(builder.Configuration);

    // שירותי הלוגיקה העסקית, AutoMapper, hash לסיסמאות ויצירת JWT
    builder.Services.AddServiceLayer();

    // Issuer, Audience ותוקף מ-appsettings; Jwt:Key מ-User Secrets.
    // HS256 דורש מפתח של לפחות 256 ביט (32 בתים); עדיף להיכשל בעלייה ולא בהתחברות הראשונה
    builder.Services.AddOptions<JwtSettings>()
        .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
        .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer) && !string.IsNullOrWhiteSpace(s.Audience) && s.ExpiryMinutes > 0,
            "Jwt:Issuer, Jwt:Audience and Jwt:ExpiryMinutes must be set in appsettings.")
        .Validate(s => Encoding.UTF8.GetByteCount(s.Key) >= 32,
            "Jwt:Key in User Secrets is missing or shorter than 32 bytes (HS256 requires 256 bits).")
        .ValidateOnStart();

    // ולידציה של ה-token: ה-claims (מזהה ו-role) מגיעים ל-Controllers דרך User
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
            };
        });
    builder.Services.AddAuthorization();

    // הלקוח (React, שלב 9) רץ על origin אחר; ה-origins מוגדרים ב-appsettings
    const string ClientCorsPolicy = "Client";
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options => options.AddPolicy(ClientCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

    var app = builder.Build();

    // Configure the HTTP request pipeline (הסדר לפי התוכנית).
    // טיפול בשגיאות ראשון, כדי לתפוס גם חריגות מה-middlewares שאחריו
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        // migrate + נתוני דמו אם הבסיס ריק
        await DbInitializer.InitializeAsync(app.Services);

        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseCors(ClientCorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // HostAbortedException: עצירה רגילה של dotnet ef
{
    // שגיאה בעליית השרת (למשל Jwt:Key חסר) נכתבת גם לקובץ הלוג
    bootstrapLogger.Error(ex, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

