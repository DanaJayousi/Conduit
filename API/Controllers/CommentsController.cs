using API.Models;
using AutoMapper;
using Domain.Article;
using Domain.Comment;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/articles/{articleId:int}/comments")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IArticleRepository _articleRepository;
        private readonly IUserRepository _userRepository;

        public CommentsController(IMapper mapper, IUnitOfWork unitOfWork, IArticleRepository articleRepository,
            IUserRepository userRepository)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }
        
        [HttpGet("{commentId:int}")]
        public async Task<ActionResult<CommentToDisplayDto>> GetCommentById(int articleId, int commentId)
        {
            var article = await _articleRepository.GetArticleWithCommentsAsync(articleId);
            if (article == null) return NotFound("Article Not Found");
            var comment = await _articleRepository.GetCommentByIdAsync(articleId, commentId);
            if (comment == null) return NotFound("Comment Not Found");
            return Ok(_mapper.Map<CommentToDisplayDto>(comment));
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommentToDisplayDto>>> GetAllComments(int articleId)
        {
            var article = await _articleRepository.GetArticleWithCommentsAsync(articleId);
            if (article == null) return NotFound("Article Not Found");
            var comments = await _articleRepository.GetCommentsAsync(articleId);
            return Ok(_mapper.Map<IEnumerable<CommentToDisplayDto>>(comments));
        }

        [Authorize(Policy = "UsersOnly")]
        [HttpPost]
        public async Task<ActionResult<CommentToDisplayDto>> AddComment(int articleId, CommentToInsertDto commentToInsert)
        {
            var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
            var userFromDb = await _userRepository.GetUserWithArticlesAsync(int.Parse(loggedInUserId));
            var articleFromDb = await _articleRepository.GetArticleWithCommentsAsync(articleId);
            if (articleFromDb == null) return NotFound("Article Not Found");
            var comment = _mapper.Map<Comment>(commentToInsert);
            comment.Author = userFromDb;
            comment.PublishDate = DateTime.UtcNow;
            comment.Article = articleFromDb;
            articleFromDb.Comments.Add(comment);
            await _unitOfWork.Commit();
            return CreatedAtAction(nameof(GetCommentById),
                new { articleId = articleFromDb.Id, commentId = comment.Id }, _mapper.Map<CommentToDisplayDto>(comment));
        }
        
        [Authorize(Policy = "UsersOnly")]
        [HttpDelete("{commentId:int}")]
        public async Task<ActionResult> DeleteComment(int articleId, int commentId)
        {
            var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
            var article = await _articleRepository.GetAsync(articleId);
            if (article == null)
                return NotFound("Article Not Found");
            var comment = await _articleRepository.GetCommentByIdAsync(articleId, commentId);
            if (comment == null)
                return NotFound("Comment Not Found");
            if (comment.Author.Id != int.Parse(loggedInUserId))
                return Forbid();
            _articleRepository.DeleteComment(comment);
            await _unitOfWork.Commit();
            return NoContent();
        }
    }
}
