using CareerSync.Data;
using CareerSync.Models;
using CareerSync.Models.ViewModels;
using CareerSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CareerSyncA.Controllers;

[Authorize]
public class JobBoardController : Controller
{
    private readonly CareerSyncDbContext _db;
    private readonly IRepositoryWrapper _repository;
    private readonly IDocumentAnalysisService _analysis;
    public JobBoardController(CareerSyncDbContext db, IRepositoryWrapper repository, IDocumentAnalysisService analysis)
    { _db = db; _repository = repository; _analysis = analysis; }
    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<JobPosting> Available() => _db.JobPostings.AsNoTracking().Where(j => j.IsActive && (j.ClosingDate == null || j.ClosingDate >= DateTime.UtcNow));

    [Authorize(Roles = "Graduate")]
    [HttpGet]
    public async Task<IActionResult> Index(int? jobId)
    {
        ViewData["Title"] = "Job opportunities";
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        var model = new JobBoardMatchViewModel
        {
            Cvs = _repository.CV.GetByAppUserId(CurrentUserId()).OrderByDescending(c => c.UploadedAt).ToList(),
            Jobs = await Available().OrderByDescending(j => j.PostedAt).ToListAsync(),
            JobPostingId = jobId ?? 0
        };
        return View(model);
    }

    [Authorize(Roles = "Graduate")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Match(int cvId, int jobPostingId)
    {
        // Never allow cross-account CV access, even when an ID is forged.
        var cv = _repository.CV.GetByAppUserId(CurrentUserId()).FirstOrDefault(c => c.Id == cvId);
        var posting = await Available().FirstOrDefaultAsync(j => j.Id == jobPostingId);
        if (cv == null || posting == null)
        {
            TempData["StatusMessage"] = "Select one of your saved CVs and an available job posting.";
            return RedirectToAction(nameof(Index));
        }

        // Save a per-analysis snapshot: historical matches don't change when an admin edits a posting.
        var extraction = _analysis.ExtractTerms(posting.Description);
        var jd = _repository.JobDescription.Add(new JobDescription
        {
            AppUserId = CurrentUserId(),
            Title = posting.Title,
            Company = posting.Company,
            RawText = posting.Description,
            RequiredSkills = extraction.Skills,
            RequiredQualifications = extraction.Qualifications
        });
        var calc = _analysis.CalculateMatch(cv, jd);
        _repository.MatchResult.Add(new MatchResult
        {
            CVId = cv.Id,
            JobDescriptionId = jd.Id,
            JobPostingId = posting.Id,
            OverallScore = calc.OverallScore,
            TechnicalSkillsScore = calc.TechnicalSkillsScore,
            ExperienceScore = calc.ExperienceScore,
            QualificationsScore = calc.QualificationsScore,
            MatchedSkills = calc.MatchedSkills,
            GapSkills = calc.GapSkills
        });
        TempData["StatusMessage"] = $"Analysis complete — {calc.OverallScore}% match with {posting.Title}.";
        return RedirectToAction("Index", "AppUser");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        ViewData["Title"] = "Manage job postings";
        if (TempData["StatusMessage"] is string msg) ViewData["StatusMessage"] = msg;
        return View(await _db.JobPostings.AsNoTracking().OrderByDescending(j => j.PostedAt).ToListAsync());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        var posting = id is null ? new JobPosting() : await _db.JobPostings.FindAsync(id.Value);
        if (posting == null) return NotFound();
        return View(posting);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(JobPosting input)
    {
        // Metadata is set server-side; do not bind over existing ownership or timestamps.
        ModelState.Remove(nameof(JobPosting.CreatedByAdminId));
        if (!ModelState.IsValid) return View(input);
        if (input.ClosingDate is DateTime date && date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(input.ClosingDate), "Closing date cannot be in the past.");
            return View(input);
        }
        if (input.Id == 0)
        {
            input.CreatedByAdminId = CurrentUserId();
            input.PostedAt = DateTime.UtcNow;
            _db.JobPostings.Add(input);
        }
        else
        {
            var existing = await _db.JobPostings.FindAsync(input.Id);
            if (existing == null) return NotFound();
            existing.Title = input.Title.Trim(); existing.Company = input.Company.Trim();
            existing.Description = input.Description.Trim(); existing.Location = input.Location;
            existing.EmploymentType = input.EmploymentType; existing.ApplicationUrl = input.ApplicationUrl;
            existing.ClosingDate = input.ClosingDate; existing.IsActive = input.IsActive;
        }
        await _db.SaveChangesAsync();
        _repository.AuditLog.Add(new AuditLogEntry { UserId = CurrentUserId(), ActionType = "ManageJobPosting", Description = $"Saved job posting: {input.Title}." });
        TempData["StatusMessage"] = "Job posting saved.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var posting = await _db.JobPostings.FindAsync(id);
        if (posting == null) return NotFound();
        posting.IsActive = false;
        await _db.SaveChangesAsync();
        _repository.AuditLog.Add(new AuditLogEntry { UserId = CurrentUserId(), ActionType = "DeactivateJobPosting", Description = $"Deactivated job posting #{id}." });
        return RedirectToAction(nameof(Manage));
    }
}
