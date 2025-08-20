using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace BackEnd.Modules
{
    public class AppUser : IdentityUser
    {
        public String Name { get; set; }
        public String Role { get; set; }
        public String Bio { get; set; } = String.Empty;
        public String FaceBook { get; set; } = String.Empty;
        public String Instgram { get; set; } = String.Empty;
        public String Behance { get; set; } = String.Empty;
        public String GitHub { get; set; } = String.Empty;
        public string UserImg { get; set; } = String.Empty;
        public string UserTitle { get; set; } = String.Empty;
        public string CVUrl { get; set; } = String.Empty;
        public ICollection<AppUserPortfolio> AppUserPortfolios { get; set; }
    }
}