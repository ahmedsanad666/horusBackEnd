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
        public static PortfolioDto ToPortfolioDto(this Portfolio portfolioModel, string? baseUrl = null)
        {
            return new PortfolioDto
            {
                Id = portfolioModel.Id,
                EnTitle = portfolioModel.EnTitle,
                ArTitle = portfolioModel.ArTitle,
                EnDescription = portfolioModel.EnDescription,
                ArDescription = portfolioModel.ArDescription,
                CreatedAt = portfolioModel.CreatedAt,
                Status = portfolioModel.Status,
                PortfolioData = portfolioModel.PortfolioData,
                PortfolioLink = portfolioModel.PortfolioLink,
                BehanceLink = portfolioModel.BehanceLink,
                YoutubeLink = portfolioModel.YoutubeLink,
                GitHubLink = portfolioModel.GitHubLink,
                PortfolioImages = portfolioModel.PortfolioImages?.Select(pi => pi.ToPortfolioImageDto()).ToList() ?? new List<PortfolioImageDto>(),
                Users = portfolioModel.AppUserPortfolios?.Where(aup => aup.AppUser != null).Select(aup => new UserDto
                {
                    Id = aup.AppUser!.Id,
                    UserName = aup.AppUser.UserName ?? string.Empty,
                    Name = aup.AppUser.Name ?? string.Empty,
                    Role = aup.AppUser.Role ?? string.Empty,
                    UserImg = !string.IsNullOrEmpty(aup.AppUser.UserImg) && baseUrl != null ? baseUrl + aup.AppUser.UserImg : aup.AppUser.UserImg ?? string.Empty,
                    UserTitle = aup.AppUser.UserTitle ?? string.Empty,
                    PhoneNumber = aup.AppUser.PhoneNumber ?? string.Empty,
                    CVUrl = aup.AppUser.CVUrl ?? string.Empty
                }).ToList() ?? new List<UserDto>(),
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
                EnTitle = portfolioCreateDto.EnTitle,
                ArTitle = portfolioCreateDto.ArTitle,
                EnDescription = portfolioCreateDto.EnDescription,
                ArDescription = portfolioCreateDto.ArDescription,
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
            existingPortfolio.EnTitle = portfolioUpdateDto.EnTitle ?? existingPortfolio.EnTitle;
            existingPortfolio.ArTitle = portfolioUpdateDto.ArTitle ?? existingPortfolio.ArTitle;
            existingPortfolio.EnDescription = portfolioUpdateDto.EnDescription ?? existingPortfolio.EnDescription;
            existingPortfolio.ArDescription = portfolioUpdateDto.ArDescription ?? existingPortfolio.ArDescription;
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