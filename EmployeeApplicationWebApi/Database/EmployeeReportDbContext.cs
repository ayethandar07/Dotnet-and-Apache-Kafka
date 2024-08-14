using EmployeeApplicationWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApplicationWebApi.Database;

public class EmployeeReportDbContext : DbContext
{
    public EmployeeReportDbContext(DbContextOptions<EmployeeReportDbContext> dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<EmployeeReport> Reports { get; set; }
}
