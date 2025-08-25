using ZefsjulaApi.Models;
using ZefsjulaApi.Models.DTO;
using ZefsjulaApi.Models.Responses;

namespace ZefsjulaApi.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);
        Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<ApiResponse<UserDto>> GetUserProfileAsync(int userId);
        Task<ApiResponse<string>> LogoutAsync(int userId);
        Task<bool> ValidateTokenAsync(string token);
        string GenerateJwtToken(User user, IList<string> roles);
    }
}