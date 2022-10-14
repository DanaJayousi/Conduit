using API.Models;
using AutoMapper;
using Domain.Article;
using Domain.Comment;

namespace API.Profiles;

public class CommentProfile : Profile
{
    public CommentProfile()
    {
        CreateMap<Comment, CommentToDisplayDto>()
            .ForMember(dto => dto.AuthorName,
                opt => opt.MapFrom(comment => comment.Author.FullName))
            .ForMember(dto => dto.ArticleTitle,
                opt => opt.MapFrom(comment => comment.Article.Title))
            .ForMember(dto => dto.PublishDate,
                opt => opt.MapFrom(comment => comment.PublishDate.ToShortDateString()));
        CreateMap<CommentToInsertDto, Comment>();
    }
}