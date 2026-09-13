namespace MeuKanBan.Domain.Common;

/// <summary>
/// Regra de negocio violada por um payload estruturalmente valido.
/// Mapeada para HTTP 422 com codigo de regra estavel, permitindo que cliente
/// e testes identifiquem a regra sem depender de texto livre
/// (docs/specs/api-contracts.md).
/// </summary>
public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string ruleCode, string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleCode);
        RuleCode = ruleCode;
    }

    /// <summary>Codigo estavel da regra (ex.: "cards.card-archived").</summary>
    public string RuleCode { get; }
}
