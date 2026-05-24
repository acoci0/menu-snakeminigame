namespace YemekliYilan.Api.Models;

public class AppUser
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string NormalizedUsername { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Score? Score { get; set; }
}