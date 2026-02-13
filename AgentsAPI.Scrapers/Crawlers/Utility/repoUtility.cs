using AgentsAPI.DataAccess.Repositories;
using AgentsAPI.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
