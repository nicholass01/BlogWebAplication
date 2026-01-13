using Blog.Application.Abstractions.Security;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Blog.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCryptNet.HashPassword(password);

    public bool Verify(string hash, string password) => BCryptNet.Verify(password, hash);
}
