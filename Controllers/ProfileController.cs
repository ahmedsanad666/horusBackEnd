using System.Threading.Tasks;
using BackEnd.Dtos.Profile;
using BackEnd.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackEnd.Data;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using BackEnd.Mappers;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace BackEnd.Controllers
{
    [Route("api/profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDBContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(UserManager<AppUser> userManager, ApplicationDBContext context, ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            _logger.LogInformation("Test endpoint called");
            return Ok(new { message = "API is working!", timestamp = DateTime.UtcNow });
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                _logger.LogInformation("Health check endpoint called");

                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation($"Database connection test: {canConnect}");

                // Test user count
                var userCount = await _userManager.Users.CountAsync();
                _logger.LogInformation($"User count: {userCount}");

                // Check if tables exist
                var tables = new List<string>();
                try
                {
                    var portfoliosCount = await _context.Portfolios.CountAsync();
                    tables.Add($"Portfolios: {portfoliosCount} records");
                }
                catch (Exception ex)
                {
                    tables.Add($"Portfolios: ERROR - {ex.Message}");
                }

                try
                {
                    var usersCount = await _context.Users.CountAsync();
                    tables.Add($"Users: {usersCount} records");
                }
                catch (Exception ex)
                {
                    tables.Add($"Users: ERROR - {ex.Message}");
                }

                try
                {
                    var appUserPortfoliosCount = await _context.AppUserPortfolios.CountAsync();
                    tables.Add($"AppUserPortfolios: {appUserPortfoliosCount} records");
                }
                catch (Exception ex)
                {
                    tables.Add($"AppUserPortfolios: ERROR - {ex.Message}");
                }

                return Ok(new
                {
                    status = "healthy",
                    databaseConnected = canConnect,
                    userCount = userCount,
                    tables = tables,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<ProfileDto>> GetProfile()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (userId == null)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullImageUrl = !string.IsNullOrEmpty(user.UserImg)
                ? baseUrl + user.UserImg
                : null;
            var dto = new ProfileDto
            {
                Id = user.Id,
                Bio = user.Bio,
                FaceBook = user.FaceBook,
                Instgram = user.Instgram,
                Behance = user.Behance,
                GitHub = user.GitHub,
                Email = user.Email,
                UserName = user.Name,
                UserImg = fullImageUrl,
                Role = user.Role,
                UserTitle = user.UserTitle,
                PhoneNumber = user.PhoneNumber,
                CVUrl = user.CVUrl,
            };
            return Ok(dto);
        }



        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                        User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (userId == null)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Update basic profile fields
            user.Bio = updateDto.Bio;
            user.FaceBook = updateDto.FaceBook;
            user.Instgram = updateDto.Instgram;
            user.Behance = updateDto.Behance;
            user.GitHub = updateDto.GitHub;
            user.UserTitle = updateDto.UserTitle;
            user.CVUrl = updateDto.CVUrl;

            // Update name if provided
            if (!string.IsNullOrWhiteSpace(updateDto.Name))
                user.Name = updateDto.Name;

            // Update phone number if provided
            if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
                user.PhoneNumber = updateDto.PhoneNumber;

            // Handle email change
            if (!string.IsNullOrWhiteSpace(updateDto.Email) && user.Email != updateDto.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, updateDto.Email);
                if (!setEmailResult.Succeeded)
                    return BadRequest(setEmailResult.Errors);

                var setUserNameResult = await _userManager.SetUserNameAsync(user, updateDto.Email);
                if (!setUserNameResult.Succeeded)
                    return BadRequest(setUserNameResult.Errors);
            }


            // Handle password change
            if (!string.IsNullOrWhiteSpace(updateDto.NewPassword))
            {
                // Verify current password first
                if (string.IsNullOrWhiteSpace(updateDto.CurrentPassword))
                {
                    return BadRequest("Current password is required to change password");
                }

                // Verify current password is correct
                var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, updateDto.CurrentPassword);
                if (!isCurrentPasswordValid)
                {
                    return BadRequest(new { error = "Current password is incorrect" });
                }

                // Change password
                var changeResult = await _userManager.ChangePasswordAsync(
                    user,
                    updateDto.CurrentPassword,
                    updateDto.NewPassword);

                if (!changeResult.Succeeded)
                {
                    return BadRequest(changeResult.Errors);
                }
            }

            // Save all changes
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(updateResult.Errors);

            return Ok(new { Message = "Profile updated successfully" });
        }

        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (userId == null)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (request.Image == null || request.Image.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{Guid.NewGuid()}_{request.Image.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            // Optionally: delete old image file if exists
            if (!string.IsNullOrEmpty(user.UserImg))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.UserImg.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }
            var imagePath = $"/images/{fileName}";
            user.UserImg = imagePath;
            await _userManager.UpdateAsync(user);

            // Create full URL
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullImageUrl = baseUrl + imagePath;

            return Ok(new { userImg = fullImageUrl });
        }

        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProfiles()
        {
            try
            {
                _logger.LogInformation("GetAllProfiles endpoint called");

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                _logger.LogInformation($"Base URL: {baseUrl}");

                var users = await _userManager.Users.ToListAsync();
                _logger.LogInformation($"Found {users.Count} users");

                var profiles = users.Select(user => new ProfileDto
                {
                    Id = user.Id,
                    Bio = user.Bio,
                    FaceBook = user.FaceBook,
                    Instgram = user.Instgram,
                    Behance = user.Behance,
                    GitHub = user.GitHub,
                    Email = user.Email,
                    UserName = user.Name,
                    UserImg = !string.IsNullOrEmpty(user.UserImg) ? baseUrl + user.UserImg : null,
                    Role = user.Role,
                    UserTitle = user.UserTitle,
                    PhoneNumber = user.PhoneNumber,
                    CVUrl = user.CVUrl,
                }).ToList();

                _logger.LogInformation($"Returning {profiles.Count} profiles");
                return Ok(new { profiles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllProfiles");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous] // Or [Authorize] if you want to restrict access
        public async Task<IActionResult> GetProfileById(string id)
        {
            // Get user by id
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound();

            // Get user's portfolios
            // You need access to ApplicationDBContext for this
            // So inject ApplicationDBContext _context in the controller constructor
            var portfolios = await _context.AppUserPortfolios
                .Where(ap => ap.AppUserId == id)
                .Include(ap => ap.Portfolio)
                    .ThenInclude(p => p.PortfolioImages)
                .Include(ap => ap.Portfolio)
                    .ThenInclude(p => p.AppUserPortfolios)
                        .ThenInclude(aup => aup.AppUser)
                .Select(ap => ap.Portfolio)
                .ToListAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var profileDto = new ProfileDto
            {
                Id = user.Id,
                Bio = user.Bio,
                FaceBook = user.FaceBook,
                Instgram = user.Instgram,
                Behance = user.Behance,
                GitHub = user.GitHub,
                Email = user.Email,
                UserName = user.Name,
                UserImg = !string.IsNullOrEmpty(user.UserImg) ? baseUrl + user.UserImg : null,
                Role = user.Role,
                UserTitle = user.UserTitle,
                PhoneNumber = user.PhoneNumber,
                CVUrl = user.CVUrl,
            };

            return Ok(new
            {
                profile = profileDto,
                portfolios = portfolios.Select(p => p.ToPortfolioDto(baseUrl))
            });
        }
    }
}