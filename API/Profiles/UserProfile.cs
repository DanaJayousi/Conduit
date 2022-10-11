using API.Models;
using AutoMapper;
using Domain.User;

namespace API.Profiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<UserForUpsertDto, User>();
        CreateMap<User, UserToDisplayDto>()
            .ForMember(dest => dest.Name,
                opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.FollowingCount,
                opt => opt.MapFrom(src => src.Following.Count))
            .ForMember(dest => dest.FollowersCount,
                opt => opt.MapFrom(src => src.Followers.Count));
    }
}