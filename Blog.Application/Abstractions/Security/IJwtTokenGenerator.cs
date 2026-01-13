using Blog.Domain.Entities;

namespace Blog.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
