namespace YemekliYilan.Api.Models;

public class Score
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; } = null!;

    public int Value { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}