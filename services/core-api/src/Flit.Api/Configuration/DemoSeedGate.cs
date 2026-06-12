namespace Flit.Api.Configuration;

/// <summary>
/// Controla si los hosted services de seed demo deben ejecutarse.
/// Local: <see cref="IHostEnvironment.IsDevelopment"/>. VPS DEV: <c>Flit:SeedDemoData=true</c>.
/// </summary>
internal static class DemoSeedGate
{
    public const string ConfigKey = "Flit:SeedDemoData";

    public static bool ShouldRun(IHostEnvironment env, IConfiguration config)
    {
        var enabled = env.IsDevelopment() || config.GetValue(ConfigKey, false);
        return enabled;
    }
}
