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
        private readonly TaskManagerContext _context;
        private readonly IConfiguration _config;
        public UserController(TaskManagerContext context, IConfiguration config) 
    => (_context, _config) = (context, config);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
            => Ok(await _context.Users
                .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Username))
                .ToListAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var u = await _context.Users.FindAsync(id);
            return u is null ? NotFound() : new UserDto(u.Id, u.Name, u.Email, u.Username);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto input)
        {
            if (await _context.Users.AnyAsync(u => u.Email == input.Email))
                return Conflict("Email already in use.");

            var (hash, salt) = PasswordUtils.Hash(input.Password);
            var user = new User
            {
                Name = input.Name,
                Email = input.Email,
                Username = input.Username,
                PasswordHash = hash,
                PasswordSalt = salt
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { version = "2.0", id = user.Id },
                new UserDto(user.Id, user.Name, user.Email, user.Username));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto input)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == input.Email);
            if (user is null) return Unauthorized();

            // Migração automática de v1 para v2
            if (user.PasswordHash is null || user.PasswordSalt is null)
            {
                return Unauthorized("Password reset required.");
            }

            if (!PasswordUtils.Verify(input.Password, user.PasswordHash, user.PasswordSalt))
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
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
        
         // PUT: api/User
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, User user)
        {
            if (id != user.Id) return BadRequest();
            _context.Entry(user).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TaskItems.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        // DELETE: api/User
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
