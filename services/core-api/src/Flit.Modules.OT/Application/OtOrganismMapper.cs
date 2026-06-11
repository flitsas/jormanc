using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.DTOs;

namespace Flit.Modules.OT.Application;

internal static class OtOrganismMapper
{
    public static OtOrganismDto ToDto(OtOrganism entity) =>
        new(
            Id: entity.Id,
            TenantId: entity.TenantId,
            Slug: entity.Slug,
            Name: entity.Name,
            Mode: entity.Mode,
            QuipuxEnabled: entity.QuipuxEnabled,
            QuipuxConfig: entity.QuipuxConfig,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);
}
