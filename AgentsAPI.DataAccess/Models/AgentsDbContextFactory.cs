using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentsAPI.DataAccess.Models
{
    public class AgentsDbContextFactory
        : IDesignTimeDbContextFactory<AgentsDbContext>
    {
        public AgentsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();

            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=agentsdb;Username=postgres;Password=postgres";

            optionsBuilder.UseNpgsql(connectionString);
            return new AgentsDbContext(optionsBuilder.Options);
        }
    }

}
