using API.Models;
using AutoMapper;
using Domain.Article;

namespace API.Profiles;

public class ArticleProfile : Profile
{
    public ArticleProfile()
    {
        CreateMap<Article, ArticleToDisplayDto>(MemberList.Destination)
            .ForMember(dto => dto.AuthorName,
                opt => opt.MapFrom(article => article.Author.FullName))
            .ForMember(dto => dto.PublishDate,
                opt => opt.MapFrom(article => article.PublishDate.ToShortDateString()))
            .ForMember(dto => dto.LastUpdated,
                opt => opt.MapFrom(article => article.LastUpdated.ToShortDateString()))
            .ForMember(dto => dto.FavoritedCount,
                opt => opt.MapFrom(article => article.FavoriteArticle.Count));
        CreateMap<ArticleToUpsertDto, Article>(MemberList.Source);
    }
}