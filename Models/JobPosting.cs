using System.ComponentModel.DataAnnotations;

namespace CareerSync.Models
{
    public class JobPosting : IEntity
    {
        public int Id { get; set; }
        [Required, StringLength(160)] public string Title { get; set; } = string.Empty;
        [Required, StringLength(160)] public string Company { get; set; } = string.Empty;
        [Required, MinLength(30)] public string Description { get; set; } = string.Empty;
        [StringLength(120)] public string? Location { get; set; }
        [StringLength(80)] public string? EmploymentType { get; set; }
        [Url, StringLength(1500)] public string? ApplicationUrl { get; set; }
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosingDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedByAdminId { get; set; }
    }
}
