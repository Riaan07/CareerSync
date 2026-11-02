using System.ComponentModel.DataAnnotations;

namespace CareerSync.Models.ViewModels;

public class ProfileViewModel
{
    [Required]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty; // read-only in the view

    [Required]
    [SmsPhoneNumber]
    [Display(Name = "Contact number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Qualification")]
    public string? Qualification { get; set; }

    [Display(Name = "LinkedIn profile (optional)")]
    [Url(ErrorMessage = "Enter a valid URL.")]
    public string? LinkedInUrl { get; set; }
}
