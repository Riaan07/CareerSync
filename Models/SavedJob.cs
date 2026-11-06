using System.ComponentModel.DataAnnotations;

namespace CareerSync.Models
{
    public class SavedJob : IEntity
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public int JobPostingId { get; set; }
        [Required, StringLength(25)] public string ApplicationStatus { get; set; } = "Saved";
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AppliedAt { get; set; }
        [StringLength(1000)] public string? Notes { get; set; }
    }
}
