using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;

namespace AgentsAPI.Shared.Models
{
    public class JobDetail
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Responsibilities { get; set; }
        public string? Achievements { get; set; } 
        public string? Requirements { get; set; }
        public string Compensation { get; set; } = string.Empty;
        public string ApplyUrl { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public bool Active { get; set; } = false;
        public string Department { get; set; } = string.Empty;
        public bool IsProcessed { get; set; } = false;
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
        public string CrawlerName { get; set; } = string.Empty;
        public string ContentHash { get; private set; } = string.Empty;
    }
}
