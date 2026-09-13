using MeuKanBan.Api.Configuration;
using MeuKanBan.Api.Correlation;
using MeuKanBan.Api.Diagnostics;
using MeuKanBan.Api.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

// Configuracao obrigatoria validada no boot: ausencia ou placeholder falha de
// forma explicita, nomeando a chave e nunca revelando o valor
// (docs/specs/api-contracts.md).
var connectionString = StartupConfiguration.RequireConnectionString(builder.Configuration, "DefaultConnection");

// Senha vem de arquivo (Docker secret em /run/secrets) quando Postgres:PasswordFile
// esta configurado: o segredo nunca trafega em variavel de ambiente.
connectionString = PostgresConnectionStringFactory.ApplyPasswordFile(
    connectionString, builder.Configuration["Postgres:PasswordFile"]);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// Test hooks de contrato (endpoints /diagnostics/contracts + esquema de
// autenticacao "Diagnostics") existem apenas quando habilitados explicitamente
// nos testes de API. Padrao: desabilitado (inclusive producao).
var testHooksEnabled = builder.Configuration.GetValue("Diagnostics:ContractsTestHooks:Enabled", false);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddControllers(options =>
    {
        if (!testHooksEnabled)
        {
            options.Conventions.Add(new RemoveDiagnosticsContractsControllerConvention());
        }
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // 400 de validacao estrutural (payload ausente, formato invalido ou
        // campo estrutural incorreto) sai como Problem Details com os campos
        // invalidos, sem segredo nem stack trace.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Valor invalido." : e.ErrorMessage)
                        .ToArray());

            var problem = ProblemDetailsWriter.Create(context.HttpContext,
                StatusCodes.Status400BadRequest,
                "Requisicao invalida",
                "A requisicao contem campos ausentes ou em formato invalido.");
            problem.Extensions["errors"] = errors;

            return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        };
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Requerido pelo middleware de excecao; a escrita efetiva e feita pelo
// GlobalExceptionHandler/ProblemDetailsWriter.
builder.Services.AddProblemDetails();

var authBuilder = testHooksEnabled
    ? builder.Services.AddAuthentication(DiagnosticsAuthHandler.SchemeName)
    : builder.Services.AddAuthentication();

if (testHooksEnabled)
{
    authBuilder.AddScheme<AuthenticationSchemeOptions, DiagnosticsAuthHandler>(
        DiagnosticsAuthHandler.SchemeName, null);
}

builder.Services.AddAuthorization(options =>
{
    if (testHooksEnabled)
    {
        options.AddPolicy(DiagnosticsContractsController.ProbePolicy,
            policy => policy.RequireClaim("probe", "allowed"));
    }
});

builder.Logging.AddConsoleFormatter<CorrelationConsoleFormatter, ConsoleFormatterOptions>();
builder.Logging.AddConsole(options => options.FormatterName = CorrelationConsoleFormatter.FormatterName);

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

// Correlation id primeiro: toda resposta (sucesso ou erro) carrega o header e
// todo log da requisicao carrega o scope.
app.UseCorrelationId();

// Excecao nao tratada vira Problem Details 500 generico.
app.UseExceptionHandler();

// Respostas de status sem corpo (401/403 de challenge/forbid, 404 de rota
// inexistente) ganham Problem Details generico, sem enumerar identidade ou recurso.
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (response.HasStarted || response.ContentLength > 0 || !string.IsNullOrEmpty(response.ContentType))
    {
        return;
    }

    var (title, detail) = response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => ("Nao autenticado", "Autenticacao ausente ou invalida."),
        StatusCodes.Status403Forbidden => ("Acesso negado", "A identidade autenticada nao possui permissao para esta operacao."),
        StatusCodes.Status404NotFound => ("Recurso nao encontrado", "O recurso solicitado nao foi encontrado."),
        _ => ("Erro", "A requisicao nao pode ser atendida.")
    };

    await ProblemDetailsWriter.WriteAsync(statusContext.HttpContext,
        ProblemDetailsWriter.Create(statusContext.HttpContext, response.StatusCode, title, detail));
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Readiness: processo + dependencia essencial (PostgreSQL).
// Banco indisponivel => 503 => container unhealthy, sem falso positivo.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Liveness: apenas o processo, sem dependencias.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.Run();

// Permite WebApplicationFactory<Program> nos testes de API.
public partial class Program;
