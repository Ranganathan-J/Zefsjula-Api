using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZefsjulaApi.Exceptions;
using ZefsjulaApi.Models;
using ZefsjulaApi.Models.DTO;
using ZefsjulaApi.Models.Responses;

namespace ZefsjulaApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation("Attempting to register user with email: {Email}", registerDto.Email);

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed - user already exists: {Email}", registerDto.Email);
                throw new ConflictException("User with this email already exists");
            }

            // Create new user
            var user = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                EmailConfirmed = true, // For demo purposes
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("User registration failed for {Email}: {Errors}", registerDto.Email, errors);
                throw new BadRequestException($"Registration failed: {errors}");
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            var authResponse = new AuthResponseDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                Roles = roles.ToList()
            };

            return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "User registered successfully");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login failed - user not found or inactive: {Email}", loginDto.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed for user: {Email}. Reason: {Reason}", 
                    loginDto.Email, 
                    result.IsLockedOut ? "Account locked" : 
                    result.IsNotAllowed ? "Not allowed" : "Invalid credentials");

                if (result.IsLockedOut)
                    throw new UnauthorizedException("Account is locked due to multiple failed login attempts");

                throw new UnauthorizedException("Invalid email or password");
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);

            var authResponse = new AuthResponseDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                Roles = roles.ToList()
            };

            return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Login successful");
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            _logger.LogInformation("Password change attempt for user ID: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive)
            {
                throw new NotFoundException("User not found");
            }

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Password change failed for user {UserId}: {Errors}", userId, errors);
                throw new BadRequestException($"Password change failed: {errors}");
            }

            _logger.LogInformation("Password changed successfully for user ID: {UserId}", userId);
            return ApiResponse<string>.SuccessResult("Password changed successfully", "Password updated");
        }

        public async Task<ApiResponse<UserDto>> GetUserProfileAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive)
            {
                throw new NotFoundException("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            };

            return ApiResponse<UserDto>.SuccessResult(userDto, "User profile retrieved successfully");
        }

        public async Task<ApiResponse<string>> LogoutAsync(int userId)
        {
            _logger.LogInformation("User logout: {UserId}", userId);
            await _signInManager.SignOutAsync();
            return ApiResponse<string>.SuccessResult("Logged out successfully", "Logout successful");
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(GetJwtSecret());

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = GetJwtIssuer(),
                    ValidateAudience = true,
                    ValidAudience = GetJwtAudience(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GenerateJwtToken(User user, IList<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetJwtSecret());

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new("UserId", user.Id.ToString()),
                new("FullName", user.FullName)
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                Issuer = GetJwtIssuer(),
                Audience = GetJwtAudience(),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GetJwtSecret() => _configuration["JWT:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        private string GetJwtIssuer() => _configuration["JWT:Issuer"] ?? "ZefsjulaApi";
        private string GetJwtAudience() => _configuration["JWT:Audience"] ?? "ZefsjulaApiUsers";
        private int GetTokenExpirationHours() => int.Parse(_configuration["JWT:ExpirationHours"] ?? "24");
    }
}