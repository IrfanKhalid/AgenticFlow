using System;

namespace AgentsAPI.Shared.Models
{
    public class ProcessingJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string JobsIds { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ApplyUrl { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public string ContentHash { get; private set; } = string.Empty;
    }
}
