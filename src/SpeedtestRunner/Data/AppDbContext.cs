using Microsoft.EntityFrameworkCore;
using SpeedtestRunner.Data.Models;

namespace SpeedtestRunner.Data
{
    public sealed class AppDbContext : DbContext
    {
        public DbSet<Speedtest> Speedtests { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
    }
}
