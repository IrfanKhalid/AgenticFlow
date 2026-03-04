using AgentsAPI.DataAccess.Models;
using AgentsAPI.DataAccess.Repositories;
using AgentsAPI.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentsAPI.Scrapers.Crawlers.Utility
{
    public static class repoUtility
    {
        public static async Task PoliteDelayAsync(int minMs = 400, int maxMs = 900)
        {
            var random = new Random();
            await Task.Delay(random.Next(minMs, maxMs));
        }

        public static async Task FlushBatchIfNeededAsync(
            List<JobDetail> buffer,
            int threshold = 500)
        {
            if (buffer.Count < threshold)
                return;

            var connectionString = DbConnectionStringProvider.GetPostgres();
            var optionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            await using var db = new AgentsDbContext(optionsBuilder.Options);
            var repo = new JobRepository(db);

            var batch = buffer.GetRange(0, threshold);
            await repo.AddOrUpdateAsync(batch);

            buffer.RemoveRange(0, threshold);
        }
    }
}
