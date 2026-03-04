using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using AgentsAPI.DataAccess.Models;
using AgentsAPI.Shared.Models;

namespace AgentsAPI.DataAccess.Repositories
{
    public class JobRepository
    {
        private readonly AgentsDbContext _db;
        private const int TitleMaxLength = 1000;
        private const int LocationMaxLength = 1000;

        public JobRepository(AgentsDbContext db)
        {
            _db = db;
        }

        public async Task AddOrUpdateAsync(List<JobDetail> jobs)
        {
            if (jobs == null || jobs.Count == 0) throw new ArgumentNullException(nameof(jobs));

            jobs.ForEach(NormalizeJob);
            await _db.JobDetails.AddRangeAsync(jobs);
            await _db.SaveChangesAsync();
        }

        public async Task<List<JobDetail>> GetAllAsync()
        {
            return await _db.JobDetails.ToListAsync();
        }

        private static void NormalizeJob(JobDetail job)
        {
            job.Active = true;
            job.StartDate = (job.StartDate.Year < 2022) ? DateTime.UtcNow : job.StartDate;

            job.Title = Truncate(job.Title, TitleMaxLength);
            job.Location = Truncate(job.Location, LocationMaxLength);

            job.Department = job.Department?.Trim() ?? string.Empty;
            job.ApplyUrl = job.ApplyUrl?.Trim() ?? string.Empty;
            job.Compensation = job.Compensation?.Trim() ?? string.Empty;
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }
    }
}
