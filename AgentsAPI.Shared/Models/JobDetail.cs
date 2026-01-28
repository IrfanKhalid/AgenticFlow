using System.Collections.Generic;
using System.ComponentModel.Design;

namespace AgentsAPI.Shared.Models
{
    public class JobDetail
    {
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
    }
}
