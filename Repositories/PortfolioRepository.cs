using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackEnd.Data;
using BackEnd.Interfaces;
using BackEnd.Modules;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Repositories
{
    public class PortfolioRepository : IPortfolioRepository
    {
        private readonly  ApplicationDBContext _context;
        public PortfolioRepository(ApplicationDBContext context)
        {
            _context = context;
            
        }
        public async Task<List<Portfolio>> GetAllAsync()
        {
            return await _context.Portfolios.ToListAsync();
        }
    }
}