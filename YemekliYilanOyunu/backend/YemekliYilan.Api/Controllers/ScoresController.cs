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
            .Take(15)
            .Select(x => new
            {
                username = x.AppUser.Username,
                score = x.Value
            })
            .ToListAsync();

        return Ok(scores);
    }

    [Authorize]
    [HttpPost("session/start")]
    public async Task<IActionResult> StartGameSession()
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");
            }

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return BadRequest($"Kullanıcı ID değeri geçersiz: {userIdClaim}");
            }

            var userExists = await _context.Users.AnyAsync(x => x.Id == userId);

            if (!userExists)
            {
                return BadRequest($"Kullanıcı bulunamadı. UserId: {userId}");
            }

            var session = new GameSession
            {
                AppUserId = userId,
                StartedAt = DateTime.UtcNow,
                FinishedAt = null,
                LastFoodAt = null,
                IsCompleted = false,
                LastSubmittedScore = 0
            };

            _context.GameSessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Oyun oturumu başlatıldı.",
                gameSessionId = session.Id,
                startedAt = session.StartedAt
            });
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;

            return StatusCode(500, $"Oyun oturumu başlatılırken sunucu hatası oluştu: {detail}");
        }
    }

    [Authorize]
    [HttpPost("session/food")]
    public async Task<IActionResult> ReportFoodCollected(ScoreDto dto)
    {
        const int GridSize = 16;
        const int InitialSnakeLength = 3;
        const int MaxPossibleScore = GridSize * GridSize - InitialSnakeLength;

        const double MinimumSecondsBetweenFoods = 0.55;
        const int MaxSessionMinutes = 20;

        if (dto.GameSessionId <= 0)
        {
            return BadRequest("Geçersiz oyun oturumu.");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Unauthorized("Kullanıcı bilgisi bulunamadı.");
        }

        var userId = int.Parse(userIdClaim);

        var session = await _context.GameSessions
            .FirstOrDefaultAsync(x =>
                x.Id == dto.GameSessionId &&
                x.AppUserId == userId
            );

        if (session is null)
        {
            return BadRequest("Oyun oturumu bulunamadı veya kullanıcıyla eşleşmiyor.");
        }

        if (session.IsCompleted)
        {
            return BadRequest("Bu oyun oturumu daha önce tamamlanmış.");
        }

        var now = DateTime.UtcNow;
        var elapsedSession = now - session.StartedAt;

        if (elapsedSession.TotalMinutes > MaxSessionMinutes)
        {
            session.IsCompleted = true;
            session.FinishedAt = now;

            await _context.SaveChangesAsync();

            return BadRequest("Oyun oturumunun süresi dolmuş.");
        }

        if (session.LastSubmittedScore >= MaxPossibleScore)
        {
            return BadRequest("Maksimum skor sınırına ulaşıldı.");
        }

        if (session.LastFoodAt is not null)
        {
            var elapsedSinceLastFood = now - session.LastFoodAt.Value;

            if (elapsedSinceLastFood.TotalSeconds < MinimumSecondsBetweenFoods)
            {
                return BadRequest("Yemek toplama hızı geçersiz görünüyor.");
            }
        }

        session.LastSubmittedScore += 1;
        session.LastFoodAt = now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Yemek toplama bildirimi kabul edildi.",
            serverScore = session.LastSubmittedScore
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SaveScore(ScoreDto dto)
    {
        const int GridSize = 16;
        const int InitialSnakeLength = 3;
        const int MaxPossibleScore = GridSize * GridSize - InitialSnakeLength;

        const int MaxSessionMinutes = 20;

        if (dto.Score < 0)
        {
            return BadRequest("Skor negatif olamaz.");
        }

        if (dto.Score > MaxPossibleScore)
        {
            return BadRequest("Geçersiz skor değeri.");
        }

        if (dto.GameSessionId <= 0)
        {
            return BadRequest("Geçersiz oyun oturumu.");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Unauthorized("Kullanıcı bilgisi bulunamadı.");
        }

        var userId = int.Parse(userIdClaim);

        var session = await _context.GameSessions
            .FirstOrDefaultAsync(x =>
                x.Id == dto.GameSessionId &&
                x.AppUserId == userId
            );

        if (session is null)
        {
            return BadRequest("Oyun oturumu bulunamadı veya kullanıcıyla eşleşmiyor.");
        }

        if (session.IsCompleted)
        {
            return BadRequest("Bu oyun oturumu daha önce tamamlanmış.");
        }

        var now = DateTime.UtcNow;
        var elapsed = now - session.StartedAt;

        if (elapsed.TotalMinutes > MaxSessionMinutes)
        {
            session.IsCompleted = true;
            session.FinishedAt = now;

            await _context.SaveChangesAsync();

            return BadRequest("Oyun oturumunun süresi dolmuş.");
        }

        if (dto.Score != session.LastSubmittedScore)
        {
            return BadRequest("Gönderilen skor, sunucu tarafından doğrulanan skorla eşleşmiyor.");
        }

        var verifiedScore = session.LastSubmittedScore;

        session.IsCompleted = true;
        session.FinishedAt = now;

        var existingScore = await _context.Scores
            .FirstOrDefaultAsync(x => x.AppUserId == userId);

        if (existingScore is null)
        {
            var newScore = new Score
            {
                AppUserId = userId,
                Value = verifiedScore,
                UpdatedAt = now
            };

            _context.Scores.Add(newScore);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "İlk skor güvenli şekilde kaydedildi.",
                score = verifiedScore,
                bestScore = verifiedScore
            });
        }

        if (verifiedScore > existingScore.Value)
        {
            existingScore.Value = verifiedScore;
            existingScore.UpdatedAt = now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Yeni en yüksek skor güvenli şekilde kaydedildi.",
                score = verifiedScore,
                bestScore = existingScore.Value
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Skor önceki en yüksek skoru geçemediği için güncellenmedi.",
            score = verifiedScore,
            bestScore = existingScore.Value
        });
    }
}