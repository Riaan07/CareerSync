using CareerSync.Models;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;

namespace CareerSync.Services;

public interface IAnalysisFeedbackEmailService
{
    Task SendAsync(AppUser graduate, MatchResult match, JobDescription job, string feedback, CancellationToken cancellationToken = default);
}

public sealed class AnalysisFeedbackEmailService : IAnalysisFeedbackEmailService
{
    private readonly IConfiguration _configuration;
    public AnalysisFeedbackEmailService(IConfiguration configuration) => _configuration = configuration;

    public async Task SendAsync(AppUser graduate, MatchResult match, JobDescription job, string feedback, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Email:SmtpHost"];
        var from = _configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email is not configured. Set Email__SmtpHost and Email__FromAddress before sending.");
        if (!int.TryParse(_configuration["Email:SmtpPort"], out var port) || port < 1 || port > 65535)
            throw new InvalidOperationException("Configure a valid Email__SmtpPort.");
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) || ssl;
        if (!enableSsl) throw new InvalidOperationException("Encrypted SMTP transport must be enabled.");
        var senderName = _configuration["Email:FromName"] ?? "CareerSyncAI Career Support";
        var enc = HtmlEncoder.Default;
        string Safe(string? value) => enc.Encode(value ?? string.Empty);
        var matched = match.MatchedSkills.Count == 0 ? "No matched skills recorded" : string.Join(" • ", match.MatchedSkills.Select(Safe));
        var gaps = match.GapSkills.Count == 0 ? "No skill gaps identified" : string.Join(" • ", match.GapSkills.Select(Safe));
        var safeFeedback = Safe(feedback).Replace("\r\n", "\n").Replace("\n", "<br/>");
        var html = $@"<!DOCTYPE html><html><body style='margin:0;padding:24px;background:#f3f5f9;font-family:Arial,sans-serif;color:#1d2939'>
<table role='presentation' style='width:100%;max-width:620px;margin:auto;background:white;border-collapse:collapse;border-radius:14px;overflow:hidden'>
<tr><td style='background:#122647;color:white;padding:26px 30px'><div style='font-size:24px;font-weight:bold'>CareerSync</div><div style='margin-top:6px;color:#cbd5e1'>Personalised CV analysis feedback</div></td></tr>
<tr><td style='padding:28px 30px'><p>Hello {Safe(graduate.FullName)},</p><p>Your CareerSync administrator has reviewed your analysis for <strong>{Safe(job.Title)}</strong>{(string.IsNullOrWhiteSpace(job.Company) ? "" : " at <strong>" + Safe(job.Company) + "</strong>")}.</p>
<div style='padding:18px;border:1px solid #d5dce6;border-radius:12px;margin:18px 0'><div style='font-size:13px;color:#64748b'>OVERALL MATCH</div><div style='font-size:34px;font-weight:bold;color:#122647'>{match.OverallScore}%</div>
<div style='font-size:13px;margin-top:6px'>Technical skills: {match.TechnicalSkillsScore}% &nbsp; | &nbsp; Experience: {match.ExperienceScore}% &nbsp; | &nbsp; Qualifications: {match.QualificationsScore}%</div></div>
<h3 style='color:#122647'>Skills matched</h3><p>{matched}</p><h3 style='color:#122647'>Skills to develop</h3><p>{gaps}</p>
<h3 style='color:#122647'>A note from your administrator</h3><div style='background:#f1f5f9;padding:16px;border-radius:10px;line-height:1.6'>{safeFeedback}</div>
<p style='margin-top:24px;font-size:13px;color:#596579'>This is guidance based on a rule-based algorithm and does not guarantee employment. Sign in to CareerSync to review your full results and learning resources.</p></td></tr>
<tr><td style='background:#f8fafc;padding:18px 30px;color:#64748b;font-size:12px'>CareerSync · Graduate career readiness support</td></tr></table></body></html>";
        var plain = $"Hello {graduate.FullName},\n\nAnalysis: {job.Title}\nOverall: {match.OverallScore}%\nTechnical: {match.TechnicalSkillsScore}% | Experience: {match.ExperienceScore}% | Qualifications: {match.QualificationsScore}%\nMatched: {string.Join(", ", match.MatchedSkills)}\nSkill gaps: {string.Join(", ", match.GapSkills)}\n\nAdministrator feedback:\n{feedback}\n\nCareerSync";
        using var mail = new MailMessage();
        mail.From = new MailAddress(from, senderName);
        mail.To.Add(new MailAddress(graduate.Email));
        mail.Subject = $"CareerSync feedback: {job.Title}";
        mail.Body = plain;
        mail.IsBodyHtml = false;
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, "text/html"));
        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl, DeliveryMethod = SmtpDeliveryMethod.Network, Timeout = 30000 };
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password ?? "");
        await client.SendMailAsync(mail, cancellationToken);
    }
}
