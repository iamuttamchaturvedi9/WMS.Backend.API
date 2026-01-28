using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WMS.Backend.API.Data;

public sealed class WMSDbContextFactory : IDesignTimeDbContextFactory<WMSDbContext>
{
    public WMSDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("WMSDatabase")
            ?? "Data Source=WMSStockAllocation.db";

        var optionsBuilder = new DbContextOptionsBuilder<WMSDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new WMSDbContext(optionsBuilder.Options);
    }
}