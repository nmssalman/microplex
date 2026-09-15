namespace Microplex.Web.Data;

public static class DatabaseConfiguration
{
    public static string GetRequiredConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = configuration["DATABASE_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Database connection is not configured. Set the DefaultConnection environment variable.");

        return connectionString;
    }
}
