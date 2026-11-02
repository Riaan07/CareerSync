using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CareerSync.Services;

public enum SmsSendStatus
{
    Sent,
    InvalidNumber,     // Twilio can't text this number (bad format, landline, unsupported country)
    TooManyRequests,   // Twilio's limit on codes sent to one number
    Failed,            // anything else: bad credentials, network error, trial-account restriction…
}

public enum SmsCheckStatus
{
    Approved,
    Incorrect,         // wrong code — the user may try again
    ExpiredOrMissing,  // no code pending: expired (10 min), already used, or too many wrong tries
    Failed,
}

public interface ISmsVerificationService
{
    Task<SmsSendStatus> SendCodeAsync(string e164Phone, CancellationToken ct = default);
    Task<SmsCheckStatus> CheckCodeAsync(string e164Phone, string code, CancellationToken ct = default);
}

/// <summary>
/// Bound from the "Twilio" configuration section. Keep AuthToken in user-secrets
/// (development) or an environment variable (production) — never in
/// appsettings.json, which gets committed to source control.
/// </summary>
public class TwilioOptions
{
    public const string SectionName = "Twilio";

    public string AccountSid { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    public string VerifyServiceSid { get; set; } = string.Empty;

    /// <summary>Only changed for automated tests.</summary>
    public string ApiBaseUrl { get; set; } = "https://verify.twilio.com";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountSid) &&
        !string.IsNullOrWhiteSpace(AuthToken) &&
        !string.IsNullOrWhiteSpace(VerifyServiceSid);
}

/// <summary>
/// Twilio Verify over its REST API (no NuGet package needed). Twilio creates the
/// 6-digit code, texts it, expires it after 10 minutes and limits wrong guesses;
/// this app never sees or stores the code.
/// </summary>
public class TwilioVerifyService : ISmsVerificationService
{
    private readonly HttpClient _http;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioVerifyService> _logger;

    public TwilioVerifyService(HttpClient http, TwilioOptions options, ILogger<TwilioVerifyService> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<SmsSendStatus> SendCodeAsync(string e164Phone, CancellationToken ct = default)
    {
        try
        {
            using var response = await PostAsync("Verifications",
                new() { ["To"] = e164Phone, ["Channel"] = "sms" }, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Twilio Verify: code sent to {Phone}", PhoneNumbers.Mask(e164Phone));
                return SmsSendStatus.Sent;
            }

            var (code, message) = ReadError(body);
            _logger.LogError("Twilio Verify could not send a code to {Phone}: HTTP {Status}, Twilio error {Code}: {Message}{Hint}",
                PhoneNumbers.Mask(e164Phone), (int)response.StatusCode, code, message, Hint(code));

            return code switch
            {
                60200 or 60205 or 21211 or 21614 => SmsSendStatus.InvalidNumber,
                60203 => SmsSendStatus.TooManyRequests,
                _ when response.StatusCode == HttpStatusCode.TooManyRequests => SmsSendStatus.TooManyRequests,
                _ => SmsSendStatus.Failed,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Twilio Verify could not be reached to send a code to {Phone}", PhoneNumbers.Mask(e164Phone));
            return SmsSendStatus.Failed;
        }
    }

    public async Task<SmsCheckStatus> CheckCodeAsync(string e164Phone, string code, CancellationToken ct = default)
    {
        try
        {
            using var response = await PostAsync("VerificationCheck",
                new() { ["To"] = e164Phone, ["Code"] = code }, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                using var json = JsonDocument.Parse(body);
                var status = json.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
                return status == "approved" ? SmsCheckStatus.Approved : SmsCheckStatus.Incorrect;
            }

            // 404: Twilio deletes a verification once it expires, is approved, or
            // reaches the maximum number of wrong attempts.
            if (response.StatusCode == HttpStatusCode.NotFound) return SmsCheckStatus.ExpiredOrMissing;

            var (errCode, message) = ReadError(body);
            if (errCode == 60202) return SmsCheckStatus.ExpiredOrMissing;   // max check attempts
            _logger.LogError("Twilio Verify check failed for {Phone}: HTTP {Status}, Twilio error {Code}: {Message}{Hint}",
                PhoneNumbers.Mask(e164Phone), (int)response.StatusCode, errCode, message, Hint(errCode));
            return SmsCheckStatus.Failed;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Twilio Verify could not be reached to check a code for {Phone}", PhoneNumbers.Mask(e164Phone));
            return SmsCheckStatus.Failed;
        }
    }

    private Task<HttpResponseMessage> PostAsync(string resource, Dictionary<string, string> form, CancellationToken ct)
    {
        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/v2/Services/{Uri.EscapeDataString(_options.VerifyServiceSid)}/{resource}";
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return _http.SendAsync(request, ct);
    }

    private static (int Code, string Message) ReadError(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var code = root.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
            var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            return (code, message);
        }
        catch (JsonException)
        {
            return (0, body.Length > 200 ? body[..200] : body);
        }
    }

    /// <summary>Plain-English pointers for the errors people hit when setting up.</summary>
    private static string Hint(int code) => code switch
    {
        20003 => " → Check Twilio:AccountSid and Twilio:AuthToken in your user secrets.",
        20404 => " → Check Twilio:VerifyServiceSid (it starts with VA).",
        21608 or 21610 => " → On a Twilio TRIAL account you can only text numbers you've verified: Console → Phone Numbers → Verified Caller IDs.",
        60200 or 21211 or 21614 => " → The user's phone number isn't a valid mobile number.",
        60203 => " → Too many codes sent to this number recently; Twilio will allow more after a while.",
        _ => "",
    };
}

/// <summary>
/// Used when Twilio isn't configured. Generates the code itself and, instead of
/// texting it, writes it to the console log and to App_Data/DevSms/ so the
/// whole reset flow can be tested without a Twilio account or credits.
/// Mirrors Twilio's rules: 6 digits, 10-minute expiry, 5 wrong tries.
/// </summary>
public class DevelopmentSmsVerificationService : ISmsVerificationService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private const int MaxChecks = 5;

    private readonly ConcurrentDictionary<string, Pending> _pending = new();
    private readonly string _folder;
    private readonly ILogger<DevelopmentSmsVerificationService> _logger;

    private sealed class Pending
    {
        public required string CodeHash { get; init; }
        public required DateTime ExpiresAt { get; init; }
        public int WrongTries;
    }

    public DevelopmentSmsVerificationService(IWebHostEnvironment env, ILogger<DevelopmentSmsVerificationService> logger)
    {
        _folder = Path.Combine(env.ContentRootPath, "App_Data", "DevSms");
        _logger = logger;
    }

    public async Task<SmsSendStatus> SendCodeAsync(string e164Phone, CancellationToken ct = default)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _pending[e164Phone] = new Pending { CodeHash = Hash(code), ExpiresAt = DateTime.UtcNow.Add(Lifetime) };

        Directory.CreateDirectory(_folder);
        var file = Path.Combine(_folder, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}_{e164Phone.TrimStart('+')}.txt");
        await File.WriteAllTextAsync(file, $"To: {e164Phone}\nYour CareerSync AI verification code is: {code}\n", ct);

        _logger.LogWarning("Twilio is not configured, so no SMS was sent. Verification code for {Phone}: {Code} (also saved to {File})",
            e164Phone, code, file);
        return SmsSendStatus.Sent;
    }

    public Task<SmsCheckStatus> CheckCodeAsync(string e164Phone, string code, CancellationToken ct = default)
    {
        if (!_pending.TryGetValue(e164Phone, out var p) || p.ExpiresAt < DateTime.UtcNow)
        {
            _pending.TryRemove(e164Phone, out _);
            return Task.FromResult(SmsCheckStatus.ExpiredOrMissing);
        }

        if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(code.Trim())), Encoding.ASCII.GetBytes(p.CodeHash)))
        {
            _pending.TryRemove(e164Phone, out _);   // single use
            return Task.FromResult(SmsCheckStatus.Approved);
        }

        if (Interlocked.Increment(ref p.WrongTries) >= MaxChecks) _pending.TryRemove(e164Phone, out _);
        return Task.FromResult(SmsCheckStatus.Incorrect);
    }

    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(code)));
}
