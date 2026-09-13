using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace MeuKanBan.Api.Correlation;

/// <summary>
/// Formatter de console padrao da API. O console logger nativo nao renderiza
/// scopes sem IncludeScopes=true, e liga-lo globalmente despejaria scopes do
/// framework (dados de request do Kestrel/HttpContext) nos logs. Este formatter
/// expoe APENAS o scope "CorrelationId=" gravado pelo CorrelationIdMiddleware,
/// tornando o correlation id observavel em runtime sem dumping irrestrito de
/// scopes (docs/specs/api-contracts.md, TC-002).
/// </summary>
public sealed class CorrelationConsoleFormatter() : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "correlation-safe";

    private const string CorrelationScopePrefix = "CorrelationId=";

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (string.IsNullOrEmpty(message) && logEntry.Exception is null)
        {
            return;
        }

        textWriter.Write(logEntry.LogLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "none",
        });
        textWriter.Write(": ");
        textWriter.Write(logEntry.Category);
        textWriter.Write('[');
        textWriter.Write(logEntry.EventId.Id);
        textWriter.Write(']');

        var correlationScope = FindCorrelationScope(scopeProvider);
        if (correlationScope is not null)
        {
            textWriter.Write(" [");
            textWriter.Write(correlationScope);
            textWriter.Write(']');
        }
        textWriter.WriteLine();

        if (!string.IsNullOrEmpty(message))
        {
            textWriter.Write("      ");
            textWriter.WriteLine(message);
        }

        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine(logEntry.Exception.ToString());
        }
    }

    private static string? FindCorrelationScope(IExternalScopeProvider? scopeProvider)
    {
        if (scopeProvider is null)
        {
            return null;
        }

        string? found = null;
        scopeProvider.ForEachScope((scope, _) =>
        {
            if (found is not null)
            {
                return;
            }

            var text = scope?.ToString();
            if (!string.IsNullOrEmpty(text) &&
                text.StartsWith(CorrelationScopePrefix, StringComparison.Ordinal))
            {
                found = text;
            }
        }, (object?)null);
        return found;
    }
}
