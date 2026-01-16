using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebApp.Data
{
    /// <summary>
    /// Design-time factory for ApplicationDbContext
    /// Used by EF Core tools (migrations, etc.) to create DbContext instances
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Check if we're targeting PostgreSQL (Production)
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (!string.IsNullOrEmpty(databaseUrl) || aspNetCoreEnvironment == "Production")
            {
                // Use PostgreSQL for production/migrations
                string connectionString;

                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    // Parse Render's DATABASE_URL format
                    var databaseUri = new Uri(databaseUrl);
                    var userInfo = databaseUri.UserInfo.Split(':');
                    
                    // Use default PostgreSQL port (5432) if not specified
                    var port = databaseUri.Port > 0 ? databaseUri.Port : 5432;

                    connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
                }
                else
                {
                    throw new InvalidOperationException(
                        "DATABASE_URL environment variable must be set for PostgreSQL migrations. " +
                        "Set it using: $env:DATABASE_URL = \"your-connection-string\"");
                }

                optionsBuilder.UseNpgsql(connectionString);
            }
            else
            {
                // Use SQL Server for local development
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
