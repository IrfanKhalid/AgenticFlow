using System;

namespace AgentsAPI.Shared.Models
{
    public class ProcessingJob
    {
        public string ContentHash { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ApplyUrl { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public bool IsProcessd { get; set; }
    }
}
