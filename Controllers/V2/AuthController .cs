using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagerApi.Data;
using TaskManagerApi.Security;

namespace TaskManagerApi.Controllers.V2
{
    public record AuthLoginDto(string Identifier, string Password);

    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly TaskManagerContext _ctx;

        public AuthController(IConfiguration config, TaskManagerContext ctx)
        {
            _config = config;
            _ctx = ctx;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthLoginDto input)
        {
            var id = input.Identifier;
            var user = await _ctx.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Email == id || u.Username == id);

            if (user is null)
                return Unauthorized();

            // Agora só aceitamos contas com Hash/Salt
            if (user.PasswordHash is null || user.PasswordSalt is null)
                return Unauthorized("Password reset required.");

            if (!PasswordUtils.Verify(input.Password, user.PasswordHash, user.PasswordSalt))
                return Unauthorized();

            // JWT
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            var keyString = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(keyString) || keyString.Length < 32)
                return Problem("JWT Key not found or too short. Configure Jwt:Key.", statusCode: 500);

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
                SecurityAlgorithms.HmacSha256);

            var expiresHours = _config.GetValue<int?>("Jwt:ExpiresHours") ?? 1;

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiresHours),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                user = new { user.Id, user.Name, user.Email, user.Username }
            });
        }
    }
}
