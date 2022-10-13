using API.Models;
using AutoMapper;
using Domain.Article;

namespace API.Profiles;

public class ArticleProfile : Profile
{
    public ArticleProfile()
    {
        CreateMap<Article, ArticleToDisplayDto>()
            .ForMember(dto => dto.AuthorName,
                opt => opt.MapFrom(article => article.Author.FullName))
            .ForMember(dto => dto.PublishDate,
                opt => opt.MapFrom(article => article.PublishDate.ToShortDateString()))
            .ForMember(dto => dto.LastUpdated,
                opt => opt.MapFrom(article => article.LastUpdated.ToShortDateString()));
        CreateMap<ArticleToUpsertDto, Article>();
    }
}