namespace Flit.Modules.OT.Application.DTOs;

public sealed record OtOrganismDto(
    Guid Id,
    Guid TenantId,
    string Slug,
    string Name,
    string Mode,
    bool QuipuxEnabled,
    string? QuipuxConfig,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
