namespace AgentsAPI.DataAccess.Models
{
    public static class DbConnectionStringProvider
    {
        public static string GetPostgres(string? configured = null)
        {
            return configured
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=agentsdb;Username=postgres;Password=postgres";
        }
    }
}