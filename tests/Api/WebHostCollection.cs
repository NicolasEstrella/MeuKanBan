namespace MeuKanBan.Api.Tests;

/// <summary>
/// Testes que sobem o host via WebApplicationFactory ou mutam variaveis de
/// ambiente do processo (configuracao obrigatoria, test hooks) executam em
/// serie para evitar condicoes de corrida na configuracao process-wide.
/// </summary>
[CollectionDefinition("WebHost", DisableParallelization = true)]
public sealed class WebHostCollection;
