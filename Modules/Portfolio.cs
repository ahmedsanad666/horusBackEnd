using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.Modules
{
    public class Portfolio
    {
        public int Id { get; set; }
        public String Name { get; set; } = String.Empty;
        public String Description { get; set; } = String.Empty;
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
    }
}