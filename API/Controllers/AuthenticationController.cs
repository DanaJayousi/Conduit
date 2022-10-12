using System.Security.Claims;
using API.Models;
using API.Services;
using AutoMapper;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/authentication")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public AuthenticationController(IUnitOfWork unitOfWork, IUserRepository userRepository, IMapper mapper,
        IConfiguration configuration, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    [HttpPost("signIn")]
    public async Task<ActionResult<Token>> SignIn(
        AuthenticationDto authenticationDto)
    {
        var user = await _userRepository.ValidateUserCredentialsAsync(authenticationDto.Email,
            authenticationDto.Password);
        if (user == null) return Unauthorized();
        var claimsForToken = new List<Claim> { new("userId", user.Id.ToString()) };
        var accessTokenToReturn = _tokenService.GenerateAccessToken(claimsForToken,
            _configuration["Authentication:SecretForKey"],
            _configuration["Authentication:Issuer"],
            _configuration["Authentication:Audience"]);
        var refreshTokenToReturn = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshTokenToReturn;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(10);
        await _unitOfWork.Commit();
        return Ok(new Token
        {
            accessToken = accessTokenToReturn,
            refreshToken = refreshTokenToReturn
        });
    }

    [HttpPost("signUp")]
    public async Task<ActionResult<UserToDisplayDto>> SignUp(UserForUpsertDto user)
    {
        if (await _userRepository.GetUserByEmailAsync(user.Email) != null)
            return Conflict();
        var storedUser = _mapper.Map<User>(user);
        await _userRepository.AddAsync(storedUser);
        await _unitOfWork.Commit();
        return CreatedAtAction(nameof(UsersController.GetUserById), "Users", new { userId = storedUser.Id },
            _mapper.Map<UserToDisplayDto>(storedUser));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<Token>> Refresh(
        Token? userToken)
    {
        if (userToken is null)
            return BadRequest("Invalid client request");
        var accessToken = userToken.accessToken;
        var refreshToken = userToken.refreshToken;
        var userIdFromAccessToken =
            _tokenService.GetUserIdFromAccessToken(accessToken, _configuration["Authentication:SecretForKey"]);
        var user = await _userRepository.GetAsync(userIdFromAccessToken);
        if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            return BadRequest("Invalid client request");
        var claimsForToken = new List<Claim> { new("userId", user.Id.ToString()) };
        var newAccessToken = _tokenService.GenerateAccessToken(claimsForToken,
            _configuration["Authentication:SecretForKey"],
            _configuration["Authentication:Issuer"],
            _configuration["Authentication:Audience"]);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        await _unitOfWork.Commit();
        return Ok(new Token
        {
            accessToken = newAccessToken,
            refreshToken = refreshToken
        });
    }

    [HttpPost]
    [Authorize(Policy = "UsersOnly")]
    [Route("logout")]
    public async Task<ActionResult> Logout()
    {
        var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
        var user = await _userRepository.GetAsync(int.Parse(loggedInUserId));
        if (user == null) return BadRequest();
        user.RefreshToken = string.Empty;
        await _unitOfWork.Commit();
        return NoContent();
    }
}