namespace CareerSync.Models;

/// <summary>Marker interface for models stored via the repository pattern.</summary>
public interface IEntity
{
    int Id { get; set; }
}
