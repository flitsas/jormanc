namespace Flit.Modules.Procedures.Domain.Services;

public sealed record CuotaValidationResult(bool IsValid, decimal CurrentSum, decimal ProposedSum)
{
    public static CuotaValidationResult Ok() => new(true, 0, 0);

    public static CuotaValidationResult Exceeds(decimal currentSum, decimal proposed) =>
        new(false, currentSum, currentSum + proposed);
}

/// <summary>Valida que la suma de cuotas de copropietarios no exceda 100%.</summary>
public static class CuotaValidator
{
    public static CuotaValidationResult Validate(decimal proposedCuota, IEnumerable<decimal?> existingCuotas)
    {
        var currentSum = existingCuotas.Where(c => c.HasValue).Sum(c => c!.Value);
        var newSum = currentSum + proposedCuota;
        return newSum > 100m
            ? CuotaValidationResult.Exceeds(currentSum, proposedCuota)
            : CuotaValidationResult.Ok();
    }
}
