namespace MeuKanBan.Api.Configuration;

/// <summary>
/// Validacao explicita de configuracao obrigatoria no boot
/// (docs/specs/api-contracts.md): ausencia ou placeholder proibido falha o
/// startup nomeando a chave ausente, sem nunca revelar o valor.
/// </summary>
public static class StartupConfiguration
{
    private static readonly string[] ForbiddenPlaceholders =
        ["changeme", "change_me", "change-me", "placeholder", "your-password"];

    /// <summary>
    /// Retorna a connection string obrigatoria ou falha o boot com mensagem clara.
    /// </summary>
    public static string RequireConnectionString(IConfiguration configuration, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configuracao obrigatoria ausente: ConnectionStrings:{name}. " +
                $"Forneca via variavel de ambiente ConnectionStrings__{name} ou user-secrets no desenvolvimento.");
        }

        if (ContainsPlaceholder(value))
        {
            throw new InvalidOperationException(
                $"Configuracao ConnectionStrings:{name} contem valor placeholder proibido. " +
                "Forneca um valor real via variavel de ambiente ou user-secrets.");
        }

        return value;
    }

    /// <summary>Detecta valores placeholder versionados por engano (case-insensitive).</summary>
    public static bool ContainsPlaceholder(string value) =>
        ForbiddenPlaceholders.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase));
}
