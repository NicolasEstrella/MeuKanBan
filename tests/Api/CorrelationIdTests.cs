using System.Net;
using System.Text.Json;
using MeuKanBan.Api.Correlation;
using MeuKanBan.Api.Tests.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace MeuKanBan.Api.Tests;

/// <summary>
/// Contrato de correlation id (docs/specs/api-contracts.md): id valido do
/// cliente e propagado; na ausencia a API gera; formato invalido nao e
/// propagado (anti injecao em log); o id esta presente em toda resposta e nos
/// logs da requisicao.
/// </summary>
[Collection("WebHost")]
public class CorrelationIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionStringEnvVar = "ConnectionStrings__DefaultConnection";
    private const string TestHooksEnvVar = "Diagnostics__ContractsTestHooks__Enabled";

    private readonly WebApplicationFactory<Program> _factory;

    public CorrelationIdTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvVar,
            "Host=localhost;Port=5432;Database=meukanban_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable(TestHooksEnvVar, "true");

        _factory = factory;
    }

    [Fact]
    public async Task IdValidoFornecido_EPropagadoNoHeaderENoProblemDetails()
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/diagnostics/contracts/conflict");
        request.Headers.Add(CorrelationId.HeaderName, "req-cliente-123");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("req-cliente-123",
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("req-cliente-123", problem.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task IdAusente_EGeradoEPresenteNaRespostaDeSucessoEDeErro()
    {
        using var client = _factory.CreateClient();

        var sucesso = await client.GetAsync("/health/live");
        var idSucesso = sucesso.Headers.GetValues(CorrelationId.HeaderName).Single();
        Assert.False(string.IsNullOrWhiteSpace(idSucesso));
        Assert.True(CorrelationId.IsValid(idSucesso));

        var erro = await client.GetAsync("/rota-que-nao-existe");
        var idErro = erro.Headers.GetValues(CorrelationId.HeaderName).Single();
        Assert.True(CorrelationId.IsValid(idErro));
        using var problem = JsonDocument.Parse(await erro.Content.ReadAsStringAsync());
        Assert.Equal(idErro, problem.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task IdComFormatoInvalido_NaoEPropagado_NovoIdSeguroEGerado()
    {
        // Formato com espacos/chaves: aceito pelo HTTP, mas proibido pelo
        // contrato para impedir injecao em logs (quebra de linha nao passa nem
        // pelo HttpClient; o contrato bloqueia qualquer caractere fora da lista).
        const string idInvalido = "abc {evil} | injection";

        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationId.HeaderName, idInvalido);

        var response = await client.SendAsync(request);

        var idResposta = response.Headers.GetValues(CorrelationId.HeaderName).Single();
        Assert.NotEqual(idInvalido, idResposta);
        Assert.True(CorrelationId.IsValid(idResposta));
    }

    [Fact]
    public async Task IdValido_ApareceNosLogsCorrelatosDaRequisicao()
    {
        var captura = new CapturingLoggerProvider();
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(captura)));
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/diagnostics/contracts/business-rule");
        request.Headers.Add(CorrelationId.HeaderName, "req-logs-456");
        var response = await client.SendAsync(request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Contains(captura.Logs,
            log => log.Scopes.Any(scope => scope.Contains("req-logs-456", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task IdInvalido_ValorBrutoNaoApareceNosLogs()
    {
        var captura = new CapturingLoggerProvider();
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(captura)));
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationId.HeaderName, "inj3t {payload} bruto");
        await client.SendAsync(request);

        Assert.DoesNotContain(captura.Logs,
            log => log.Message.Contains("inj3t", StringComparison.Ordinal)
                || log.Scopes.Any(scope => scope.Contains("inj3t", StringComparison.Ordinal)));
    }
}
