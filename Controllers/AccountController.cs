using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackEnd.Dtos.account;
using BackEnd.Interfaces;
using BackEnd.Modules;
using BackEnd.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackEnd.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ITokenService tokenService, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "Horus API",
                Version = "1.0.0"
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                _logger.LogInformation("Register endpoint called");

                if (registerDto == null)
                {
                    _logger.LogWarning("Register: DTO is null");
                    return BadRequest("Registration data is required");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Register: Model state is invalid");
                    return BadRequest(ModelState);
                }

                // Validate the role exists
                var validRoles = new[] { "Developer", "Designer", "Marketing", "MotionGraphic" };
                if (!validRoles.Contains(registerDto.Role))
                {
                    _logger.LogWarning($"Register: Invalid role specified: {registerDto.Role}");
                    return BadRequest("Invalid role specified");
                }

                _logger.LogInformation($"Register: Creating user with email {registerDto.Email}");

                var appUser = new AppUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    Name = registerDto.Name ?? string.Empty,
                    Role = registerDto.Role
                };

                var createdUser = await _userManager.CreateAsync(appUser, registerDto.Password);

                if (createdUser.Succeeded)
                {
                    _logger.LogInformation($"Register: User created successfully with ID {appUser.Id}");

                    // Ensure role exists
                    if (!await _roleManager.RoleExistsAsync(registerDto.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(registerDto.Role));
                        _logger.LogInformation($"Register: Created role {registerDto.Role}");
                    }

                    // Assign role to user
                    var roleResult = await _userManager.AddToRoleAsync(appUser, registerDto.Role);

                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError($"Register: Failed to assign role {registerDto.Role} to user {appUser.Id}");
                        return BadRequest(new { Errors = roleResult.Errors.Select(e => e.Description) });
                    }

                    _logger.LogInformation($"Register: User {appUser.Id} registered successfully");
                    return Ok(new
                    {
                        Message = "User registered successfully",
                        User = new
                        {
                            appUser.Id,
                            appUser.Name,
                            appUser.Email,
                            appUser.Role,
                        },
                        token = _tokenService.CreateToken(appUser),
                    });
                }
                else
                {
                    _logger.LogError($"Register: Failed to create user. Errors: {string.Join(", ", createdUser.Errors.Select(e => e.Description))}");
                    return BadRequest(new { Errors = createdUser.Errors.Select(e => e.Description) });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Register: Unexpected error occurred");
                return StatusCode(500, new { Message = "An error occurred while registering user", Error = e.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("Login endpoint called");

                if (loginDto == null)
                {
                    _logger.LogWarning("Login: DTO is null");
                    return BadRequest("Login data is required");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login: Model state is invalid");
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Login: Attempting login for email {loginDto.Email}");

                var user = await _userManager.FindByEmailAsync(loginDto.Email);

                if (user == null)
                {
                    _logger.LogWarning($"Login: User not found for email {loginDto.Email}");
                    return Unauthorized("Invalid email or password");
                }

                var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);

                if (!passwordValid)
                {
                    _logger.LogWarning($"Login: Invalid password for user {user.Id}");
                    return Unauthorized("Invalid email or password");
                }

                _logger.LogInformation($"Login: User {user.Id} logged in successfully");
                return Ok(new
                {
                    Message = "Login successful",
                    User = new
                    {
                        user.Id,
                        user.Name,
                        user.Email,
                        user.Role,
                        user.UserTitle,
                        user.PhoneNumber,
                        user.CVUrl
                    },
                    Token = _tokenService.CreateToken(user)
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Login: Unexpected error occurred");
                return StatusCode(500, new { Message = "An error occurred while logging in", Error = e.Message });
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("GetAllUsers endpoint called");

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var users = await _userManager.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Email,
                        u.UserTitle,
                        u.PhoneNumber,
                        u.CVUrl,
                        UserImg = !string.IsNullOrEmpty(u.UserImg) ? baseUrl + u.UserImg : null
                    })
                    .ToListAsync();

                _logger.LogInformation($"GetAllUsers: Found {users.Count} users");
                return Ok(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllUsers: Unexpected error occurred");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}