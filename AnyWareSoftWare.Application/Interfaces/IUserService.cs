using System.Collections.Generic;
using System.Threading.Tasks;
using AnyWareSoftWare.Application.DTOs;

namespace AnyWareSoftWare.Application.Interfaces
{
    public interface IUserService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<UserDto> GetCurrentUserAsync(int userId);

        Task<string> CreateUserAsync(RegisterDto dto, string role);
        Task<bool> DeleteUserAsync(int userId);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
    }
}
