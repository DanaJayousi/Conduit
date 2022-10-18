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

    [Fact]
    public void ValidateArticleProfile()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => { cfg.AddProfile(new ArticleProfile()); });

        IMapper mapper = new Mapper(mapperConfig);

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
    [Fact]
    public void ValidateCommentProfile()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => { cfg.AddProfile(new CommentProfile()); });

        IMapper mapper = new Mapper(mapperConfig);

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
}