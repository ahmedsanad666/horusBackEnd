using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.Modules
{
    public class Portfolio
    {
        public int Id { get; set; }
        public String EnTitle { get; set; } = String.Empty;
        public String ArTitle { get; set; } = String.Empty;
        public String TrTitle { get; set; } = String.Empty;
        public String EnDescription { get; set; } = String.Empty;
        public String ArDescription { get; set; } = String.Empty;
        public String TrDescription { get; set; } = String.Empty;
        /// <summary>Author/source language for title & description (en, ar, tr). Other locales filled via DeepL.</summary>
        public string OriginalLanguage { get; set; } = "en";
        public bool IsTranslated { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool Status { get; set; } = true;

        public ICollection<AppUserPortfolio> AppUserPortfolios { get; set; }

        public List<PortfolioImage> PortfolioImages { get; set; }
        public DateTime PortfolioData { get; set; }
        public string PortfolioLink { get; set; }
        public string BehanceLink { get; set; }
        public string YoutubeLink { get; set; }
        public string GitHubLink { get; set; }
        public string Type { get; set; }

        /// <summary>Main cover image URL for cards and listings (stored under wwwroot/images).</summary>
        public string? ThumbnailUrl { get; set; }
    }
}