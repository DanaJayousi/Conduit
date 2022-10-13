using API.Models;
using AutoMapper;
using Domain.Article;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/articles")]
[ApiController]
public class ArticlesController : ControllerBase
{
    private readonly IArticleRepository _articleRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public ArticlesController(IMapper mapper, IUnitOfWork unitOfWork, IArticleRepository articleRepository,
        IUserRepository userRepository)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _articleRepository = articleRepository;
        _userRepository = userRepository;
    }

    [HttpGet("{articleId:int}")]
    public async Task<ActionResult<ArticleToDisplayDto>> GetArticleById(int articleId)
    {
        var article = await _articleRepository.GetArticleWithoutCommentsAsync(articleId);
        if (article == null) return NotFound();
        return Ok(_mapper.Map<ArticleToDisplayDto>(article));
    }

    [Authorize(Policy = "UsersOnly")]
    [HttpPost]
    public async Task<ActionResult<ArticleToUpsertDto>> AddArticle(ArticleToUpsertDto articleToUpsertDto)
    {
        var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
        var userFromDb = await _userRepository.GetUserWithArticlesAsync(int.Parse(loggedInUserId));
        var addedArticle = _mapper.Map<Article>(articleToUpsertDto);
        addedArticle.Author = userFromDb;
        addedArticle.PublishDate = DateTime.UtcNow;
        addedArticle.LastUpdated = DateTime.UtcNow;
        await _articleRepository.AddAsync(addedArticle);
        await _unitOfWork.Commit();
        return CreatedAtAction(nameof(GetArticleById),
            new { articleId = addedArticle.Id }, _mapper.Map<ArticleToDisplayDto>(addedArticle));
    }

    [Authorize(Policy = "UsersOnly")]
    [HttpPut("{articleId:int}")]
    public async Task<ActionResult<ArticleToDisplayDto>> UpdateArticle(int articleId,
        ArticleToUpsertDto articleToUpsert)
    {
        var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
        var article = await _articleRepository.GetArticleWithoutCommentsAsync(articleId);
        if (article == null)
            return NotFound();
        if (article.Author.Id != int.Parse(loggedInUserId))
            return Forbid();
        _mapper.Map(articleToUpsert, article);
        article.LastUpdated = DateTime.UtcNow.AddDays(5);
        await _unitOfWork.Commit();
        return NoContent();
    }

    [Authorize(Policy = "UsersOnly")]
    [HttpDelete("{articleId:int}")]
    public async Task<ActionResult> DeleteArticle(int articleId)
    {
        var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
        var article = await _articleRepository.GetArticleWithoutCommentsAsync(articleId);
        if (article == null)
            return NotFound();
        if (article.Author.Id != int.Parse(loggedInUserId))
            return Forbid();
        _articleRepository.Remove(article);
        await _unitOfWork.Commit();
        return NoContent();
    }
}