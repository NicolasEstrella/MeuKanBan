namespace MeuKanBan.Api.Correlation;

/// <summary>
/// Resolve o correlation id no inicio do pipeline: propaga o id valido recebido,
/// gera um novo quando ausente ou invalido, devolve em toda resposta (sucesso ou
/// erro) e o disponibiliza no scope de log da requisicao.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationId.HeaderName].ToString();

        string correlationId;
        if (CorrelationId.IsValid(incoming))
        {
            correlationId = incoming;
        }
        else
        {
            correlationId = CorrelationId.NewId();
            if (!string.IsNullOrEmpty(incoming))
            {
                // O valor recebido NAO e registrado: pode carregar payload de injecao de log.
                logger.LogWarning("Correlation id recebido com formato invalido; um novo id foi gerado.");
            }
        }

        context.Items[CorrelationId.ItemKey] = correlationId;

        // OnStarting: o middleware de excecao limpa os headers ao tratar erro;
        // o correlation id e regravado no envio e chega em TODA resposta.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationId.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope("CorrelationId={CorrelationId}", correlationId))
        {
            await next(context);
        }
    }

    public static string Get(HttpContext context) =>
        context.Items[CorrelationId.ItemKey] as string ?? context.TraceIdentifier;
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
