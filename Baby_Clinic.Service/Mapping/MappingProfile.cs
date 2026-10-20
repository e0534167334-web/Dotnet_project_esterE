using AutoMapper;
using Core.DTOs;
using Core.Entities;

namespace Baby_Clinic.Service.Mapping;

// Entity -> Response ו-Request -> Entity. ישויות לא יוצאות מהשכבה
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<VisitType, VisitTypeResponse>();

        CreateMap<Slot, SlotResponse>();
        CreateMap<CreateSlotRequest, Slot>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.Version, o => o.Ignore())
            .ForMember(d => d.Appointments, o => o.Ignore())
            // סוגי הביקור נטענים מה-repository לפי VisitTypeIds
            .ForMember(d => d.VisitTypes, o => o.Ignore());

        CreateMap<Child, ChildResponse>();
        CreateMap<CreateChildRequest, Child>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Appointments, o => o.Ignore());

        // ChildName ו-SlotStartTimeUtc ממופים אוטומטית (flattening)
        CreateMap<Appointment, AppointmentResponse>()
            .ForMember(d => d.NurseName, o => o.MapFrom(s => s.Slot!.NurseName));
    }
}
