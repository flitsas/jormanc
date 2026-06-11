namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Verificador de contraseñas. Abstracción sobre BCrypt para facilitar tests.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Verifica que el password en texto plano coincide con el hash almacenado.</summary>
    bool Verify(string password, string hash);

    /// <summary>Genera un hash del password en texto plano.</summary>
    string Hash(string password);
}
