using System.Text.Json;
using MeuKanBan.Api.Correlation;
using Microsoft.AspNetCore.Mvc;

namespace MeuKanBan.Api.Errors;

/// <summary>
/// Serializacao unica de Problem Details (RFC 7807): todo erro da API sai como
/// application/problem+json com type/title/status/detail seguro, instance e a
/// extensao estavel correlationId (docs/specs/api-contracts.md).
/// </summary>
public static class ProblemDetailsWriter
{
    public const string ProblemJsonContentType = "application/problem+json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ProblemDetails Create(HttpContext context, int status, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions[CorrelationId.ExtensionName] = CorrelationIdMiddleware.Get(context);
        return problem;
    }

    public static async Task WriteAsync(HttpContext context, ProblemDetails problem, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = ProblemJsonContentType;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions), cancellationToken);
    }
}
