using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YemekliYilan.Api.Data;
using YemekliYilan.Api.Dtos;
using YemekliYilan.Api.Models;
using YemekliYilan.Api.Services;

namespace YemekliYilan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var email = dto.Email;
        var password = dto.Password;
        var username = dto.Username.Trim();
        var normalizedUsername = UsernameNormalizer.Normalize(username);

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Mail adresi boş bırakılamaz.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return BadRequest("Şifre en az 6 karakter olmalıdır.");
        }

        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 18)
        {
            return BadRequest("Kullanıcı adı 3-18 karakter arasında olmalıdır.");
        }

        if (ProfanityService.ContainsBadWord(username))
        {
            return BadRequest("Bu kullanıcı adı uygun değil. Lütfen farklı bir kullanıcı adı seç.");
        }

        var emailExists = await _context.Users.AnyAsync(x => x.Email == email);

        if (emailExists)
        {
            return BadRequest("Bu mail adresi zaten kayıtlı.");
        }

        var usernameExists = await _context.Users.AnyAsync(x =>
            x.NormalizedUsername == normalizedUsername
        );

        if (usernameExists)
        {
            return BadRequest("Bu kullanıcı adı zaten alınmış.");
        }

        var user = new AppUser
        {
            Email = email,
            Username = username,
            NormalizedUsername = normalizedUsername
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateToken(user);

        return Ok(new
        {
            message = "Kayıt başarılı.",
            token,
            username = user.Username
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (user is null)
        {
            return Unauthorized("Mail veya şifre hatalı.");
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Mail veya şifre hatalı.");
        }

        var token = GenerateToken(user);

        return Ok(new
        {
            message = "Giriş başarılı.",
            token,
            username = user.Username
        });
    }

    private string GenerateToken(AppUser user)
    {
        var jwtKey = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new Exception("JWT Key bulunamadı.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}