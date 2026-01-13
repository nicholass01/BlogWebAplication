namespace Blog.Application.Contracts.Auth;

public record LoginRequest(string UsernameOrEmail, string Password);
