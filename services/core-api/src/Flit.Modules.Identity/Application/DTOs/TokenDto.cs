namespace Flit.Modules.Identity.Application.DTOs;

/// <summary>Respuesta de POST /auth/login: token + perfil del usuario.</summary>
public sealed record TokenDto(
    string AccessToken,
    int ExpiresIn,
    UserProfileDto User);
