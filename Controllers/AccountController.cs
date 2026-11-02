using CareerSync.Data;
using CareerSync.Models;
using CareerSync.Models.Enums;
using CareerSync.Models.ViewModels;
using CareerSync.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CareerSync.Controllers;

public class AccountController : Controller
{
    private readonly IRepositoryWrapper _repository;
    private readonly ISmsVerificationService _sms;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountController> _logger;

    // SMS codes: at most one text per account per minute (stops someone using
    // the form to spam a phone and burn Twilio credit), and at most 5 wrong
    // codes per 10 minutes before the user has to request a new one.
    private static readonly TimeSpan SmsResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxSmsCodeAttempts = 5;
    private static readonly TimeSpan SmsAttemptWindow = TimeSpan.FromMinutes(10);

    // Recovery hardening: after this many wrong answers the account's
    // security-question step is locked for LockoutDuration.
    private const int MaxSecurityAnswerAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private const string InvalidResetMessage = "That reset session is invalid or has expired — please start again.";

    public AccountController(IRepositoryWrapper repository, ISmsVerificationService sms,
        IMemoryCache cache, ILogger<AccountController> logger)
    {
        _repository = repository;
        _sms = sms;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = _repository.AppUser.FindByEmail(model.Email);
        if (user is null || user.PasswordHash is null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Contact the Careers & Employability Office.");
            return View(model);
        }

        // Transparently upgrade accounts still on the old unsalted SHA-256 hash.
        if (PasswordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = PasswordHasher.Hash(model.Password);
            _repository.AppUser.Update(user);
        }

        await SignInUserAsync(user, model.RememberMe);

        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = user.Id,
            ActionType = "Login",
            Description = $"{user.FullName} signed in.",
        });

        // Accounts created before security questions existed can't recover
        // their password, so send them to set questions up straight away.
        if (!user.HasSecurityQuestions)
        {
            TempData["StatusMessage"] = "Please set up your security questions — you'll need them if you ever forget your password.";
            return RedirectToAction(nameof(SecurityQuestions));
        }

        return user.Role == UserRole.Admin
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "AppUser");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (_repository.AppUser.EmailExists(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        _repository.AppUser.Add(new AppUser
        {
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Qualification = model.Qualification,
            PasswordHash = PasswordHasher.Hash(model.Password),
            Role = UserRole.Graduate,
            SecurityQuestion1 = model.SecurityQuestion1,
            SecurityAnswer1Hash = SecurityAnswerHasher.Hash(model.SecurityAnswer1),
            SecurityQuestion2 = model.SecurityQuestion2,
            SecurityAnswer2Hash = SecurityAnswerHasher.Hash(model.SecurityAnswer2),
        });

        var newUser = _repository.AppUser.FindByEmail(model.Email);
        _repository.AuditLog.Add(new AuditLogEntry
        {
            UserId = newUser?.Id,
            ActionType = "Register",
            Description = $"{model.FullName} created an account.",
        });

        TempData["StatusMessage"] = "Account created — please sign in.";
        return RedirectToAction("Login");
    }
}
