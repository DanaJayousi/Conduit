using API.Profiles;
using AutoMapper;

namespace UnitTests;

public class MapperProfilesTests
{
    [Fact]
    public void ValidateUserProfile()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => { cfg.AddProfile(new UserProfile()); });

        IMapper mapper = new Mapper(mapperConfig);

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
}