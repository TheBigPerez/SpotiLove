using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Spotilove;

public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();

        // Try direct connection string first
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__PostgresConnection");

        if (string.IsNullOrEmpty(connStr))
        {
            // Fall back to URL parsing
            var url = Environment.GetEnvironmentVariable("DATABASE_URL")
                      ?? throw new Exception("No database connection configured");

            var uri = new Uri(url);
            var userInfo = uri.UserInfo.Split(':', 2);

            connStr =
                $"Host={uri.Host};" +
                $"Port={uri.Port};" +
                $"Database={uri.AbsolutePath.TrimStart('/')};" +
                $"Username={userInfo[0]};" +
                $"Password={userInfo[1]};" +
                $"SSL Mode=Require;" +
                $"Trust Server Certificate=true";
        }

        builder.UseNpgsql(connStr)
               .UseSnakeCaseNamingConvention();

        return new AppDbContext(builder.Options);
    }
}
