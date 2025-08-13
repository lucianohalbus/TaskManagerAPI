using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Data;
using TaskManagerApi.Dtos;
using TaskManagerApi.Models;

namespace TaskManagerApi.Controllers.V2
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class TaskItemController : ControllerBase
    {
        private readonly TaskManagerContext _context;
        public TaskItemController(TaskManagerContext context) => _context = context;

        private int GetUserId()
        {
            //try NameIdentifier (ClaimTypes.NameIdentifier) e 'sub'
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(idStr))
                throw new UnauthorizedAccessException("User id claim not found.");
            return int.Parse(idStr);
        }

        // GET /api/v2/taskitem -> for logged user
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetMine()
        {
            var userId = GetUserId();

            var items = await _context.TaskItems
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            return Ok(items);
        }

        // GET /api/v2/taskitem/{id} -> only for specific users
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskItem>> GetById(int id)
        {
            var userId = GetUserId();

            var item = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (item is null) return NotFound();
            return Ok(item);
        }

        // POST /api/v2/taskitem
        [HttpPost]
        public async Task<ActionResult<TaskItem>> Create([FromBody] TaskItemCreateDto dto)
        {
            var userId = GetUserId();

            var entity = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                IsCompleted = dto.IsCompleted,
                UserId = userId
            };

            _context.TaskItems.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = entity.Id, version = "2.0" },
                entity
            );
        }

        // PUT /api/v2/taskitem/{id} (partial update through DTO)
        [HttpPut("{id:int}")]
        public async Task<ActionResult<TaskItem>> Update(int id, [FromBody] TaskItemUpdateDto dto)
        {
            var userId = GetUserId();

            var item = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (item is null) return NotFound();

            if (dto.Title is not null) item.Title = dto.Title;
            if (dto.Description is not null) item.Description = dto.Description;
            if (dto.IsCompleted.HasValue) item.IsCompleted = dto.IsCompleted.Value;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // DELETE /api/v2/taskitem/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();

            var item = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (item is null) return NotFound();

            _context.TaskItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
