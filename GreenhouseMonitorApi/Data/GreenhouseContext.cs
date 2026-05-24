using GreenhouseMonitorApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenhouseMonitorApi.Data;

public class GreenhouseContext : DbContext
{
    public GreenhouseContext(DbContextOptions<GreenhouseContext> options)
        : base(options)
    {
    }

    public DbSet<Reading> Readings => Set<Reading>();
}
