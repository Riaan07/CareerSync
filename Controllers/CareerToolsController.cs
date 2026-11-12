using CareerSync.Data;
using CareerSync.Models;
using CareerSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace CareerSync.Controllers;

[Authorize(Roles = "Graduate")]
public class CareerToolsController : Controller
{
    private readonly CareerSyncDbContext _db;
    private readonly IRepositoryWrapper _repo;
    private readonly IDocumentAnalysisService _analysis;
    public CareerToolsController(CareerSyncDbContext db, IRepositoryWrapper repo, IDocumentAnalysisService analysis)
    { _db = db; _repo = repo; _analysis = analysis; }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> SavedJobs()
    {
        ViewData["StatusMessage"] = TempData["StatusMessage"];
        var jobs = await (from saved in _db.SavedJobs.AsNoTracking()
                          join job in _db.JobPostings.AsNoTracking() on saved.JobPostingId equals job.Id
                          where saved.AppUserId == UserId
                          orderby saved.SavedAt descending
                          select new SavedJobRow { Saved = saved, Job = job }).ToListAsync();
        return View(jobs);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveJob(int jobPostingId)
    {
        var posting = await _db.JobPostings.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobPostingId && j.IsActive && (j.ClosingDate == null || j.ClosingDate >= DateTime.UtcNow));
        if (posting == null) return NotFound();
        if (!await _db.SavedJobs.AnyAsync(x => x.AppUserId == UserId && x.JobPostingId == jobPostingId))
        {
            _db.SavedJobs.Add(new SavedJob { AppUserId = UserId, JobPostingId = jobPostingId });
            await _db.SaveChangesAsync();
        }
        TempData["StatusMessage"] = "Job saved to your application tracker.";
        return RedirectToAction(nameof(SavedJobs));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateApplication(int id, string status, string? notes)
    {
        var allowed = new[] { "Saved", "Applied", "Interview", "Offer", "Rejected", "Withdrawn" };
        var saved = await _db.SavedJobs.FirstOrDefaultAsync(s => s.Id == id && s.AppUserId == UserId);
        if (saved == null) return NotFound();
        if (!allowed.Contains(status)) return BadRequest("Invalid application status.");
        saved.ApplicationStatus = status;
        saved.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()[..Math.Min(notes.Trim().Length, 1000)];
        if (status == "Applied" && !saved.AppliedAt.HasValue) saved.AppliedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Application status updated.";
        return RedirectToAction(nameof(SavedJobs));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSavedJob(int id)
    {
        var saved = await _db.SavedJobs.FirstOrDefaultAsync(s => s.Id == id && s.AppUserId == UserId);
        if (saved == null) return NotFound();
        _db.SavedJobs.Remove(saved);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(SavedJobs));
    }

    [HttpGet]
    public async Task<IActionResult> Compare(int? jobPostingId, int? firstCvId, int? secondCvId)
    {
        var cvs = _repo.CV.GetByAppUserId(UserId).OrderByDescending(c => c.UploadedAt).ToList();
        var jobs = await _db.JobPostings.AsNoTracking().Where(j => j.IsActive).OrderByDescending(j => j.PostedAt).ToListAsync();
        var vm = new CvComparisonPage { Cvs = cvs, Jobs = jobs, JobPostingId = jobPostingId, FirstCvId = firstCvId, SecondCvId = secondCvId };
        if (jobPostingId.HasValue && firstCvId.HasValue && secondCvId.HasValue && firstCvId != secondCvId)
        {
            var job = jobs.FirstOrDefault(j => j.Id == jobPostingId);
            var first = cvs.FirstOrDefault(c => c.Id == firstCvId);
            var second = cvs.FirstOrDefault(c => c.Id == secondCvId);
            if (job == null || first == null || second == null) { ModelState.AddModelError("", "Select an active job and two CVs belonging to your account."); return View(vm); }
            var terms = _analysis.ExtractTerms(job.Description);
            var snapshot = new JobDescription { Title = job.Title, Company = job.Company, RawText = job.Description, RequiredSkills = terms.Skills, RequiredQualifications = terms.Qualifications };
            vm.First = _analysis.CalculateMatch(first, snapshot);
            vm.Second = _analysis.CalculateMatch(second, snapshot);
            vm.FirstName = first.FileName; vm.SecondName = second.FileName;
            vm.PriorResults = _repo.MatchResult.GetAll().Where(m => m.JobPostingId == job.Id && (m.CVId == first.Id || m.CVId == second.Id)).OrderBy(m => m.GeneratedAt).ToList();
        }
        return View(vm);
    }

    [HttpGet]
    public IActionResult AnalysisPdf(int id)
    {
        var match = _repo.MatchResult.GetById(id);
        var cv = match == null ? null : _repo.CV.GetById(match.CVId);
        if (match == null || cv == null || cv.AppUserId != UserId) return NotFound();
        var job = _repo.JobDescription.GetById(match.JobDescriptionId);
        if (job == null) return NotFound();
        var graduate = _repo.AppUser.GetById(UserId)?.FullName ?? "Graduate";
        var content = AnalysisPdfWriter.Build(match, cv, job, graduate);
        return File(content, "application/pdf", $"CareerSyncAI_Analysis_{id}.pdf");
    }
}
public class SavedJobRow
{
    public SavedJob Saved { get; set; } = new();
    public JobPosting Job { get; set; } = new();
}
public class CvComparisonPage
{
    public List<CV> Cvs { get; set; } = new();
    public List<JobPosting> Jobs { get; set; } = new();
    public int? JobPostingId { get; set; }
    public int? FirstCvId { get; set; }
    public int? SecondCvId { get; set; }
    public string? FirstName { get; set; }
    public string? SecondName { get; set; }
    public CareerSync.Services.MatchCalculationResult? First { get; set; }
    public CareerSync.Services.MatchCalculationResult? Second { get; set; }
    public List<MatchResult> PriorResults { get; set; } = new();
}
