using Baby_Clinic.Data.Repositories;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Baby_Clinic.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        // ב-Render ה-connection string מגיע כ-URL (postgresql://user:pass@host:port/db), ש-Npgsql לא קורא
        var csBuilder = connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
            ? FromUrl(connectionString)
            : new NpgsqlConnectionStringBuilder(connectionString);

        // בפיתוח הסיסמה לא נשמרת בקבצי appsettings - מגיעה מ-User Secrets
        var password = configuration["Database:Password"];
        if (!string.IsNullOrEmpty(password))
            csBuilder.Password = password;

        // AddDbContext רושם את ה-DbContext כ-Scoped
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(csBuilder.ConnectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChildRepository, ChildRepository>();
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IVisitTypeRepository, VisitTypeRepository>();

        return services;
    }

    private static NpgsqlConnectionStringBuilder FromUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Prefer
        };
    }
}
