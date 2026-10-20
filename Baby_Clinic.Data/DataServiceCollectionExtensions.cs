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

        // הסיסמה לא נשמרת בקבצי appsettings - מגיעה מ-User Secrets
        var csBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Password = configuration["Database:Password"]
        };

        // AddDbContext רושם את ה-DbContext כ-Scoped
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(csBuilder.ConnectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChildRepository, ChildRepository>();
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IVisitTypeRepository, VisitTypeRepository>();

        return services;
    }
}
