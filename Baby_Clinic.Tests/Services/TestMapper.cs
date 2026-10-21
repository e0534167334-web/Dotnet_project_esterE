using AutoMapper;
using Baby_Clinic.Service.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baby_Clinic.Tests.Services;

// שלב 8 – ה-Profile האמיתי של האפליקציה (ולא Mock), כדי שהבדיקות יתפסו גם מיפוי שבור
internal static class TestMapper
{
    public static IMapper Create()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }
}
