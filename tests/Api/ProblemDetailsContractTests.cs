using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeuKanBan.Api.Tests;

/// <summary>
/// Contrato de Problem Details (RFC 7807) da docs/specs/api-contracts.md:
/// cada status relevante responde application/problem+json com detail seguro,
/// sem stack trace, sem segredo e sem enumeracao de identidade/recurso.
/// </summary>
[Collection("WebHost")]
public class ProblemDetailsContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionStringEnvVar = "ConnectionStrings__DefaultConnection";
    private const string TestHooksEnvVar = "Diagnostics__ContractsTestHooks__Enabled";

    private readonly HttpClient _client;

    public ProblemDetailsContractTests(WebApplicationFactory<Program> factory)
    {
        // Variaveis de ambiente: o Program valida configuracao e test hooks
        // antes do build do host; a fonte in-memory do factory nao e visivel
        // nesse ponto (mesmo padrao do HealthEndpointsTests).
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvVar,
            "Host=localhost;Port=5432;Database=meukanban_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable(TestHooksEnvVar, "true");

        _client = factory.CreateClient();
    }

    private static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(body));
        return JsonDocument.Parse(body);
    }

    [Fact]
    public async Task ValidacaoEstrutural_PayloadAusente_Retorna400ComCamposInvalidos()
    {
        var response = await _client.PostAsJsonAsync("/diagnostics/contracts/echo", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await ReadProblemAsync(response);
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("Name", out _));
        Assert.True(problem.RootElement.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task ValidacaoEstrutural_JsonMalformado_Retorna400ProblemDetails()
    {
        var response = await _client.PostAsync("/diagnostics/contracts/echo",
            new StringContent("{ nao-e-json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await ReadProblemAsync(response);
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task AutenticacaoAusente_EndpointProtegido_Retorna401Generico()
    {
        var response = await _client.GetAsync("/diagnostics/contracts/authorized");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(401, problem.RootElement.GetProperty("status").GetInt32());
        // Corpo generico: nao confirma identidade nem como autenticar.
        Assert.DoesNotContain("X-Test-Identity", body);
        Assert.DoesNotContain("probe", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AutorizacaoNegada_IdentidadeSemPermissao_Retorna403SemDadosDoRecurso()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/diagnostics/contracts/forbidden");
        request.Headers.Add("X-Test-Identity", "usuario-sem-permissao");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(403, problem.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("usuario-sem-permissao", body);
    }

    [Fact]
    public async Task RotaInexistente_Retorna404ProblemDetails()
    {
        var response = await _client.GetAsync("/rota-que-nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var problem = await ReadProblemAsync(response);
        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task RecursoInexistente_ExcecaoDeNegocio_Retorna404ProblemDetails()
    {
        var response = await _client.GetAsync("/diagnostics/contracts/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var problem = await ReadProblemAsync(response);
        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ConflitoDeEstado_Retorna409ComCorrelationId()
    {
        var response = await _client.GetAsync("/diagnostics/contracts/conflict");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var problem = await ReadProblemAsync(response);
        Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task RegraDeNegocio_Retorna422ComCodigoEstavel()
    {
        var response = await _client.GetAsync("/diagnostics/contracts/business-rule");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        using var problem = await ReadProblemAsync(response);
        Assert.Equal(422, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("diagnostics.sample-rule", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task FalhaInesperada_Retorna500Generico_SemStackTraceOuSegredo()
    {
        var response = await _client.GetAsync("/diagnostics/contracts/unexpected");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(500, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Ocorreu um erro inesperado. Tente novamente mais tarde.",
            problem.RootElement.GetProperty("detail").GetString());
        // Nada de stack trace, tipo de excecao ou dado interno/sensivel.
        Assert.DoesNotContain("SuperSecret123", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("at MeuKanBan", body);
    }
}
