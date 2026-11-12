
namespace CareerSync.Models.ViewModels;

/// <summary>Graduate → My analyses: every CV-vs-job comparison they've run,
/// searchable from the top-bar search box.</summary>
public class MatchHistoryViewModel
{
    public PagedResult<MatchHistoryRow> Matches { get; set; } = new();
    public string? Search { get; set; }

    /// <summary>Learning resources whose skill or title matches the search
    /// (only filled when a search term is given).</summary>
    public List<LearningResource> MatchingCourses { get; set; } = new();
}

public class MatchHistoryRow
{
    public int MatchId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string CvFileName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Score { get; set; }
    public int GapCount { get; set; }
}

/// <summary>Full breakdown of a single match result.</summary>
public class MatchDetailViewModel
{
    public MatchResult Match { get; set; } = new();
    public string JobTitle { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string JobDescriptionText { get; set; } = string.Empty;
    public string CvFileName { get; set; } = string.Empty;
    public List<LearningResource> RecommendedResources { get; set; } = new();
    public MatchingWeights Weights { get; set; } = new();
}

