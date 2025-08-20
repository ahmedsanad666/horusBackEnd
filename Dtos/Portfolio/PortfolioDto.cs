using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BackEnd.Dtos.Portfolio
{
    public class PortfolioDto
    {
        public int Id { get; set; }
        public String Name { get; set; } = String.Empty;
        public String Description { get; set; } = String.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool Status { get; set; } = true;

        public List<PortfolioImageDto> PortfolioImages { get; set; }
        public DateTime PortfolioData { get; set; }
        public string PortfolioLink { get; set; }
        public string BehanceLink { get; set; }
        public string YoutubeLink { get; set; }
        public string GitHubLink { get; set; }
        public List<UserDto> Users { get; set; }
        public string Type { get; set; }
    }

    public class PortfolioCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PortfolioLink { get; set; } = string.Empty;
        public string BehanceLink { get; set; } = string.Empty;
        public string YoutubeLink { get; set; } = string.Empty;
        public string GitHubLink { get; set; } = string.Empty;
        public string PortfolioData { get; set; } = string.Empty; // Changed to string to handle frontend date strings
        public bool? Status { get; set; }
        public List<string> UserIds { get; set; } = new List<string>();
        public string Type { get; set; } = string.Empty;
    }

    public class PortfolioImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class PortfolioUpdateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PortfolioLink { get; set; } = string.Empty;
        public string BehanceLink { get; set; } = string.Empty;
        public string YoutubeLink { get; set; } = string.Empty;
        public string GitHubLink { get; set; } = string.Empty;
        public string PortfolioData { get; set; } = string.Empty; // Changed to string
        public bool? Status { get; set; }
        public List<string> UserIds { get; set; } = new List<string>();
        public string Type { get; set; } = string.Empty;
    }

    public class PortfolioImageCreateDto
    {
        public IFormFile? ImageFile { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserImg { get; set; } = string.Empty;
        public string UserTitle { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CVUrl { get; set; } = string.Empty;
    }
}