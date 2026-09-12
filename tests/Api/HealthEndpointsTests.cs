using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeuKanBan.Api.Tests;

public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionStringEnvVar = "ConnectionStrings__DefaultConnection";

    private readonly HttpClient _client;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        // Connection string fake via variavel de ambiente: o Program valida a
        // configuracao obrigatoria antes do build do host, entao a fonte
        // in-memory do WebApplicationFactory nao e suficiente. O smoke test
        // cobre apenas liveness, que nao depende de PostgreSQL. Readiness com
        // banco real fica para os testes de integracao (T04).
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvVar,
            "Host=localhost;Port=5432;Database=meukanban_test;Username=test;Password=test");

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Liveness_RetornaHealthy_SemDependencias()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
