using CareerSync.Data;
using CareerSync.Services;
//using CareerSync.Hubs;
using CareerSync.Services;
using CareerSyncAI.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDbContext<CareerSyncDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();

//builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
//builder.Services.AddTransient<IAnalysisFeedbackEmailService, AnalysisFeedbackEmailService>();
//builder.Services.AddSingleton<ICvTextExtractor, CvTextExtractor>();
builder.Services.AddMemoryCache();

// SMS verification codes for password reset (Twilio Verify). Fill in the
// "Twilio" section with user-secrets — see TWILIO_SETUP.md. Without it, codes
// are written to the console and App_Data/DevSms instead of being texted, so
// the reset flow still works locally.
var twilioOptions = builder.Configuration.GetSection(TwilioOptions.SectionName).Get<TwilioOptions>() ?? new TwilioOptions();
builder.Services.AddSingleton(twilioOptions);
if (twilioOptions.IsConfigured)
    builder.Services.AddHttpClient<ISmsVerificationService, TwilioVerifyService>(http => http.Timeout = TimeSpan.FromSeconds(20));
else
    builder.Services.AddSingleton<ISmsVerificationService, DevelopmentSmsVerificationService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;

        // Sign a user out on their next request if an admin has deactivated
        // or deleted their account — otherwise their 14-day cookie would
        // keep working.
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var repository = context.HttpContext.RequestServices.GetRequiredService<IRepositoryWrapper>();

                if (!int.TryParse(idClaim, out var userId) || !repository.AppUser.IsActiveUser(userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            },
        };
    });

var app = builder.Build();

if (!twilioOptions.IsConfigured)
    app.Logger.LogWarning("Twilio is not configured — password-reset codes will be shown in this console and saved to {Folder} instead of being texted.",
        Path.Combine(app.Environment.ContentRootPath, "App_Data", "DevSms"));


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CareerSyncDbContext>();
    db.Database.Migrate();

    var repositoryWrapper = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();
    SeedIdentity.Seed(repositoryWrapper);
    SeedSkillDictionary.Seed(repositoryWrapper);
    SeedLearningResources.Seed(repositoryWrapper);
    SeedDemoData.Seed(repositoryWrapper);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//app.MapHub<MessagingHub>("/hubs/messaging");

app.Run();
