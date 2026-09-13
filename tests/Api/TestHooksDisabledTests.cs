using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeuKanBan.Api.Tests;

/// <summary>
/// Garantia de producao: com Diagnostics:ContractsTestHooks:Enabled ausente
/// (padrao), os endpoints de diagnostico de contrato nao existem no pipeline.
/// </summary>
[Collection("WebHost")]
public class TestHooksDisabledTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ConnectionStringEnvVar = "ConnectionStrings__DefaultConnection";
    private const string TestHooksEnvVar = "Diagnostics__ContractsTestHooks__Enabled";

    private readonly HttpClient _client;

    public TestHooksDisabledTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvVar,
            "Host=localhost;Port=5432;Database=meukanban_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable(TestHooksEnvVar, null);

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TestHooks_DesabilitadosPorPadrao_EndpointsDeDiagnosticoRetornam404()
    {
        var response = await _client.GetAsync("/diagnostics/contracts/unexpected");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
