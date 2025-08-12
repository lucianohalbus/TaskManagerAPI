using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagerApi.Models;
using TaskManagerApi.Data;

namespace TaskManagerApi.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TaskManagerContext _context;

        public AuthController(IConfiguration configuration, TaskManagerContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User login)
        {
            // Aqui você pode validar no banco real (não use assim em produção!)
            var user = _context.Users.FirstOrDefault(u => u.Username == login.Username && u.Password == login.Password);
            if (user == null) return Unauthorized();

            // Cria claims (informações codificadas no token)
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var keyString = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString) || keyString.Length < 32)
                throw new Exception("JWT Key not found or invalid length! Configure via appsettings or environment variable.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { token = tokenString });
        }
    }
}
