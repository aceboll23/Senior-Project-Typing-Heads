using BoredGamers.Data;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Tests.TestUtilities
{
  public class TestApplicationDbContext : ApplicationDbContext
  {
    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // SQLite doesn't support SQL Server function GETUTCDATE()
      foreach (var entity in modelBuilder.Model.GetEntityTypes())
      {
        foreach (var prop in entity.GetProperties())
        {
          if (prop.GetDefaultValueSql() == "GETUTCDATE()")
          {
            prop.SetDefaultValueSql(null);
          }
        }
      }
    }
  }
}