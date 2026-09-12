using MeuKanBan.Api.Configuration;

namespace MeuKanBan.Api.Tests;

public class PostgresConnectionStringFactoryTests
{
    private const string BaseConnectionString =
        "Host=postgres;Port=5432;Database=meukanban_db;Username=meukanban";

    [Fact]
    public void ApplyPasswordFile_CaminhoNulo_RetornaStringOriginal()
    {
        var result = PostgresConnectionStringFactory.ApplyPasswordFile(BaseConnectionString, null);

        Assert.Equal(BaseConnectionString, result);
    }

    [Fact]
    public void ApplyPasswordFile_CaminhoVazio_RetornaStringOriginal()
    {
        var result = PostgresConnectionStringFactory.ApplyPasswordFile(BaseConnectionString, "  ");

        Assert.Equal(BaseConnectionString, result);
    }

    [Fact]
    public void ApplyPasswordFile_ArquivoInexistente_FalhaExplicitaSemRevelarSegredo()
    {
        var caminhoInexistente = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var ex = Assert.Throws<InvalidOperationException>(
            () => PostgresConnectionStringFactory.ApplyPasswordFile(BaseConnectionString, caminhoInexistente));

        Assert.Contains("nao encontrado", ex.Message);
    }

    [Fact]
    public void ApplyPasswordFile_ArquivoValido_AplicaSenhaComTrimDeQuebraDeLinha()
    {
        var arquivo = CriaArquivoTemporario("senha-local-com;caractere-especial\n");

        try
        {
            var result = PostgresConnectionStringFactory.ApplyPasswordFile(BaseConnectionString, arquivo);

            var builder = new Npgsql.NpgsqlConnectionStringBuilder(result);
            Assert.Equal("senha-local-com;caractere-especial", builder.Password);
            Assert.Equal("postgres", builder.Host);
            Assert.Equal("meukanban_db", builder.Database);
        }
        finally
        {
            File.Delete(arquivo);
        }
    }

    [Fact]
    public void ApplyPasswordFile_ArquivoVazio_FalhaExplicita()
    {
        var arquivo = CriaArquivoTemporario("  \n");

        try
        {
            Assert.Throws<InvalidOperationException>(
                () => PostgresConnectionStringFactory.ApplyPasswordFile(BaseConnectionString, arquivo));
        }
        finally
        {
            File.Delete(arquivo);
        }
    }

    private static string CriaArquivoTemporario(string conteudo)
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.secret");
        File.WriteAllText(caminho, conteudo);
        return caminho;
    }
}
