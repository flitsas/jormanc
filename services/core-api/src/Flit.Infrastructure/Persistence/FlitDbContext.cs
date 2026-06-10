using Microsoft.EntityFrameworkCore;

namespace Flit.Infrastructure.Persistence;

/// <summary>
/// DbContext unificado del Modular Monolith FLIT 2.0 (esqueleto base post-reset).
///
/// Arranca vacio: sin DbSets ni configuraciones. Cada nueva feature registra
/// sus entidades aqui (via DbSet + IEntityTypeConfiguration en este assembly)
/// y genera su migracion EF Core correspondiente.
/// </summary>
public sealed class FlitDbContext(DbContextOptions<FlitDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlitDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
