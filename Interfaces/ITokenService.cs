using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackEnd.Modules;

namespace BackEnd.Interfaces
{
    public interface ITokenService
    {
        String CreateToken(AppUser user);
    }
}