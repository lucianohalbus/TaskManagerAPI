using System.Collections.Generic;

namespace TaskManagerApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;

        // v2 (hash/salt)
        public byte[]? PasswordHash { get; set; }
        public byte[]? PasswordSalt { get; set; }

        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}
