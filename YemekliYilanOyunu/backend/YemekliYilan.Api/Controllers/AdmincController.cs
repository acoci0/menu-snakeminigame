using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YemekliYilan.Api.Data;

namespace YemekliYilan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private bool IsAuthorized()
    {
        var adminKey = _configuration["Admin:Key"];
        var requestKey = Request.Headers["X-Admin-Key"].FirstOrDefault();

        return !string.IsNullOrWhiteSpace(adminKey) && requestKey == adminKey;
    }

    [HttpDelete("users/by-email")]
    public async Task<IActionResult> DeleteUserByEmail([FromQuery] string email)
    {
        if (!IsAuthorized())
        {
            return Unauthorized("Yetkisiz işlem.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email boş olamaz.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail);

        if (user is null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        var userScores = await _context.Scores
            .Where(x => x.AppUserId == user.Id)
            .ToListAsync();

        _context.Scores.RemoveRange(userScores);
        _context.Users.Remove(user);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Kullanıcı ve skorları silindi.",
            deletedUser = user.Username,
            deletedEmail = user.Email,
            deletedScores = userScores.Count
        });
    }

    [HttpDelete("users/all")]
    public async Task<IActionResult> DeleteAllUsers()
    {
        if (!IsAuthorized())
        {
            return Unauthorized("Yetkisiz işlem.");
        }

        var scores = await _context.Scores.ToListAsync();
        var users = await _context.Users.ToListAsync();

        _context.Scores.RemoveRange(scores);
        _context.Users.RemoveRange(users);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Tüm kullanıcılar ve skorlar silindi.",
            deletedUsers = users.Count,
            deletedScores = scores.Count
        });
    }
}