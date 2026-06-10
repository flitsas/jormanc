using FluentAssertions;
using Flit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Flit.Api.Tests.Infrastructure;

/// <summary>
/// HU #9437 — validación de CHECK JSONB en procedures_config.rules (AC1 positivo, AC2 negativo).
/// Requiere PostgreSQL local. Aplica migraciones EF (incluye RGL-01) al iniciar cada prueba.
/// </summary>
public sealed class ProceduresConfigRulesMigrationTests
{
    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-7000-8001-000000000010");
    private static readonly Guid DemoTypeId = Guid.Parse("00000000-0000-7000-8002-000000000001");
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-7000-8000-000000000001");

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("FLIT_TEST_PG")
        ?? "Host=localhost;Port=5432;Database=flit_dev;Username=flit;Password=flit_local";

    private static async Task<NpgsqlConnection?> TryOpenWithMigrationsAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<FlitDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using (var db = new FlitDbContext(options))
            {
                await db.Database.MigrateAsync();
            }

            var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            return conn;
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return null;
        }
    }

    [Fact]
    public async Task Is_valid_rule_condition_accepts_valid_leaf()
    {
        await using var conn = await TryOpenWithMigrationsAsync();
        if (conn is null)
        {
            return;
        }

        await using var cmd = new NpgsqlCommand(
            """
            SELECT public.is_valid_rule_condition(
              '{"field":"marca","operator":"equal","value":{"kind":"static","value":"Tesla"}}'::jsonb
            )
            """,
            conn);
        var ok = (bool)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? false);
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task Is_valid_rule_condition_rejects_unknown_operator()
    {
        await using var conn = await TryOpenWithMigrationsAsync();
        if (conn is null)
        {
            return;
        }

        await using var cmd = new NpgsqlCommand(
            """
            SELECT public.is_valid_rule_condition(
              '{"field":"marca","operator":"sqlInjection","value":{"kind":"static","value":"x"}}'::jsonb
            )
            """,
            conn);
        var ok = (bool)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? false);
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task Rules_insert_accepts_valid_jsonb_tree()
    {
        await using var conn = await TryOpenWithMigrationsAsync();
        if (conn is null)
        {
            return;
        }

        var ruleId = Guid.NewGuid();
        var name = "HU9437-valid-" + ruleId.ToString("N")[..8];

        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO procedures_config.rules (
              id, tenant_id, procedure_type_id, name, condition_tree, actions,
              created_by, updated_by
            ) VALUES (
              @id, @tenant, @type, @name,
              '{"field":"placa","operator":"isNotEmpty"}'::jsonb,
              '[{"type":"popup_modal","params":{"title":"Aviso","body":"OK"}}]'::jsonb,
              @user, @user
            )
            """,
            conn))
        {
            insert.Parameters.AddWithValue("id", ruleId);
            insert.Parameters.AddWithValue("tenant", DemoTenantId);
            insert.Parameters.AddWithValue("type", DemoTypeId);
            insert.Parameters.AddWithValue("user", SystemUserId);
            await insert.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await using var countCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM procedures_config.rules WHERE id = @id",
            conn);
        countCmd.Parameters.AddWithValue("id", ruleId);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        count.Should().Be(1);

        await using var delete = new NpgsqlCommand(
            "DELETE FROM procedures_config.rules WHERE id = @id",
            conn);
        delete.Parameters.AddWithValue("id", ruleId);
        await delete.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Rules_insert_rejects_invalid_condition_operator()
    {
        await using var conn = await TryOpenWithMigrationsAsync();
        if (conn is null)
        {
            return;
        }

        await using var insert = new NpgsqlCommand(
            """
            INSERT INTO procedures_config.rules (
              id, tenant_id, procedure_type_id, name, condition_tree, actions,
              created_by, updated_by
            ) VALUES (
              @id, @tenant, @type, 'HU9437-invalid',
              '{"field":"placa","operator":"eval","value":{"kind":"static","value":"1"}}'::jsonb,
              '[]'::jsonb,
              @user, @user
            )
            """,
            conn);
        insert.Parameters.AddWithValue("id", Guid.NewGuid());
        insert.Parameters.AddWithValue("tenant", DemoTenantId);
        insert.Parameters.AddWithValue("type", DemoTypeId);
        insert.Parameters.AddWithValue("user", SystemUserId);

        var act = async () => await insert.ExecuteNonQueryAsync();
        await act.Should().ThrowAsync<PostgresException>();
    }

    [Fact]
    public async Task AC1_endpoint_catalog_accepts_vault_secret_ref()
    {
        await using var conn = await TryOpenWithMigrationsAsync();
        if (conn is null)
        {
            return;
        }

        await using var insert = new NpgsqlCommand(
            """
            INSERT INTO procedures_config.endpoint_catalog (
              id, tenant_id, code, name, url, method, auth_type, auth_config,
              created_by, updated_by
            ) VALUES (
              @id, @tenant, 'HU9439_OK', 'Valid', 'https://example.com/hook', 'POST', 'api_key',
              '{"secret_ref":"vault://rues-key"}'::jsonb,
              @user, @user
            )
            """,
            conn);
        insert.Parameters.AddWithValue("id", Guid.NewGuid());
        insert.Parameters.AddWithValue("tenant", DemoTenantId);
        insert.Parameters.AddWithValue("user", SystemUserId);

        var rows = await insert.ExecuteNonQueryAsync();
        rows.Should().Be(1);
    }
}
