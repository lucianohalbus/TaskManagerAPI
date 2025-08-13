using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagerApi.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; } = false;
        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
    }
}

