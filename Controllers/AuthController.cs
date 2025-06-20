using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Models;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Verifica se o usuário já existe
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("Usuário já existe.");

        // Cria o usuário (em produção, use BCrypt para hashear a senha!)
        var user = new User
        {
            Email = request.Email,
            PasswordHash = request.Password // ⚠️ Em projetos reais, use hashing!
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Usuário registrado com sucesso!" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Validação simplificada (em produção, compare hashes!)
        if (user == null || user.PasswordHash != request.Password)
            return Unauthorized("Credenciais inválidas.");

        // Gera o token JWT
        var token = GenerateJwtToken(user);
        return Ok(new { Token = token });
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("userId", user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

   [Authorize]
[HttpGet("profile")]
public async Task<IActionResult> GetProfile()
{
    // Verifica se a claim existe de forma segura
    var userIdClaim = User.FindFirst("userId");
    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        return Unauthorized("Token inválido: claim 'userId' não encontrada.");

    var user = await _context.Users
        .Where(u => u.Id == userId)
        .Select(u => new { u.Id, u.Email })
        .FirstOrDefaultAsync();
    
    if (user == null)
        return NotFound("Usuário não encontrado.");
    
    return Ok(user);
}

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("API está respondendo!");
    }
}

// DTOs (Data Transfer Objects)
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}