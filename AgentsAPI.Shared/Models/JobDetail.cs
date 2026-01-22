using System.Collections.Generic;

namespace AgentsAPI.Shared.Models
{
    public class JobDetail
    {
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public List<string> Description { get; set; } = new();
        public List<string> Responsibilities { get; set; } = new();
        public List<string> Achievements { get; set; } = new();
        public List<string> Requirements { get; set; } = new();
        public string Compensation { get; set; } = string.Empty;
        public string ApplyUrl { get; set; } = string.Empty;
    }
}
