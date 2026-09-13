using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MeuKanBan.Api.Tests.TestHost;

/// <summary>
/// Provider de log de teste que captura mensagens e scopes (incluindo o
/// CorrelationId adicionado pelo middleware), permitindo verificar que o
/// correlation id esta disponivel nos logs da requisicao.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ConcurrentQueue<CapturedLog> Logs { get; } = new();

    public ILogger CreateLogger(string categoryName) =>
        new CapturingLogger(categoryName, Logs, () => _scopeProvider);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) =>
        _scopeProvider = scopeProvider;

    public void Dispose()
    {
    }

    public sealed record CapturedLog(string Category, LogLevel Level, string Message, List<string> Scopes, Exception? Exception);

    private sealed class CapturingLogger(
        string category,
        ConcurrentQueue<CapturedLog> sink,
        Func<IExternalScopeProvider> scopeProvider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            scopeProvider().Push(state!);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var scopes = new List<string>();
            scopeProvider().ForEachScope((scope, list) => list.Add(scope?.ToString() ?? string.Empty), scopes);
            // A excecao e capturada porque em runtime o console a renderiza
            // (ToString inclui a mensagem): testes de sanitizacao precisam
            // enxerga-la para provar que dados sensiveis nao vazam.
            sink.Enqueue(new CapturedLog(category, logLevel, formatter(state, exception), scopes, exception));
        }
    }
}
