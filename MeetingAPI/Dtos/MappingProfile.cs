using AutoMapper;
using MeetingAPI.Dtos;
using MeetingAPI.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MeetingAPI.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapování User <=> UserDto
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ReverseMap();

            // Mapování MeetingRecurrence <=> MeetingRecurrenceDto
            CreateMap<MeetingRecurrence, MeetingRecurrenceDto>().ReverseMap();

            // Mapování MeetingParticipant <=> MeetingParticipantDto
            CreateMap<MeetingParticipant, MeetingParticipantDto>().ReverseMap();

            // Mapování Meeting <=> MeetingDto
            CreateMap<Meeting, MeetingDto>()
                .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants.Select(p => p.User)))
                .ReverseMap()
                .ForMember(dest => dest.Participants, opt => opt.Ignore());
            
            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>();
            CreateMap<UpdateUserDto, User>().ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
