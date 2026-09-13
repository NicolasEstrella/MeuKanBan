using System.Net;
using MeuKanBan.Api.Correlation;
using MeuKanBan.Api.Tests.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace MeuKanBan.Api.Tests;

/// <summary>
/// Regressao FB-T03-01 (workflow WL-T03T04-2026-09-13, iteracao 1):
/// - TC-002: o log de erro do 500 deve carregar o correlation id da requisicao
///   (scope do CorrelationIdMiddleware).
/// - OBS-1: o log NAO pode conter a mensagem bruta da excecao nem o segredo
///   simulado; apenas tipo da excecao + stack trace (frames sem mensagem).
/// A resposta HTTP (500 generico) ja e coberta por ProblemDetailsContractTests.
/// </summary>
[Collection("WebHost")]
public class UnexpectedErrorLogTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionStringEnvVar = "ConnectionStrings__DefaultConnection";
    private const string TestHooksEnvVar = "Diagnostics__ContractsTestHooks__Enabled";

    // Mesmo segredo simulado pelo hook /diagnostics/contracts/unexpected.
    private const string SegredoSimulado = "SuperSecret123";
    private const string MensagemBruta = "Falha interna com dado sensivel simulado";

    private readonly WebApplicationFactory<Program> _factory;

    public UnexpectedErrorLogTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvVar,
            "Host=localhost;Port=5432;Database=meukanban_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable(TestHooksEnvVar, "true");

        _factory = factory;
    }

    [Fact]
    public async Task FalhaInesperada_LogContemCorrelationId_ESemMensagemOuSegredo()
    {
        var captura = new CapturingLoggerProvider();
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(captura)));
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/diagnostics/contracts/unexpected");
        request.Headers.Add(CorrelationId.HeaderName, "req-500-sanitizado");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var logErro = Assert.Single(captura.Logs,
            log => log.Level == LogLevel.Error
                && log.Category.Contains("GlobalExceptionHandler", StringComparison.Ordinal));

        // TC-002: correlation id presente no scope do log de erro.
        Assert.Contains(logErro.Scopes,
            scope => scope.Contains("req-500-sanitizado", StringComparison.Ordinal));

        // OBS-1: nem a mensagem formatada nem a excecao capturada podem
        // carregar a mensagem bruta ou o segredo simulado.
        Assert.DoesNotContain(SegredoSimulado, logErro.Message);
        Assert.DoesNotContain(MensagemBruta, logErro.Message);
        Assert.DoesNotContain(SegredoSimulado, logErro.Exception?.ToString() ?? string.Empty);
        Assert.DoesNotContain(MensagemBruta, logErro.Exception?.ToString() ?? string.Empty);

        // O log mantem valor diagnostico: tipo da excecao e stack trace.
        Assert.Contains("InvalidOperationException", logErro.Message);
        Assert.Contains("DiagnosticsContractsController", logErro.Message);
    }
}
