namespace MeuKanBan.Application.Common;

/// <summary>
/// Recurso inexistente ou ocultado pela politica de anti-enumeracao.
/// Mapeada para HTTP 404 com Problem Details seguro (docs/specs/api-contracts.md).
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>
/// Conflito de estado/versao ou unicidade concorrente: o estado esperado
/// diverge do estado atual, sem update parcial. Mapeada para HTTP 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
