using MeuKanBan.Api.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MeuKanBan.Api.Tests;

/// <summary>
/// Contrato de configuracao externa (docs/specs/api-contracts.md): secret
/// obrigatorio ausente ou placeholder proibido falha o boot de forma
/// explicita, nomeando a chave sem revelar o valor.
/// </summary>
public class StartupConfigurationValidatorTests
{
    private static IConfiguration ConfigSemConnectionString() =>
        new ConfigurationBuilder().Build();

    private static IConfiguration ConfigComConnectionString(string valor) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = valor
            })
            .Build();

    [Fact]
    public void ConnectionString_Ausente_FalhaNomeandoChave()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => StartupConfiguration.RequireConnectionString(ConfigSemConnectionString(), "DefaultConnection"));

        Assert.Contains("ConnectionStrings:DefaultConnection", ex.Message);
    }

    [Theory]
    [InlineData("Host=localhost;Password=changeme")]
    [InlineData("Host=localhost;Password=CHANGE_ME")]
    [InlineData("Host=localhost;Password=your-password-here")]
    public void ConnectionString_PlaceholderProibido_FalhaSemRevelarValor(string valor)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => StartupConfiguration.RequireConnectionString(ConfigComConnectionString(valor), "DefaultConnection"));

        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(valor, ex.Message);
    }

    [Fact]
    public void ConnectionString_Valida_ERetornada()
    {
        const string valor = "Host=localhost;Port=5432;Database=meukanban;Username=app;Password=s3gura!";

        var resultado = StartupConfiguration.RequireConnectionString(
            ConfigComConnectionString(valor), "DefaultConnection");

        Assert.Equal(valor, resultado);
    }
}

/// <summary>
/// Prova de boot: sem a variavel de ambiente obrigatoria (ou com placeholder),
/// a aplicacao falha no startup com mensagem clara em vez de erro opaco em runtime.
/// </summary>
[Collection("WebHost")]
public class StartupFailureTests
{
    private const string ConnectionStringEnvVar = "ConnectionStrings__DefaultConnection";

    [Fact]
    public void Boot_SemConnectionStringObrigatoria_FalhaExplicitoNoStartup()
    {
        var original = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, null);

            using var factory = new WebApplicationFactory<Program>();
            var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains("ConnectionStrings:DefaultConnection", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, original);
        }
    }

    [Fact]
    public void Boot_ComPlaceholder_FalhaExplicitoSemRevelarValor()
    {
        var original = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar,
                "Host=localhost;Port=5432;Database=meukanban_test;Username=test;Password=changeme");

            using var factory = new WebApplicationFactory<Program>();
            var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains("ConnectionStrings:DefaultConnection", ex.Message);
            Assert.DoesNotContain("changeme", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, original);
        }
    }
}
