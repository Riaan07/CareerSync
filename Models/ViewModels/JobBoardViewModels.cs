using System.ComponentModel.DataAnnotations;

namespace CareerSync.Models.ViewModels
{
    public class JobBoardMatchViewModel
    {
        [Required(ErrorMessage = "Please select a saved CV.")] public int CvId { get; set; }
        [Required(ErrorMessage = "Please select a job posting.")] public int JobPostingId { get; set; }
        public List<CV> Cvs { get; set; } = new();
        public List<JobPosting> Jobs { get; set; } = new();
    }
}
