using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackEnd.Modules;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data
{
    public class ApplicationDBContext : IdentityDbContext<AppUser>
    {
        public ApplicationDBContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {


        }

        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<AppUserPortfolio> AppUserPortfolios { get; set; }
        public DbSet<PortfolioImage> PortfolioImages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Seed roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "b1a7e8c2-1f2a-4c3b-9d2e-123456789abc",
                    Name = "Developer",
                    NormalizedName = "DEVELOPER"
                },
                new IdentityRole
                {
                    Id = "c2b8f9d3-2e3b-5d4c-0e3f-234567890bcd",
                    Name = "Designer",
                    NormalizedName = "DESIGNER"
                },
                new IdentityRole
                {
                    Id = "d3c9fab4-3f4c-6e5d-1f4g-345678901cde",
                    Name = "Marketing",
                    NormalizedName = "MARKETING"
                },
                new IdentityRole
                {
                    Id = "e4dabcb5-4g5d-7f6e-2g5h-456789012def",
                    Name = "MotionGraphic",
                    NormalizedName = "MOTIONGRAPHIC"
                }
            );

            // Configure many-to-many relationship between AppUser and Portfolio
            builder.Entity<AppUserPortfolio>()
                .HasKey(ap => new { ap.AppUserId, ap.PortfolioId });

            builder.Entity<AppUserPortfolio>()
                .HasOne(ap => ap.AppUser)
                .WithMany(u => u.AppUserPortfolios)
                .HasForeignKey(ap => ap.AppUserId);

            builder.Entity<AppUserPortfolio>()
                .HasOne(ap => ap.Portfolio)
                .WithMany(p => p.AppUserPortfolios)
                .HasForeignKey(ap => ap.PortfolioId);

            // Configure one-to-many relationship between Portfolio and PortfolioImage
            builder.Entity<PortfolioImage>()
                .HasOne(pi => pi.Portfolio)
                .WithMany(p => p.PortfolioImages)
                .HasForeignKey(pi => pi.PortfolioId);

            // Additional configurations if needed
        }

    }
}