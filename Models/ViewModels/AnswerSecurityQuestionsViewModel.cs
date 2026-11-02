using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CareerSync.Models.ViewModels
{
    /// <summary>Step 2 of recovery — answer both security questions.</summary>
    public class AnswerSecurityQuestionsViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        // Display-only: always reloaded from the database on the server so a
        // tampered hidden field can't change which questions are checked.
        [ValidateNever] public string Question1 { get; set; } = string.Empty;
        [ValidateNever] public string Question2 { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your answer.")]
        [Display(Name = "Your answer")]
        public string Answer1 { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your answer.")]
        [Display(Name = "Your answer")]
        public string Answer2 { get; set; } = string.Empty;
    }
}
