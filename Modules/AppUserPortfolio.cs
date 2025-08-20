using System.ComponentModel.DataAnnotations.Schema;

namespace BackEnd.Modules
{
    public class AppUserPortfolio
    {
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public int PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; }
    }
}