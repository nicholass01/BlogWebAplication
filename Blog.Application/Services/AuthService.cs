using Blog.Application.Abstractions.Persistence;
using Blog.Application.Abstractions.Security;
using Blog.Application.Contracts.Auth;
using Blog.Domain.Entities;
using FluentValidation;

namespace Blog.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _userRepository.GetByUsernameAsync(request.UsernameOrEmail, cancellationToken)
                   ?? await _userRepository.GetByEmailAsync(request.UsernameOrEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return null;
        }

        var token = _jwtTokenGenerator.Generate(user);
        return new AuthResult(user.Id, user.Username, token);
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var existing = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken)
                        ?? await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Usuário já existe.");
        }

        var user = new User
        {
            Id = Guid.Empty,
            Username = request.Username.Trim(),
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.IsAdmin ? UserRole.Admin : UserRole.Author,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createdId = await _userRepository.CreateAsync(user, cancellationToken);

        var createdUser = new User
        {
            Id = createdId,
            Username = user.Username,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Role = user.Role,
            CreatedAtUtc = user.CreatedAtUtc
        };

        var token = _jwtTokenGenerator.Generate(createdUser);
        return new AuthResult(createdUser.Id, createdUser.Username, token);
    }
}
