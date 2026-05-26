using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Spotilove;

public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();

        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__PostgresConnection");

        if (string.IsNullOrEmpty(connStr))
        {
            var url = Environment.GetEnvironmentVariable("DATABASE_URL")
                      ?? "Host=localhost;Port=5432;Database=spotilove_dev;Username=postgres;Password=postgres";

            if (!url.StartsWith("postgres://") && !url.StartsWith("postgresql://"))
            {
                connStr = url;
            }
            else
            {
                var uri = new Uri(url);
                var userInfo = uri.UserInfo.Split(':', 2);
                connStr =
                    $"Host={uri.Host};" +
                    $"Port={uri.Port};" +
                    $"Database={uri.AbsolutePath.TrimStart('/')};" +
                    $"Username={Uri.UnescapeDataString(userInfo[0])};" +
                    $"Password={Uri.UnescapeDataString(userInfo[1])};" +
                    $"SSL Mode=Require;" +
                    $"Trust Server Certificate=true";
            }
        }

        builder.UseNpgsql(connStr)
               .UseSnakeCaseNamingConvention();

        return new AppDbContext(builder.Options);
    }
}