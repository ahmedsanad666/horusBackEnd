using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BackEnd.Dtos.Portfolio;
using BackEnd.Modules;

namespace BackEnd.Mappers
{
    public static class PortfolioMapper
    {
        public static PortfolioDto ToPortfolioDto(this Portfolio portfolioModel, string baseUrl = null)
        {
            return new PortfolioDto
            {
                Id = portfolioModel.Id,
                Name = portfolioModel.Name,
                Description = portfolioModel.Description,
                CreatedAt = portfolioModel.CreatedAt,
                Status = portfolioModel.Status,
                PortfolioData = portfolioModel.PortfolioData,
                PortfolioLink = portfolioModel.PortfolioLink,
                BehanceLink = portfolioModel.BehanceLink,
                YoutubeLink = portfolioModel.YoutubeLink,
                GitHubLink = portfolioModel.GitHubLink,
                PortfolioImages = portfolioModel.PortfolioImages?.Select(pi => pi.ToPortfolioImageDto()).ToList(),
                Users = portfolioModel.AppUserPortfolios?.Where(aup => aup.AppUser != null).Select(aup => new UserDto
                {
                    Id = aup.AppUser.Id,
                    UserName = aup.AppUser.UserName,
                    Name = aup.AppUser.Name,
                    Role = aup.AppUser.Role,
                    UserImg = !string.IsNullOrEmpty(aup.AppUser.UserImg) && baseUrl != null ? baseUrl + aup.AppUser.UserImg : aup.AppUser.UserImg,
                    UserTitle = aup.AppUser.UserTitle,
                    PhoneNumber = aup.AppUser.PhoneNumber,
                    CVUrl = aup.AppUser.CVUrl
                }).ToList(),
                Type = portfolioModel.Type
            };
        }

        public static PortfolioImageDto ToPortfolioImageDto(this PortfolioImage portfolioImage)
        {
            return new PortfolioImageDto
            {
                Id = portfolioImage.Id,
                ImageUrl = portfolioImage.ImageUrl
            };
        }

        public static Portfolio ToPortfolioFromCreateDto(this PortfolioCreateDto portfolioCreateDto)
        {
            // Parse the PortfolioData string to DateTime
            DateTime portfolioData;
            if (string.IsNullOrEmpty(portfolioCreateDto.PortfolioData))
            {
                portfolioData = DateTime.UtcNow;
            }
            else if (!DateTime.TryParse(portfolioCreateDto.PortfolioData, out DateTime parsedDate))
            {
                portfolioData = DateTime.UtcNow;
            }
            else
            {
                // Convert to UTC - this is required for PostgreSQL
                portfolioData = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }

            return new Portfolio
            {
                Name = portfolioCreateDto.Title,
                Description = portfolioCreateDto.Description,
                PortfolioLink = portfolioCreateDto.PortfolioLink,
                BehanceLink = portfolioCreateDto.BehanceLink,
                YoutubeLink = portfolioCreateDto.YoutubeLink,
                GitHubLink = portfolioCreateDto.GitHubLink,
                PortfolioData = portfolioData,
                Status = portfolioCreateDto.Status ?? true,
                CreatedAt = DateTime.UtcNow,
                Type = portfolioCreateDto.Type
            };
        }

        public static Portfolio ToPortfolioFromUpdateDto(this PortfolioUpdateDto portfolioUpdateDto, Portfolio existingPortfolio)
        {
            existingPortfolio.Name = portfolioUpdateDto.Title ?? existingPortfolio.Name;
            existingPortfolio.Description = portfolioUpdateDto.Description ?? existingPortfolio.Description;
            existingPortfolio.PortfolioLink = portfolioUpdateDto.PortfolioLink ?? existingPortfolio.PortfolioLink;
            existingPortfolio.BehanceLink = portfolioUpdateDto.BehanceLink ?? existingPortfolio.BehanceLink;
            existingPortfolio.YoutubeLink = portfolioUpdateDto.YoutubeLink ?? existingPortfolio.YoutubeLink;
            existingPortfolio.GitHubLink = portfolioUpdateDto.GitHubLink ?? existingPortfolio.GitHubLink;

            // Parse the PortfolioData string to DateTime
            if (!string.IsNullOrEmpty(portfolioUpdateDto.PortfolioData))
            {
                if (DateTime.TryParse(portfolioUpdateDto.PortfolioData, out DateTime parsedDate))
                {
                    existingPortfolio.PortfolioData = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                }
            }

            existingPortfolio.Status = portfolioUpdateDto.Status ?? existingPortfolio.Status;
            existingPortfolio.Type = portfolioUpdateDto.Type ?? existingPortfolio.Type;

            return existingPortfolio;
        }
    }
}