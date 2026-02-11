using System;

namespace AgentsAPI.Shared.Models
{
    public class CrawlerRun
    {
        public Guid Id { get; set; }
        public string CrawlerName { get; set; } = null!;
        public DateTime StartedAtUtc { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public long DurationMs { get; set; }
    }
}
