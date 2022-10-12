using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Models;
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
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public AuthenticationController(IUnitOfWork unitOfWork, IUserRepository userRepository, IMapper mapper,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    [HttpPost]
    public async Task<ActionResult<string>> Authenticate(
        AuthenticationDto authenticationDto)
    {
        var user = await ValidateUserCredentialsAsync(authenticationDto.Email, authenticationDto.Password);
        if (user == null) return Unauthorized();
        var securityKey =
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Authentication:SecretForKey"]));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claimsForToken = new List<Claim> { new("sub", user.Id.ToString()) };
        var jwtSecurityToken = new JwtSecurityToken(
            _configuration["Authentication:Issuer"],
            _configuration["Authentication:Audience"],
            claimsForToken,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(10),
            signingCredentials);
        var tokenToReturn = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        return Ok(tokenToReturn);
    }

    private async Task<User?> ValidateUserCredentialsAsync(string email, string password)
    {
        var user = await _userRepository.GetUserByEmailAsync(email);
        if (user == null) return user;
        return user.Password == password ? user : null;
    }
}