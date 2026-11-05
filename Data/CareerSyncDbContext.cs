using CareerSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace CareerSync.Data
{
    public class CareerSyncDbContext : DbContext
    {
        public CareerSyncDbContext(DbContextOptions<CareerSyncDbContext> options) : base(options) { }

        public DbSet<AppUser> AppUsers => Set<AppUser>();
        public DbSet<CV> CVs => Set<CV>();
        //public DbSet<JobPosting> JobPostings => Set<JobPosting>();
        //public DbSet<SavedJob> SavedJobs => Set<SavedJob>();
        public DbSet<JobDescription> JobDescriptions => Set<JobDescription>();
        public DbSet<MatchResult> MatchResults => Set<MatchResult>();
        public DbSet<LearningResource> LearningResources => Set<LearningResource>();
        public DbSet<SkillDictionaryTerm> SkillDictionaryTerms => Set<SkillDictionaryTerm>();
        public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<MatchingWeights> MatchingWeights => Set<MatchingWeights>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // List<string> properties (ExtractedSkills, RequiredSkills, MatchedSkills,
            // GapSkills, etc.) are stored as a JSON array in a single NVARCHAR(MAX)
            // column — matches CareerSyncAI_Schema.sql's design exactly.
            var stringListComparer = new ValueComparer<List<string>>(
                (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList());

            void ConfigureStringList(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<List<string>> prop)
            {
                prop.HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new())
                    .Metadata.SetValueComparer(stringListComparer);
            }

            modelBuilder.Entity<CV>(e =>
            {
                ConfigureStringList(e.Property(c => c.ExtractedSkills));
                ConfigureStringList(e.Property(c => c.ExtractedQualifications));
            });

            modelBuilder.Entity<JobDescription>(e =>
            {
                ConfigureStringList(e.Property(j => j.RequiredSkills));
                ConfigureStringList(e.Property(j => j.RequiredQualifications));
            });

            modelBuilder.Entity<MatchResult>(e =>
            {
                ConfigureStringList(e.Property(m => m.MatchedSkills));
                ConfigureStringList(e.Property(m => m.GapSkills));
            });

            //modelBuilder.Entity<JobPosting>(e =>
            //{
            //    e.HasIndex(j => new { j.IsActive, j.PostedAt });
            //    e.Property(j => j.Title).HasMaxLength(160).IsRequired();
            //    e.Property(j => j.Company).HasMaxLength(160).IsRequired();
            //    e.Property(j => j.Location).HasMaxLength(120);
            //    e.Property(j => j.EmploymentType).HasMaxLength(80);
            //    e.Property(j => j.ApplicationUrl).HasMaxLength(1500);
            //});

            //modelBuilder.Entity<SavedJob>(e =>
            //{
            //    e.HasIndex(s => new { s.AppUserId, s.JobPostingId }).IsUnique();
            //    e.HasOne<AppUser>().WithMany().HasForeignKey(s => s.AppUserId).OnDelete(DeleteBehavior.Cascade);
            //    e.HasOne<JobPosting>().WithMany().HasForeignKey(s => s.JobPostingId).OnDelete(DeleteBehavior.Cascade);
            //    e.Property(s => s.ApplicationStatus).HasMaxLength(25).IsRequired();
            //    e.Property(s => s.Notes).HasMaxLength(1000);
            //});
            // A graduate/admin's email is how they sign in — must be unique.
            modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        }
    }
}
