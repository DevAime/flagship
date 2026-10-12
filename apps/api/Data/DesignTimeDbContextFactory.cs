using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlagForge.Api.Data;

// Lets `dotnet ef migrations add` work without booting the whole app (and its JWT/config checks).
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=flagforge;Username=flagforge;Password=flagforge")
            .Options;
        return new AppDbContext(options);
    }
}
