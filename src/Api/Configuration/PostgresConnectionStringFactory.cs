using Npgsql;

namespace MeuKanBan.Api.Configuration;

/// <summary>
/// Compoe a connection string do PostgreSQL com a senha lida de um arquivo
/// (Docker secret montado em /run/secrets), evitando segredo em variavel de
/// ambiente - que vazaria em `docker compose config`, `docker inspect` e
/// metadados do processo. Ver docs/specs/containers.md.
/// </summary>
public static class PostgresConnectionStringFactory
{
    /// <summary>
    /// Retorna a connection string com a senha aplicada a partir do arquivo.
    /// Caminho nulo/vazio retorna a string original (fluxo nativo com
    /// user-secrets ja contem a senha). Se o arquivo foi configurado, sua
    /// ausencia ou conteudo vazio falha de forma explicita no startup, sem
    /// revelar o valor.
    /// </summary>
    public static string ApplyPasswordFile(string connectionString, string? passwordFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (string.IsNullOrWhiteSpace(passwordFilePath))
        {
            return connectionString;
        }

        if (!File.Exists(passwordFilePath))
        {
            throw new InvalidOperationException(
                $"Arquivo de senha do PostgreSQL nao encontrado: '{passwordFilePath}'. " +
                "No Compose, o secret 'postgres_password' e montado a partir de " +
                "infra/postgres/secrets/postgres_password (ou POSTGRES_PASSWORD_FILE).");
        }

        var password = File.ReadAllText(passwordFilePath).Trim();
        if (password.Length == 0)
        {
            throw new InvalidOperationException(
                $"Arquivo de senha do PostgreSQL esta vazio: '{passwordFilePath}'.");
        }

        // NpgsqlConnectionStringBuilder cuida do escape de caracteres especiais (;, aspas).
        return new NpgsqlConnectionStringBuilder(connectionString) { Password = password }.ConnectionString;
    }
}
