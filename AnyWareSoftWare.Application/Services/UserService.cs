using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AnyWareSoftWare.Application.DTOs;
using AnyWareSoftWare.Application.Exceptions;
using AnyWareSoftWare.Application.Interfaces;
using AnyWareSoftWare.Domain.Entities;
using AnyWareSoftWare.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AnyWareSoftWare.Application.Services
{
    public class UserService : IUserService
    {
        public const string AdminRole = "Admin";
        public const string UserRole = "User";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IUnitOfWork unitOfWork,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _config = config;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            return await CreateUserAsync(dto, UserRole);
        }

        public async Task<string> CreateUserAsync(RegisterDto dto, string role)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                throw new ConflictException("Email already exists.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new AppException(400, string.Join("; ", result.Errors.Select(e => e.Description)));

            await EnsureRoleExistsAsync(role);
            await _userManager.AddToRoleAsync(user, role);

            return "User created successfully.";
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                throw new UnauthorizedException("Invalid credentials.");

            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var stored = await _unitOfWork.RefreshTokens.GetByExpressionSingleAsync(
                rt => rt.Token == refreshToken,
                new System.Linq.Expressions.Expression<Func<RefreshToken, object>>[] { rt => rt.User });

            if (stored == null || !stored.IsActive)
                throw new UnauthorizedException("Invalid or expired refresh token.");

            stored.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.RefreshTokens.UpdateAsync(stored);

            return await BuildAuthResponseAsync(stored.User);
        }

        public async Task<UserDto> GetCurrentUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new NotFoundException("User not found.");

            return await MapToDtoAsync(user);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var dtos = new List<UserDto>();
            foreach (var user in users)
                dtos.Add(await MapToDtoAsync(user));
            return dtos;
        }


        private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60"));
            var accessToken = await GenerateJwtTokenAsync(user, expiresAt);
            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = expiresAt
            };
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, DateTime expiresAt)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var keyStr = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> CreateRefreshTokenAsync(int userId)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
            {
                Token = token,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            return token;
        }

        private async Task EnsureRoleExistsAsync(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        private async Task<UserDto> MapToDtoAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? UserRole,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
