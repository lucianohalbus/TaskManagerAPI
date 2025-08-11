using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Data;
using TaskManagerApi.Models;
using TaskManagerApi.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace TaskManagerApi.Controllers.V2
{
    public record UserDto(int Id, string Name, string Email, string Username);
    public record RegisterDto(string Name, string Email, string Username, string Password);
    public record LoginDto(string Email, string Password);

    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly TaskManagerContext _ctx;
        private readonly IConfiguration _config;
        public UserController(TaskManagerContext ctx) => _ctx = ctx;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
            => Ok(await _ctx.Users
                .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Username))
                .ToListAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var u = await _ctx.Users.FindAsync(id);
            return u is null ? NotFound() : new UserDto(u.Id, u.Name, u.Email, u.Username);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto input)
        {
            if (await _ctx.Users.AnyAsync(u => u.Email == input.Email))
                return Conflict("Email already in use.");

            var (hash, salt) = PasswordUtils.Hash(input.Password);
            var user = new User
            {
                Name = input.Name,
                Email = input.Email,
                Username = input.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                Password = null
            };
            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { version = "2.0", id = user.Id },
                new UserDto(user.Id, user.Name, user.Email, user.Username));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto input)
        {
            var user = await _ctx.Users.SingleOrDefaultAsync(u => u.Email == input.Email);
            if (user is null) return Unauthorized();

            // Migração automática de v1 para v2
            if (user.PasswordHash is null || user.PasswordSalt is null)
            {
                if (user.Password is null || user.Password != input.Password) return Unauthorized();
                var (h, s) = PasswordUtils.Hash(input.Password);
                user.PasswordHash = h; 
                user.PasswordSalt = s; 
                user.Password = null;
                await _ctx.SaveChangesAsync();
            }
            else if (!PasswordUtils.Verify(input.Password, user.PasswordHash, user.PasswordSalt))
            {
                return Unauthorized();
            }

            // Gerar token JWT
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new 
            {
                user = new UserDto(user.Id, user.Name, user.Email, user.Username),
                token = tokenString
            });
        }
    }
}
