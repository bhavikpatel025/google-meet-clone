using AutoMapper;
using VideoCallApp.Application.DTOs.Auth;
using VideoCallApp.Application.DTOs.Chat;
using VideoCallApp.Application.DTOs.Meeting;
using VideoCallApp.Domain.Entities;

namespace VideoCallApp.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, AuthResponseDto>()
            .ForMember(dest => dest.Token, opt => opt.Ignore())
            .ForMember(dest => dest.Expiration, opt => opt.Ignore());

        // Meeting mappings
        CreateMap<CreateMeetingRequestDto, Meeting>()
            .ForMember(dest => dest.MeetingCode, opt => opt.Ignore())
            .ForMember(dest => dest.HostId, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Title) ? "Instant meeting" : src.Title.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Description) ? null : src.Description.Trim()));

        CreateMap<Meeting, MeetingResponseDto>()
            .ForMember(dest => dest.HostName, opt => opt.MapFrom(src =>
                src.Host.FirstName + " " + src.Host.LastName))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CurrentParticipants, opt => opt.MapFrom(src =>
                src.Participants.Count(p => p.LeftAt == null)));

        // Participant mappings
        CreateMap<MeetingParticipant, ParticipantDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                src.User.FirstName + " " + src.User.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        // Chat mappings
        CreateMap<ChatMessage, ChatMessageDto>()
            .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src =>
                src.Sender.FirstName + " " + src.Sender.LastName))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));
    }
}
