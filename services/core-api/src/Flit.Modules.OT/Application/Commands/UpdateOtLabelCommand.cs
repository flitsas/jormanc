namespace Flit.Modules.OT.Application.Commands;

public sealed record UpdateOtLabelCommand(
    Guid OtId,
    Guid LabelId,
    Guid TenantId,
    Guid RequestedByUserId,
    string DisplayName,
    bool? IsActive);
