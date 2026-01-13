namespace Blog.Application.Contracts.Auth;

public record RegisterRequest(string Username, string Email, string Password, bool IsAdmin = false);
