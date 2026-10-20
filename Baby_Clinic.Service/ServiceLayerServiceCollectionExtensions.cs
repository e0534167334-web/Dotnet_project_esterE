using Baby_Clinic.Service.Mapping;
using Baby_Clinic.Service.Security;
using Baby_Clinic.Service.Services;
using Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Baby_Clinic.Service;

public static class ServiceLayerServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        // Scoped: תלויים ב-repositories שהם Scoped
        services.AddScoped<ISlotService, SlotService>();
        services.AddScoped<IChildService, ChildService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IAuthService, AuthService>();

        // Singleton: חסרי מצב ולא מחזיקים שום שירות Scoped
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
