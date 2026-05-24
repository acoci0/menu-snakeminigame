using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YemekliYilan.Api.Data;
using YemekliYilan.Api.Dtos;
using YemekliYilan.Api.Models;

namespace YemekliYilan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public ScoresController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetScores()
    {
        var scores = await _context.Scores
            .Include(x => x.AppUser)
            .OrderByDescending(x => x.Value)
            .Take(10)
            .Select(x => new
            {
                username = x.AppUser.Username,
                score = x.Value
            })
            .ToListAsync();

        return Ok(scores);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SaveScore(ScoreDto dto)
    {
        if (dto.Score < 0)
        {
            return BadRequest("Skor negatif olamaz.");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Unauthorized("Kullanıcı bilgisi bulunamadı.");
        }

        var userId = int.Parse(userIdClaim);

        var existingScore = await _context.Scores
            .FirstOrDefaultAsync(x => x.AppUserId == userId);

        if (existingScore is null)
        {
            var newScore = new Score
            {
                AppUserId = userId,
                Value = dto.Score,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Scores.Add(newScore);
        }
        else
        {
            existingScore.Value = dto.Score;
            existingScore.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Skor kaydedildi.",
            score = dto.Score
        });
    }
}