using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackEnd.Data;
using BackEnd.Interfaces;
using BackEnd.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackEnd.Modules;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using BackEnd.Dtos.Portfolio;
using Microsoft.Extensions.Logging;

namespace BackEnd.Controllers
{
  [Route("api/portfolios")]
  [ApiController]
  public class PortfolioController : ControllerBase
  {
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ApplicationDBContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        IPortfolioRepository portfolioRepository,
        ApplicationDBContext context,
        UserManager<AppUser> userManager,
        ILogger<PortfolioController> logger)
    {
      _portfolioRepository = portfolioRepository;
      _context = context;
      _userManager = userManager;
      _logger = logger;
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
    {
      _logger.LogInformation("PortfolioController test endpoint called");
      return Ok(new { message = "Portfolio API is working!", timestamp = DateTime.UtcNow });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePortfolio([FromBody] PortfolioCreateDto dto)
    {
      try
      {
        _logger.LogInformation("CreatePortfolio endpoint called");

        if (dto == null)
        {
          _logger.LogWarning("CreatePortfolio: DTO is null");
          return BadRequest("Portfolio data is required");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userId == null)
        {
          _logger.LogWarning("CreatePortfolio: User ID not found in token");
          return Unauthorized("User not authenticated");
        }

        _logger.LogInformation($"CreatePortfolio: Creating portfolio for user {userId}");
        _logger.LogInformation($"CreatePortfolio: DTO data - Title: {dto.Title}, Description: {dto.Description}, Type: {dto.Type}");

        // Parse the PortfolioData if it's a string
        DateTime portfolioData;
        if (string.IsNullOrEmpty(dto.PortfolioData))
        {
          // If no date provided, use current date
          portfolioData = DateTime.UtcNow;
          _logger.LogInformation("CreatePortfolio: Using current date for PortfolioData");
        }
        else
        {
          // Try to parse the date string and convert to UTC
          if (DateTime.TryParse(dto.PortfolioData, out DateTime parsedDate))
          {
            // Convert to UTC - this is required for PostgreSQL
            portfolioData = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            _logger.LogInformation($"CreatePortfolio: Parsed date for PortfolioData: {portfolioData} (UTC)");
          }
          else
          {
            // If parsing fails, use current date
            portfolioData = DateTime.UtcNow;
            _logger.LogWarning($"CreatePortfolio: Failed to parse date '{dto.PortfolioData}', using current date");
          }
        }

        var portfolio = new Portfolio
        {
          Name = dto.Title ?? string.Empty,
          Description = dto.Description ?? string.Empty,
          PortfolioLink = dto.PortfolioLink ?? string.Empty,
          BehanceLink = dto.BehanceLink ?? string.Empty,
          YoutubeLink = dto.YoutubeLink ?? string.Empty,
          GitHubLink = dto.GitHubLink ?? string.Empty,
          PortfolioData = portfolioData,
          CreatedAt = DateTime.UtcNow,
          Status = true,
          Type = dto.Type ?? string.Empty
        };

        _logger.LogInformation($"CreatePortfolio: Portfolio object created - Name: {portfolio.Name}, Type: {portfolio.Type}");

        try
        {
          await _context.Portfolios.AddAsync(portfolio);
          _logger.LogInformation("CreatePortfolio: Portfolio added to context");

          await _context.SaveChangesAsync();
          _logger.LogInformation($"CreatePortfolio: Portfolio saved with ID {portfolio.Id}");
        }
        catch (Exception dbEx)
        {
          _logger.LogError(dbEx, "CreatePortfolio: Database error while saving portfolio");
          return StatusCode(500, new { error = "Database error", message = dbEx.Message, details = dbEx.InnerException?.Message });
        }

        // Add creator as portfolio user by default
        var userPortfolios = new List<AppUserPortfolio>
              {
                  new AppUserPortfolio
                  {
                      AppUserId = userId,
                      PortfolioId = portfolio.Id
                  }
              };

        // Add additional users if specified
        if (dto.UserIds != null && dto.UserIds.Any())
        {
          foreach (var id in dto.UserIds)
          {
            if (!string.IsNullOrEmpty(id) && id != userId) // Avoid duplicate
            {
              userPortfolios.Add(new AppUserPortfolio
              {
                AppUserId = id,
                PortfolioId = portfolio.Id
              });
            }
          }
        }

        try
        {
          await _context.AppUserPortfolios.AddRangeAsync(userPortfolios);
          await _context.SaveChangesAsync();
          _logger.LogInformation($"CreatePortfolio: Added {userPortfolios.Count} user associations");
        }
        catch (Exception userEx)
        {
          _logger.LogError(userEx, "CreatePortfolio: Database error while saving user associations");
          return StatusCode(500, new { error = "Database error", message = userEx.Message, details = userEx.InnerException?.Message });
        }

        // Reload the portfolio with related data for the response
        try
        {
          var portfolioWithData = await _context.Portfolios
              .Include(p => p.PortfolioImages)
              .Include(p => p.AppUserPortfolios)
                  .ThenInclude(aup => aup.AppUser)
              .FirstOrDefaultAsync(p => p.Id == portfolio.Id);

          if (portfolioWithData == null)
          {
            _logger.LogError("CreatePortfolio: Failed to reload portfolio data");
            return StatusCode(500, "Failed to create portfolio");
          }

          _logger.LogInformation("CreatePortfolio: Portfolio created successfully");
          return Ok(portfolioWithData.ToPortfolioDto());
        }
        catch (Exception reloadEx)
        {
          _logger.LogError(reloadEx, "CreatePortfolio: Error reloading portfolio data");
          return StatusCode(500, new { error = "Error reloading data", message = reloadEx.Message });
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "CreatePortfolio: Unexpected error occurred");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message, details = ex.InnerException?.Message });
      }
    }

    [HttpGet("my-projects")]
    [Authorize]
    public async Task<IActionResult> GetMyPortfolios()
    {
      try
      {
        _logger.LogInformation("GetMyPortfolios endpoint called");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userId == null)
        {
          _logger.LogWarning("GetMyPortfolios: User ID not found in token");
          return Unauthorized("User not authenticated");
        }

        _logger.LogInformation($"GetMyPortfolios: Fetching portfolios for user {userId}");

        var portfolios = await _context.AppUserPortfolios
            .Where(ap => ap.AppUserId == userId)
            .Include(ap => ap.Portfolio)
                .ThenInclude(p => p.PortfolioImages)
            .Include(ap => ap.Portfolio)
                .ThenInclude(p => p.AppUserPortfolios)
                    .ThenInclude(aup => aup.AppUser)
            .Select(ap => ap.Portfolio)
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        _logger.LogInformation($"GetMyPortfolios: Found {portfolios.Count} portfolios");

        return Ok(
          new
          {
            success = true,
            data = portfolios.Select(p => p.ToPortfolioDto(baseUrl))
          }
        );
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "GetMyPortfolios: Unexpected error occurred");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
      }
    }

    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPortfoliosWithUsers()
    {
      try
      {
        _logger.LogInformation("GetAllPortfoliosWithUsers: Fetching all portfolios with users");

        var portfolios = await _context.Portfolios
            .Include(p => p.PortfolioImages)
            .Include(p => p.AppUserPortfolios)
                .ThenInclude(aup => aup.AppUser)
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        _logger.LogInformation($"GetAllPortfoliosWithUsers: Found {portfolios.Count} portfolios");

        return Ok(new
        {
          success = true,
          data = portfolios.Select(p => p.ToPortfolioDto(baseUrl))
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "GetAllPortfoliosWithUsers: Unexpected error occurred");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
      }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPortfolioById(int id)
    {
      try
      {
        _logger.LogInformation($"GetPortfolioById: Fetching portfolio with ID {id}");

        var portfolio = await _context.Portfolios
            .Include(p => p.PortfolioImages)
            .Include(p => p.AppUserPortfolios)
                .ThenInclude(aup => aup.AppUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (portfolio == null)
        {
          _logger.LogWarning($"GetPortfolioById: Portfolio with ID {id} not found");
          return NotFound("Portfolio not found");
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        _logger.LogInformation($"GetPortfolioById: Portfolio {id} retrieved successfully");
        return Ok(portfolio.ToPortfolioDto(baseUrl));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"GetPortfolioById: Unexpected error occurred for ID {id}");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
      }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdatePortfolio(int id, [FromBody] PortfolioUpdateDto dto)
    {
      try
      {
        _logger.LogInformation($"UpdatePortfolio: Updating portfolio with ID {id}");

        if (dto == null)
        {
          _logger.LogWarning("UpdatePortfolio: DTO is null");
          return BadRequest("Portfolio data is required");
        }

        var portfolio = await _context.Portfolios.FindAsync(id);
        if (portfolio == null)
        {
          _logger.LogWarning($"UpdatePortfolio: Portfolio with ID {id} not found");
          return NotFound("Portfolio not found");
        }

        // Update basic info
        portfolio.Name = dto.Title ?? portfolio.Name;
        portfolio.Description = dto.Description ?? portfolio.Description;
        portfolio.PortfolioLink = dto.PortfolioLink ?? portfolio.PortfolioLink;
        portfolio.BehanceLink = dto.BehanceLink ?? portfolio.BehanceLink;
        portfolio.YoutubeLink = dto.YoutubeLink ?? portfolio.YoutubeLink;
        portfolio.GitHubLink = dto.GitHubLink ?? portfolio.GitHubLink;

        // Parse the PortfolioData string to DateTime
        if (!string.IsNullOrEmpty(dto.PortfolioData))
        {
          if (DateTime.TryParse(dto.PortfolioData, out DateTime parsedDate))
          {
            // Convert to UTC - this is required for PostgreSQL
            portfolio.PortfolioData = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            _logger.LogInformation($"UpdatePortfolio: Updated PortfolioData to {portfolio.PortfolioData} (UTC)");
          }
        }

        portfolio.Status = dto.Status ?? portfolio.Status;
        portfolio.Type = dto.Type ?? portfolio.Type;

        // Update associated users if provided
        if (dto.UserIds != null)
        {
          // Remove existing user associations
          var existingUsers = await _context.AppUserPortfolios
              .Where(aup => aup.PortfolioId == id)
              .ToListAsync();

          _context.AppUserPortfolios.RemoveRange(existingUsers);

          // Add new user associations
          var newUserPortfolios = dto.UserIds
              .Where(userId => !string.IsNullOrEmpty(userId))
              .Select(userId => new AppUserPortfolio
              {
                AppUserId = userId,
                PortfolioId = id
              });

          await _context.AppUserPortfolios.AddRangeAsync(newUserPortfolios);
        }

        await _context.SaveChangesAsync();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        _logger.LogInformation($"UpdatePortfolio: Portfolio {id} updated successfully");
        return Ok(portfolio.ToPortfolioDto(baseUrl));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"UpdatePortfolio: Unexpected error occurred for ID {id}");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
      }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePortfolio(int id)
    {
      try
      {
        _logger.LogInformation($"DeletePortfolio: Deleting portfolio with ID {id}");

        var portfolio = await _context.Portfolios
            .Include(p => p.PortfolioImages)
            .Include(p => p.AppUserPortfolios)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (portfolio == null)
        {
          _logger.LogWarning($"DeletePortfolio: Portfolio with ID {id} not found");
          return NotFound("Portfolio not found");
        }

        // Remove all related entities
        _context.PortfolioImages.RemoveRange(portfolio.PortfolioImages);
        _context.AppUserPortfolios.RemoveRange(portfolio.AppUserPortfolios);
        _context.Portfolios.Remove(portfolio);

        await _context.SaveChangesAsync();
        _logger.LogInformation($"DeletePortfolio: Portfolio {id} deleted successfully");
        return NoContent();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"DeletePortfolio: Unexpected error occurred for ID {id}");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
      }
    }

    [HttpPost("{portfolioId}/images")]
    [Authorize]
    public async Task<IActionResult> AddPortfolioImage(int portfolioId, [FromForm] PortfolioImageCreateDto dto)
    {
      try
      {
        _logger.LogInformation($"AddPortfolioImage: Adding image to portfolio {portfolioId}");

        var portfolio = await _context.Portfolios.FindAsync(portfolioId);
        if (portfolio == null)
        {
          _logger.LogWarning($"AddPortfolioImage: Portfolio with ID {portfolioId} not found");
          return NotFound("Portfolio not found");
        }

        if (dto?.ImageFile == null || dto.ImageFile.Length == 0)
        {
          _logger.LogWarning("AddPortfolioImage: No file uploaded");
          return BadRequest("No file was uploaded.");
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        var fileExtension = Path.GetExtension(dto.ImageFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
          _logger.LogWarning($"AddPortfolioImage: Invalid file type {fileExtension}");
          return BadRequest("Invalid file type. Only image files are allowed.");
        }

        // Generate unique filename
        var fileName = Guid.NewGuid().ToString() + fileExtension;
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

        // Ensure directory exists
        if (!Directory.Exists(uploadPath))
        {
          Directory.CreateDirectory(uploadPath);
          _logger.LogInformation($"AddPortfolioImage: Created upload directory {uploadPath}");
        }

        var filePath = Path.Combine(uploadPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await dto.ImageFile.CopyToAsync(stream);
        }

        // Generate URL for the saved image
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var imageUrl = $"{baseUrl}/images/{fileName}";

        var image = new PortfolioImage
        {
          ImageUrl = imageUrl,
          PortfolioId = portfolioId
        };

        await _context.PortfolioImages.AddAsync(image);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"AddPortfolioImage: Image added successfully for portfolio {portfolioId}");
        return Ok(image.ToPortfolioImageDto());
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"AddPortfolioImage: Unexpected error occurred for portfolio {portfolioId}");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
      }
    }

    [HttpGet("{portfolioId}/images")]
    public async Task<IActionResult> GetPortfolioImages(int portfolioId)
    {
      try
      {
        _logger.LogInformation($"GetPortfolioImages: Fetching images for portfolio {portfolioId}");

        var images = await _context.PortfolioImages
            .Where(pi => pi.PortfolioId == portfolioId)
            .ToListAsync();

        _logger.LogInformation($"GetPortfolioImages: Found {images.Count} images for portfolio {portfolioId}");
        return Ok(images.Select(i => i.ToPortfolioImageDto()));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"GetPortfolioImages: Unexpected error occurred for portfolio {portfolioId}");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
      }
    }
  }
}