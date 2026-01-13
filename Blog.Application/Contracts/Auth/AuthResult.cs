namespace Blog.Application.Contracts.Auth;

public record AuthResult(Guid UserId, string Username, string Token);
