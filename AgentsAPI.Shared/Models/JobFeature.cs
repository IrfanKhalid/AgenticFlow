namespace AgentsAPI.Shared.Models
{
    public class JobFeature
    {
        public string ContentHash { get; set; } = string.Empty;
        public int? RequiredYears { get; set; }
        public string? Skills { get; set; }
        public string? Tools { get; set; }
        public string? CloudDemand { get; set; }
        public string? AiDemand { get; set; }
        public string? Salary { get; set; }
        public bool HasAi { get; set; }
        public bool HasCloud { get; set; }
    }
}
