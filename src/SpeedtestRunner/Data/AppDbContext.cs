using Microsoft.EntityFrameworkCore;
using SpeedtestRunner.Data.Models;
using System;

namespace SpeedtestRunner.Data
{
    public sealed class AppDbContext : DbContext
    {
        public DbSet<Speedtest> Speedtests { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Speedtest>()
                .Property(s => s.Timestamp)
                .HasConversion(
                    v => v.DateTime.Ticks,
                    v => new DateTimeOffset(new DateTime(v)));
        }
    }
}
