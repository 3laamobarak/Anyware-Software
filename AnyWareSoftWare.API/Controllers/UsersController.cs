using System.Security.Claims;
using System.Threading.Tasks;
using AnyWareSoftWare.Application.DTOs;
using AnyWareSoftWare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnyWareSoftWare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _userService.RegisterAsync(dto);
            return Ok(new { Message = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var auth = await _userService.LoginAsync(dto);
            return Ok(auth);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
        {
            var auth = await _userService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(auth);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _userService.GetCurrentUserAsync(userId);
            return Ok(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/users")]
        public async Task<IActionResult> CreateUser(RegisterDto dto)
        {
            var result = await _userService.CreateUserAsync(dto, "User");
            return Ok(new { Message = result });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result) return NotFound();
            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
    }
}