using System;

namespace AgentsAPI.Shared.Models
{
    public class CronCrawler
    {
        public Guid Id { get; set; }
        public string CrawlerName { get; set; } = null!;
        public string CronExpression { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime? LastRunTime { get; set; }
    }
}
