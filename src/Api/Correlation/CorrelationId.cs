namespace MeuKanBan.Api.Correlation;

/// <summary>
/// Contrato do correlation id (docs/specs/api-contracts.md): o cliente pode
/// enviar um id valido; na ausencia, a API gera um. Valores invalidos nao sao
/// propagados nem registrados, impedindo injecao em logs.
/// </summary>
public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ExtensionName = "correlationId";
    public const string ItemKey = "MeuKanBan.CorrelationId";
    public const int MaxLength = 64;

    public static string NewId() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Formato aceito: ate 64 caracteres alfanumericos, '-', '_' ou '.'.
    /// A restricao impede quebra de linha e chaves de formato em logs.
    /// </summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxLength
        && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
}
