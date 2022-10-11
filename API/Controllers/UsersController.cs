using API.Models;
using AutoMapper;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public UsersController(IUnitOfWork unitOfWork, IUserRepository userRepository, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserToDisplayDto>> GetUserById(int userId)
    {
        var user = await _userRepository.GetAsync(userId);
        if (user == null) return NotFound();
        return Ok(_mapper.Map<UserToDisplayDto>(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserToDisplayDto>> AddUser(UserForCreationDto user)
    {
        var storedUser = _mapper.Map<User>(user);
        await _userRepository.AddAsync(storedUser);
        await _unitOfWork.Commit();
        return CreatedAtAction(nameof(GetUserById), new { userId = storedUser.Id },
            _mapper.Map<UserToDisplayDto>(storedUser));
    }
}