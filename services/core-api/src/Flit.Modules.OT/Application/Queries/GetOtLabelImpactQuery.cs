namespace Flit.Modules.OT.Application.Queries;

public sealed record GetOtLabelImpactQuery(Guid OtId, Guid LabelId, Guid TenantId);
