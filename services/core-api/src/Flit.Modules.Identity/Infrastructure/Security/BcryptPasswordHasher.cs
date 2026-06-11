using Flit.Modules.Identity.Domain.Interfaces;

namespace Flit.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Implementación de IPasswordHasher usando BCrypt (work factor 12).
/// HU-9769: "BCrypt para password verification".
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
}
