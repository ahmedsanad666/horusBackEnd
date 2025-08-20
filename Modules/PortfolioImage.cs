using System.ComponentModel.DataAnnotations.Schema;

namespace BackEnd.Modules
{
    public class PortfolioImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public int PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; }
    }
}