using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Models;
using API.Services;
using AutoMapper;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

[Route("api/authentication")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
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
    public async Task<ActionResult<string>> SignIn(
        AuthenticationDto authenticationDto)
    {
        var user = await _userRepository.ValidateUserCredentialsAsync(authenticationDto.Email, authenticationDto.Password);
        if (user == null) return Unauthorized();
        var claimsForToken = new List<Claim> { new("userId", user.Id.ToString()) };
        var accessTokenToReturn = _tokenService.GenerateAccessToken(claimsForToken,
            _configuration["Authentication:SecretForKey"],
            _configuration["Authentication:Issuer"],
            _configuration["Authentication:Audience"]);
        return Ok(accessTokenToReturn);
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
}