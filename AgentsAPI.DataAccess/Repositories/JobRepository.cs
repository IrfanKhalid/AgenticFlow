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
        public JobRepository(AgentsDbContext db)
        {
            _db = db;
        }

        public async Task AddOrUpdateAsync(JobDetail job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            var existing = await _db.JobDetails.FindAsync(job.ApplyUrl);
            if (existing == null)
            {
                await _db.JobDetails.AddAsync(job);
            }
            else
            {
                existing.Title = job.Title;
                existing.Location = job.Location;
                existing.Description = job.Description;
                existing.Responsibilities = job.Responsibilities;
                existing.Achievements = job.Achievements;
                existing.Requirements = job.Requirements;
                existing.Compensation = job.Compensation;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<List<JobDetail>> GetAllAsync()
        {
            return await _db.JobDetails.ToListAsync();
        }
    }
}
