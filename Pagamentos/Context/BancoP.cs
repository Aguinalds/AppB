using Pagamentos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Pagamentos.Context
{
    public class BancoP : IdentityDbContext
    {
        public BancoP(DbContextOptions<BancoP> options) : base(options)
        {
        }
        public DbSet<Boleto> Boletos { get; set; }
    }
}
