using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CentroDeProduccion.Infrastructure.Data;

/// <summary>
/// Design-time factory so `dotnet ef` can run with Infrastructure as the startup project
/// without building/locking the Api host (whose bin folder is often locked by a running
/// dev server). Connection string: DOTNET_ConnectionString env var, else the appsettings'
/// localhost default.
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DOTNET_ConnectionString")
            ?? "Server=localhost;Database=CentroDeProduccion;Trusted_Connection=true;TrustServerCertificate=true";

        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options);
    }
}
