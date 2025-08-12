namespace TaskManagerApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;

        // v1 (legado – remova depois)
        public string? Password { get; set; }

        // v2
        public byte[]? PasswordHash { get; set; }
        public byte[]? PasswordSalt { get; set; }
    }
}