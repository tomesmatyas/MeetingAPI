using AutoMapper;
using MeetingAPI.Dtos;
using MeetingAPI.Models;

namespace MeetingAPI.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User <=> UserDto
        CreateMap<User, UserDto>().ReverseMap();
        CreateMap<CreateUserDto, User>();
        CreateMap<UpdateUserDto, User>().ForMember(dest => dest.Id, opt => opt.Ignore());

        // MeetingRecurrence <=> DTO
        CreateMap<MeetingRecurrence, MeetingRecurrenceDto>().ReverseMap();

        // Participant <=> DTO (včetně User)
        CreateMap<MeetingParticipant, MeetingParticipantDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ReverseMap();

        // Meeting <=> DTO (včetně Title, Recurrence, Participants)
        CreateMap<Meeting, MeetingDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants))
            .ReverseMap()
            .ForMember(dest => dest.Participants, opt => opt.Ignore()); // API nepřijímá Participants přímo
    }
}
