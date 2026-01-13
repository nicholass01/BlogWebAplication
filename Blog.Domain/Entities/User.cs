namespace Blog.Domain.Entities;

public class User
{
    public Guid Id { get; init; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Author;
    public DateTime CreatedAtUtc { get; init; }
}
