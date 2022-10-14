using API.Models;
using AutoMapper;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
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
        var user = await _userRepository.GetUserWithFollowAsync(userId);
        if (user == null) return NotFound();
        return Ok(_mapper.Map<UserToDisplayDto>(user));
    }

    [Authorize(Policy = "UsersOnly")]
    [HttpPut("{userId:int}")]
    public async Task<ActionResult<UserToDisplayDto>> UpdateUser(int userId,
        UserForUpsertDto userToUpdate)
    {
        var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
        if (loggedInUserId != userId.ToString()) return Forbid();

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

    [Authorize(Policy = "UsersOnly")]
    [HttpPatch("{userId:int}")]
    public async Task<ActionResult> PartiallyUpdateUser(int userId,
        JsonPatchDocument<UserForUpsertDto> patchDocument)
    {
        var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
        if (loggedInUserId != userId.ToString()) return Forbid();

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

    [Authorize(Policy = "UsersOnly")]
    [HttpPost("{userId:int}/follow/{followingId:int}")]
    public async Task<ActionResult> Follow(int userId, int followingId)
    {
        var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
        if (loggedInUserId != userId.ToString()) return Forbid();
        if (userId == followingId) return BadRequest();
        var userFromDb = await _userRepository.GetUserWithFollowAsync(userId);
        if (userFromDb == null) return NotFound();
        var followingFromDb = await _userRepository.GetUserWithFollowAsync(followingId);
        if (followingFromDb == null) return NotFound();
        _userRepository.Follow(followingFromDb, userFromDb);
        await _unitOfWork.Commit();
        return Ok();
    }

    [HttpPost("{userId:int}/unfollow/{followingId:int}")]
    public async Task<ActionResult> Unfollow(int userId, int followingId)
    {
        var loggedInUserId = User.Claims.FirstOrDefault(claim => claim.Type == "userId")?.Value;
        if (loggedInUserId != userId.ToString()) return Forbid();
        if (userId == followingId) return BadRequest();
        var userFromDb = await _userRepository.GetUserWithFollowAsync(userId);
        if (userFromDb == null) return NotFound();
        var followingFromDb = await _userRepository.GetUserWithFollowAsync(followingId);
        if (followingFromDb == null) return NotFound();
        _userRepository.Unfollow(followingFromDb, userFromDb);
        await _unitOfWork.Commit();
        return Ok();
    }
}