using System.Text.Json;
using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Flit.Api.HostedServices;

/// <summary>
/// Catálogo demo idempotente para QA manual y regresión en DEV (tenant acme).
/// Solo activo en Development. Requiere que <see cref="DevSeedService"/> haya creado tenant/usuarios.
/// </summary>
public sealed class DevQaDemoSeedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment env,
    ILogger<DevQaDemoSeedService> logger) : IHostedService
{
    private static readonly Guid SystemSeedId = new("00000000-0000-0000-0000-000000000001");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlitDbContext>();

        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == "acme", cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("Dev QA seed omitido: tenant 'acme' no existe aún.");
            return;
        }

        await ApplySessionContextAsync(db, tenant.Id, cancellationToken);
        await SeedQaCatalogAsync(db, tenant.Id, cancellationToken);
    }

    private async Task SeedQaCatalogAsync(FlitDbContext db, Guid tenantId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var company = await db.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Nit == "900000001", ct);
        if (company is null)
        {
            logger.LogWarning("Dev QA seed omitido: compañía Acme (NIT 900000001) no encontrada.");
            return;
        }

        var operador = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == "operador@acme.com", ct);
        var createdBy = operador?.Id ?? SystemSeedId;

        // ── OT demo ───────────────────────────────────────────────────────────
        var ot = await db.OtOrganisms
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Slug == "ot-movilidad-bogota", ct);
        if (ot is null)
        {
            ot = new OtOrganism
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Slug = "ot-movilidad-bogota",
                Name = "Secretaría de Movilidad Bogotá (Demo)",
                Mode = "dashboard",
                QuipuxEnabled = false,
                CreatedAt = now,
                CreatedBy = SystemSeedId,
                UpdatedAt = now,
                UpdatedBy = SystemSeedId
            };
            db.OtOrganisms.Add(ot);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Dev QA seed: OT '{Slug}' creado", ot.Slug);
        }

        foreach (var (slug, label) in new[]
                 {
                     ("tarjeta-propiedad", "Tarjeta de propiedad"),
                     ("contrato-compraventa", "Contrato de compraventa")
                 })
        {
            var exists = await db.OtDocumentLabels
                .AnyAsync(l => l.OtId == ot.Id && l.Slug == slug, ct);
            if (!exists)
            {
                db.OtDocumentLabels.Add(new OtDocumentLabel
                {
                    Id = Guid.NewGuid(),
                    OtId = ot.Id,
                    TenantId = tenantId,
                    Slug = slug,
                    DisplayName = label,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = SystemSeedId,
                    UpdatedAt = now,
                    UpdatedBy = SystemSeedId
                });
            }
        }

        await db.SaveChangesAsync(ct);

        var otEnabled = await db.CompanyOtEnabled
            .AnyAsync(e => e.CompanyId == company.Id && e.OtSlug == ot.Slug, ct);
        if (!otEnabled)
        {
            db.CompanyOtEnabled.Add(new CompanyOtEnabled
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                OtSlug = ot.Slug,
                ProcedureFamily = "traspasos",
                IsEnabled = true
            });
            await db.SaveChangesAsync(ct);
        }

        // ── Tipos de documento ──────────────────────────────────────────────────
        var docCedulaVendedor = await EnsureDocumentTypeAsync(
            db, tenantId, "Cédula vendedor", "carga", now, ct);
        var docCedulaComprador = await EnsureDocumentTypeAsync(
            db, tenantId, "Cédula comprador", "carga", now, ct);
        var docContrato = await EnsureDocumentTypeAsync(
            db, tenantId, "Contrato compraventa", "generacion", now, ct);

        // ── Tipo de trámite demo (4 pasos) ──────────────────────────────────────
        var procedureType = await db.ProcedureTypes
            .IgnoreQueryFilters()
            .Include(pt => pt.Steps)
            .ThenInclude(s => s.FormSections)
            .ThenInclude(sec => sec.FormFields)
            .Include(pt => pt.ActorDefinitions)
            .FirstOrDefaultAsync(pt => pt.TenantId == tenantId && pt.Slug == "traspaso-vehicular-demo", ct);

        if (procedureType is null)
        {
            procedureType = new ProcedureType
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Slug = "traspaso-vehicular-demo",
                Name = "Traspaso vehicular (Demo QA)",
                Family = "traspasos",
                Scope = "global",
                VehicleQueryKey = "placa",
                Version = 1,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = SystemSeedId,
                UpdatedAt = now,
                UpdatedBy = SystemSeedId
            };
            db.ProcedureTypes.Add(procedureType);
            await db.SaveChangesAsync(ct);

            var stepDefs = new[]
            {
                (1, "Datos del vehículo", "form"),
                (2, "Vendedor", "form"),
                (3, "Comprador", "form"),
                (4, "Revisión y envío", "review")
            };

            foreach (var (order, name, stepType) in stepDefs)
            {
                var step = new ProcedureStep
                {
                    Id = Guid.NewGuid(),
                    ProcedureTypeId = procedureType.Id,
                    TenantId = tenantId,
                    OrderIndex = order,
                    Name = name,
                    StepType = stepType,
                    IsRequired = true,
                    CreatedAt = now,
                    CreatedBy = SystemSeedId,
                    UpdatedAt = now,
                    UpdatedBy = SystemSeedId
                };
                db.ProcedureSteps.Add(step);

                if (order == 1)
                {
                    var section = new FormSection
                    {
                        Id = Guid.NewGuid(),
                        StepId = step.Id,
                        TenantId = tenantId,
                        OrderIndex = 1,
                        Slug = "vehiculo",
                        Name = "Vehículo",
                        IsCollapsible = false,
                        CreatedAt = now,
                        CreatedBy = SystemSeedId,
                        UpdatedAt = now,
                        UpdatedBy = SystemSeedId
                    };
                    db.FormSections.Add(section);
                    db.FormFields.Add(new FormField
                    {
                        Id = Guid.NewGuid(),
                        SectionId = section.Id,
                        TenantId = tenantId,
                        OrderIndex = 1,
                        Slug = "placa",
                        Name = "Placa",
                        FieldType = "text",
                        IsRequired = true,
                        Config = """{"placeholder":"ABC123"}""",
                        CreatedAt = now,
                        CreatedBy = SystemSeedId,
                        UpdatedAt = now,
                        UpdatedBy = SystemSeedId
                    });
                }
            }

            db.ActorDefinitions.Add(new ActorDefinition
            {
                Id = Guid.NewGuid(),
                ProcedureTypeId = procedureType.Id,
                TenantId = tenantId,
                Role = "vendedor",
                AllowedNature = "natural",
                MinCount = 1,
                MaxCount = 1,
                IsRequired = true,
                OrderIndex = 1,
                CreatedAt = now,
                CreatedBy = SystemSeedId,
                UpdatedAt = now,
                UpdatedBy = SystemSeedId
            });
            db.ActorDefinitions.Add(new ActorDefinition
            {
                Id = Guid.NewGuid(),
                ProcedureTypeId = procedureType.Id,
                TenantId = tenantId,
                Role = "comprador",
                AllowedNature = "natural",
                MinCount = 1,
                MaxCount = 1,
                IsRequired = true,
                OrderIndex = 2,
                CreatedAt = now,
                CreatedBy = SystemSeedId,
                UpdatedAt = now,
                UpdatedBy = SystemSeedId
            });

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Dev QA seed: tipo de trámite '{Slug}' creado", procedureType.Slug);

            procedureType = await db.ProcedureTypes
                .IgnoreQueryFilters()
                .Include(pt => pt.Steps)
                .ThenInclude(s => s.FormSections)
                .ThenInclude(sec => sec.FormFields)
                .Include(pt => pt.ActorDefinitions)
                .Include(pt => pt.ApiConnectors)
                .FirstAsync(pt => pt.Id == procedureType.Id, ct);
        }

        await EnsureProcedureTypeDocumentAsync(
            db, tenantId, procedureType.Id, docCedulaVendedor.Id, 1, now, ct);
        await EnsureProcedureTypeDocumentAsync(
            db, tenantId, procedureType.Id, docCedulaComprador.Id, 2, now, ct);
        await EnsureProcedureTypeDocumentAsync(
            db, tenantId, procedureType.Id, docContrato.Id, 3, now, ct);

        var snapshot = await db.ProcedureTypeSnapshots
            .FirstOrDefaultAsync(
                s => s.ProcedureTypeId == procedureType.Id && s.Version == procedureType.Version, ct);
        if (snapshot is null)
        {
            snapshot = new ProcedureTypeSnapshot
            {
                Id = Guid.NewGuid(),
                ProcedureTypeId = procedureType.Id,
                Version = procedureType.Version,
                SnapshotJson = JsonSerializer.Serialize(ProcedureTypeMapper.ToDto(procedureType), JsonOptions),
                CreatedAt = now
            };
            db.ProcedureTypeSnapshots.Add(snapshot);
            await db.SaveChangesAsync(ct);
        }

        // ── Trámites demo (dashboard / grilla) ──────────────────────────────────
        await EnsureDemoProcedureAsync(
            db, tenantId, company.Id, ot.Id, procedureType.Id, snapshot.Id,
            "TRASP-ACO-0001", "draft", createdBy, now, ct);
        await EnsureDemoProcedureAsync(
            db, tenantId, company.Id, ot.Id, procedureType.Id, snapshot.Id,
            "TRASP-ACO-0002", "submitted", createdBy, now.AddHours(-2), ct);
        await EnsureDemoProcedureAsync(
            db, tenantId, company.Id, ot.Id, procedureType.Id, snapshot.Id,
            "TRASP-ACO-0003", "approved", createdBy, now.AddDays(-1), ct);

        logger.LogInformation(
            "Dev QA seed catálogo listo — tenant acme · OT {OtSlug} · trámite {PtSlug} · 3 procedures demo",
            ot.Slug,
            procedureType.Slug);
    }

    private static async Task<DocumentType> EnsureDocumentTypeAsync(
        FlitDbContext db,
        Guid tenantId,
        string name,
        string loadType,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var doc = await db.DocumentTypes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Name == name, ct);
        if (doc is not null)
            return doc;

        doc = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            LoadType = loadType,
            AllowedFormats = """["pdf","jpg","png"]""",
            MaxSizeMb = 10,
            IsReusable = true,
            CreatedAt = now,
            CreatedBy = SystemSeedId,
            UpdatedAt = now,
            UpdatedBy = SystemSeedId
        };
        db.DocumentTypes.Add(doc);
        await db.SaveChangesAsync(ct);
        return doc;
    }

    private static async Task EnsureProcedureTypeDocumentAsync(
        FlitDbContext db,
        Guid tenantId,
        Guid procedureTypeId,
        Guid documentTypeId,
        int orderIndex,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var exists = await db.ProcedureTypeDocuments
            .IgnoreQueryFilters()
            .AnyAsync(
                ptd => ptd.ProcedureTypeId == procedureTypeId && ptd.DocumentTypeId == documentTypeId,
                ct);
        if (exists)
            return;

        db.ProcedureTypeDocuments.Add(new ProcedureTypeDocument
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = procedureTypeId,
            DocumentTypeId = documentTypeId,
            TenantId = tenantId,
            IsRequired = true,
            OrderIndex = orderIndex,
            AllowPartialConsolidation = false,
            CreatedAt = now,
            CreatedBy = SystemSeedId,
            UpdatedAt = now,
            UpdatedBy = SystemSeedId
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureDemoProcedureAsync(
        FlitDbContext db,
        Guid tenantId,
        Guid companyId,
        Guid otId,
        Guid procedureTypeId,
        Guid snapshotId,
        string compositeId,
        string status,
        Guid createdBy,
        DateTimeOffset createdAt,
        CancellationToken ct)
    {
        var exists = await db.Procedures
            .IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == tenantId && p.CompositeId == compositeId, ct);
        if (exists)
            return;

        var procedure = new Procedure
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyId = companyId,
            OtId = otId,
            ProcedureTypeId = procedureTypeId,
            ProcedureTypeSnapshotId = snapshotId,
            CompositeId = compositeId,
            Status = status,
            CurrentStepOrder = status == "draft" ? 1 : 4,
            StepData = """{"1":{"placa":"FLS123"}}""",
            SubmittedAt = status is "submitted" or "approved" ? createdAt.AddMinutes(30) : null,
            ApprovedAt = status == "approved" ? createdAt.AddHours(4) : null,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = createdAt,
            UpdatedBy = createdBy
        };
        db.Procedures.Add(procedure);
        await db.SaveChangesAsync(ct);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task ApplySessionContextAsync(FlitDbContext db, Guid tenantId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.tenant_id', {0}, false), set_config('app.user_id', {1}, false)",
            tenantId.ToString(),
            SystemSeedId.ToString());
    }
}
