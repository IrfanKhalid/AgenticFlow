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
    public class repoUtility
    {
        private readonly JobRepository _jobRepository;
        private readonly ILogger<repoUtility> _logger;
        public repoUtility(JobRepository jobRepositoy, ILogger<repoUtility> logger)
        {
            _jobRepository = jobRepositoy;
            _logger = logger;
        }
        public async  Task InsertJobsByBatch(List<JobDetail> jobs)
        {
            try
            {
                await _jobRepository.AddOrUpdateAsync(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving job {ApplyUrl}", jobs);
            }
        }
    }
}
