namespace YemekliYilan.Api.Models;

public class GameSession
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAt { get; set; }

    public DateTime? LastFoodAt { get; set; }

    public bool IsCompleted { get; set; }

    public int LastSubmittedScore { get; set; }
}