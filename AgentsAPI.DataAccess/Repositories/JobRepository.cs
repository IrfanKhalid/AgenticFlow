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

        public async Task AddOrUpdateAsync(List<JobDetail> jobs)
        {
            if (jobs.Count == 0) throw new ArgumentNullException(nameof(jobs));

            jobs.ForEach(x=>x.Active=true);
            jobs.ForEach(x => x.StartDate = DateTime.UtcNow);
            await _db.JobDetails.AddRangeAsync(jobs);
            await _db.SaveChangesAsync();
        }

        public async Task<List<JobDetail>> GetAllAsync()
        {
            return await _db.JobDetails.ToListAsync();
        }
    }
}
