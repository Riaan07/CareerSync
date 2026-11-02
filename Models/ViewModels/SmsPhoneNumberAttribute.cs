using System.ComponentModel.DataAnnotations;
using CareerSync.Services;

namespace CareerSync.Models.ViewModels
{
    /// <summary>
    /// Accepts only phone numbers that can be converted to international (E.164)
    /// format, so the password-reset SMS can reach them. Empty values pass — combine
    /// with [Required] where the number is mandatory.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SmsPhoneNumberAttribute : ValidationAttribute
    {
        public SmsPhoneNumberAttribute()
            : base("Enter a mobile number like 082 123 4567, or include the country code (e.g. +27 66 002 1234).") { }

        public override bool IsValid(object? value) =>
            value is not string s || string.IsNullOrWhiteSpace(s) || PhoneNumbers.ToE164(s) is not null;
    }
}

