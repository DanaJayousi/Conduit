using API.Models;
using AutoMapper;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.JsonPatch;
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

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<UserToDisplayDto>> GetUserById(int userId)
    {
        var user = await _userRepository.GetAsync(userId);
        if (user == null) return NotFound();
        return Ok(_mapper.Map<UserToDisplayDto>(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserToDisplayDto>> AddUser(UserForUpsertDto user)
    {
        if (await _userRepository.GetUserByEmailAsync(user.Email) != null)
            return Conflict();
        var storedUser = _mapper.Map<User>(user);
        await _userRepository.AddAsync(storedUser);
        await _unitOfWork.Commit();
        return CreatedAtAction(nameof(GetUserById), new { userId = storedUser.Id },
            _mapper.Map<UserToDisplayDto>(storedUser));
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult<UserToDisplayDto>> UpdateUser(int userId,
        UserForUpsertDto userToUpdate)
    {
        var userFromDb = await _userRepository.GetAsync(userId);
        if (userFromDb == null)
            return NotFound();
        var possibleEmailConflict = await _userRepository.GetUserByEmailAsync(userToUpdate.Email);
        if (possibleEmailConflict != null && possibleEmailConflict.Id != userId)
            return Conflict();
        _mapper.Map(userToUpdate, userFromDb);
        await _unitOfWork.Commit();
        return NoContent();
    }

    [HttpPatch("{userId:int}")]
    public async Task<ActionResult> PartiallyUpdateUser(int userId,
        JsonPatchDocument<UserForUpsertDto> patchDocument)
    {
        var userFromDb = await _userRepository.GetAsync(userId);
        if (userFromDb == null) return NotFound();
        var userToPatch = _mapper.Map<UserForUpsertDto>(
            userFromDb);
        patchDocument.ApplyTo(userToPatch);
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!TryValidateModel(userToPatch)) return BadRequest(ModelState);

        _mapper.Map(userToPatch, userFromDb);
        await _unitOfWork.Commit();
        return NoContent();
    }
}