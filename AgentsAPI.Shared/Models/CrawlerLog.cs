using System;
using System.ComponentModel.DataAnnotations;

namespace AgentsAPI.Shared.Models
{
    public class CrawlerLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CrawlerName { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = string.Empty; // "Success", "Error", "Canceled"
        public int JobsCrawled { get; set; }
        public int JobsSaved { get; set; }
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
    }
}
