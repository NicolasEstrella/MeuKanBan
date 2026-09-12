using MeuKanBan.Api.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configuracao obrigatoria: ausencia falha de forma explicita no startup,
// sem revelar o valor (T03 detalha o contrato completo de erros/configuracao).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Configuracao obrigatoria ausente: ConnectionStrings:DefaultConnection. " +
        "Forneca via variavel de ambiente ConnectionStrings__DefaultConnection ou user-secrets no desenvolvimento.");

// Senha vem de arquivo (Docker secret em /run/secrets) quando Postgres:PasswordFile
// esta configurado: o segredo nunca trafega em variavel de ambiente.
connectionString = PostgresConnectionStringFactory.ApplyPasswordFile(
    connectionString, builder.Configuration["Postgres:PasswordFile"]);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

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

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

app.UseCors();

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
