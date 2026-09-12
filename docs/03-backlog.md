# Backlog

O sequenciamento completo, os blocos de execucao, gates, paralelismo e as primeiras 10 sessoes estao em [docs/06-execution-plan.md](06-execution-plan.md). Este backlog conserva os IDs historicos T01-T25; tasks de evolucao e notificacoes posteriores possuem IDs T26-T34 no plano.

Backlog inicial do MeuKanBan. As tasks sao pequenas, ordenadas por dependencia tecnica e agrupadas em epicos. O tamanho e relativo: P cabe em poucas horas; M pode atravessar parte de um dia; G indica que a entrega ainda deve ser quebrada antes de implementacao.

## EPIC E0 - Fundacao e arquitetura

### T01 - Validar charter, linguagem ubiqua e fronteiras DDD

- **Objetivo**: consolidar agregados, invariantes, contextos e contratos entre Identity, Projects, Authorization, Boards/Cards e Audit.
- **Dependencias**: nenhuma.
- **Conceitos**: DDD, bounded context, agregado, ownership, modular monolith.
- **Tamanho**: P.
- **Criterios de aceite relacionados**: invariantes de `docs/domain/domain-model.md`; base dos ACs de Identity, Membership, Kanban e Audit.
- **Testes**: checklist de invariantes e revisao dos cenarios Given/When/Then existentes; nenhum codigo de aplicacao nesta task.

### T02 - Inicializar solucao API, frontend e containers

- **Objetivo**: criar os projetos vazios ASP.NET Core e Angular e adiciona-los ao Compose, mantendo PostgreSQL como dependencia compartilhada.
- **Dependencias**: T01.
- **Conceitos**: monorepo, Docker Compose, health check, configuracao por ambiente.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Roadmap Fase 0; ambiente local sobe de forma reproduzivel e API conecta ao PostgreSQL.
- **Testes**: `docker compose config`; health checks; smoke test de inicializacao da API e frontend.

### T03 - Definir contratos de erro, correlation id e configuracao

- **Objetivo**: padronizar erros HTTP, correlation id e origem de configuracoes sem codificar segredos no repositorio.
- **Dependencias**: T02.
- **Conceitos**: Problem Details, correlation id, environment configuration, secrets.
- **Tamanho**: P.
- **Criterios de aceite relacionados**: Security Baseline; conflitos devem poder retornar `409 Conflict` e auditoria deve carregar correlation id quando disponivel.
- **Testes**: API retorna formato de erro estavel; correlation id e preservado em log/resposta; ausencia de segredo em configuracao versionada.

### T04 - Criar modelo inicial de persistencia e migrations

- **Objetivo**: estabelecer EF Core, conexao PostgreSQL e migrations iniciais sem antecipar tabelas de features ainda nao modeladas.
- **Dependencias**: T01, T02.
- **Conceitos**: EF Core, migration, ownership de persistencia, constraint.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Roadmap Fase 0; API conecta e aplica migration de forma reproduzivel.
- **Testes**: migration em banco vazio; rollback ou estrategia documentada; teste de conexao usando PostgreSQL containerizado.

## EPIC E1 - Identity & Access

### T05 - Especificar cenarios BDD de cadastro e login

- **Objetivo**: transformar os ACs de `identity.md` em cenarios executaveis para cadastro valido, login valido e login invalido.
- **Dependencias**: T01, T03.
- **Conceitos**: BDD leve, credencial, enumeracao de identidade, auditoria.
- **Tamanho**: P.
- **Criterios de aceite relacionados**: Identity - cadastro valido, login valido e login invalido.
- **Testes**: cenarios falham inicialmente e verificam senha protegida, tokens emitidos e resposta indistinguivel para identificador desconhecido.

### T06 - Implementar cadastro aberto e credenciais protegidas

- **Objetivo**: criar identidade com identificador unico e senha protegida, auditando o cadastro.
- **Dependencias**: T04, T05.
- **Conceitos**: agregado User, hash de senha, unicidade, evento de dominio, transacao.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Identity - cadastro valido; identidade criada, senha nao e texto puro e cadastro auditado.
- **Testes**: unitario de politica de credencial; integracao de unicidade; API de cadastro; verificacao de ausencia de senha em logs/auditoria.

### T07 - Implementar login e emissao de tokens

- **Objetivo**: autenticar identidade ativa e emitir access JWT curto e refresh token, persistindo somente hash do refresh.
- **Dependencias**: T06.
- **Conceitos**: JWT, access token, refresh session, hash, expiracao configuravel.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Identity - login valido e login invalido.
- **Testes**: API para credencial valida/invalida; claims minimas; banco e logs nao contem refresh em texto puro.

### T08 - Implementar refresh rotation e deteccao de reutilizacao

- **Objetivo**: rotacionar refresh token na mesma familia e revogar a familia ao detectar reutilizacao.
- **Dependencias**: T07, T06.
- **Conceitos**: refresh family, revogacao, concorrencia de sessao, idempotencia de logout.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Identity - refresh rotativo e reutilizacao de refresh token.
- **Testes**: TDD seletivo para token usado uma vez, token reutilizado, familia revogada, expiracao e ausencia de novo token no incidente.

### T09 - Implementar logout e revogacao auditada

- **Objetivo**: revogar sessao/familia aplicavel sem reativacao por repeticao.
- **Dependencias**: T08.
- **Conceitos**: revogacao, sessao, auditoria de seguranca.
- **Tamanho**: P.
- **Criterios de aceite relacionados**: Identity - logout; refresh posterior deve ser rejeitado.
- **Testes**: API de logout repetido; refresh apos logout; evento de revogacao auditado.

## EPIC E2 - Projects, Membership e Authorization

### T10 - Definir matriz de permissoes e lacunas de ownership

- **Objetivo**: fechar, com o produto, permissoes por acao para `ProjectOwner`, `ProjectAdmin`, `Member`, `Viewer` e politica de `GlobalAdmin`.
- **Dependencias**: T01, T06.
- **Conceitos**: resource-based authorization, deny by default, ownership, matriz de acesso.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Membership - viewer tenta editar, administrador altera membership e usuario sem escopo nao infere existencia.
- **Testes**: tabela de decisao cobrindo permitido, negado, membership ausente, inativo e acesso cruzado. Esta task fecha lacunas, nao inventa permissoes sem validacao.

### T11 - Especificar cenarios BDD de isolamento e membership

- **Objetivo**: registrar cenarios executaveis para projeto privado, revogacao, viewer e administracao de membership.
- **Dependencias**: T10.
- **Conceitos**: BDD, escopo de recurso, membership ativo, anti-enumeracao.
- **Tamanho**: P.
- **Criterios de aceite relacionados**: Membership - isolamento entre projetos, membership revogado, viewer tenta editar e usuario sem escopo.
- **Testes**: cenarios inicialmente falhos para cada fronteira de autorizacao.

### T12 - Implementar projeto privado e membership

- **Objetivo**: criar projeto privado, membership ativo e alteracoes de papel/revogacao conforme a matriz aprovada.
- **Dependencias**: T04, T06, T10, T11.
- **Conceitos**: agregado Project, ProjectMembership, unico owner, estado ativo/inativo.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Membership - administrador altera membership e membership revogado.
- **Testes**: unitarios de invariantes; integracao de owner unico; API de inclusao, papel e revogacao; auditoria da mudanca.

### T13 - Implementar policies resource-based e isolamento por projeto

- **Objetivo**: autorizar cada operacao apos resolver recurso/projeto, negando acesso ausente, inativo ou cruzado.
- **Dependencias**: T12, T03.
- **Conceitos**: policy, resource authorization, projeto privado, deny by default.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Membership - isolamento entre projetos e usuario sem escopo tenta inferir existencia.
- **Testes**: API autorizada/negada; nenhum dado do projeto B retornado; respostas nao confirmam recurso privado; papel global sem acesso automatico.

## EPIC E3 - Boards & Cards MVP

### T14 - Especificar cenarios BDD do fluxo Kanban

- **Objetivo**: validar cenarios de criar board, criar card, mover no mesmo projeto, rejeitar outro projeto, arquivar e bloquear viewer.
- **Dependencias**: T13.
- **Conceitos**: BDD, agregado Board, agregado Card, invariantes de pertencimento.
- **Tamanho**: P.
- **Criterios de aceite relacionados**: Kanban - todos os cenarios de criacao, movimentacao, arquivamento e viewer.
- **Testes**: cenarios executaveis inicialmente falhos, incluindo board/card de projetos diferentes.

### T15 - Implementar boards e cards autorizados

- **Objetivo**: permitir CRUD minimo de boards/cards dentro do projeto autorizado, com card pertencendo a exatamente um board.
- **Dependencias**: T04, T13, T14.
- **Conceitos**: Board, Card, aggregate boundary, ordenacao deterministica, arquivamento.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Kanban - criar board, criar card, arquivar card e viewer em operacao mutavel.
- **Testes**: unitarios de pertencimento; integracao de constraints; API de CRUD autorizado/negado; cards arquivados fora da visao ativa.

### T16 - Implementar movimentacao entre columns do mesmo board

- **Objetivo**: mover card entre columns do mesmo board mantendo ordenacao deterministica e respeitando permissao. O card pertence a exatamente um board; troca de board fica fora do MVP e somente pode ser adicionada como operacao futura explicita com regra de negocio aprovada.
- **Dependencias**: T15.
- **Conceitos**: column, comando mutavel, invariantes de board, posicao, ordenacao.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Kanban - card pertence a exatamente um board; movimento normal ocorre entre columns do mesmo board. A divergencia textual sobre mover entre boards da spec deve ser alinhada antes da implementacao.
- **Testes**: unitario de pertencimento board/column; API de movimento valido; tentativa para outro board/projeto rejeitada sem alterar card.

## EPIC E4 - Concorrencia e auditoria

### T17 - Especificar cenarios BDD e testes de interleaving

- **Objetivo**: preparar cenarios para primeira atualizacao vencer, segunda conflitar e cliente recarregar estado atual.
- **Dependencias**: T15, T16, T14.
- **Conceitos**: optimistic concurrency, interleaving, atomicidade, `409 Conflict`.
- **Tamanho**: P.
- **Criterios de aceite relacionados**: Optimistic Concurrency - quatro cenarios de aceite.
- **Testes**: testes falhos com dois clientes lendo a mesma versao e submetendo comandos.

### T18 - Aplicar Version bigint a comandos mutaveis

- **Objetivo**: exigir versao esperada em editar, mover e arquivar; incrementar atomicamente apenas em sucesso.
- **Dependencias**: T04, T15, T16, T17.
- **Conceitos**: EF Core concurrency token, update condicional, atomicidade.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Optimistic Concurrency - primeira atualizacao vence e movimento tambem valida versao.
- **Testes**: integracao PostgreSQL com interleavings; sucesso incrementa uma vez; conflito nao altera nem incrementa.

### T19 - Expor 409 e recuperacao explicita no cliente

- **Objetivo**: retornar contrato de conflito com dados para recarga e permitir nova decisao sem retry cego.
- **Dependencias**: T18, T03.
- **Conceitos**: Problem Details, HTTP 409, client recovery, no blind retry.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Optimistic Concurrency - segunda atualizacao conflita e cliente recupera conflito.
- **Testes**: API valida status/payload; E2E recarrega versao; cliente nao reenvia automaticamente comando mutavel.

### T20 - Implementar auditoria append-only e minimizada

- **Objetivo**: registrar eventos de identidade, membership, projeto, board, card, autorizacao negada e conflito com escopo e correlation id.
- **Dependencias**: T06, T09, T12, T15, T18, T03.
- **Conceitos**: Audit Entry, append-only, minimizacao, consistencia transacional.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Audit - alteracao de card auditada e conflito auditado.
- **Testes**: unitarios de payload sem segredos; integracao de atomicidade; conflito nao descrito como sucesso; registros nao editaveis pela operacao normal.

### T21 - Implementar consulta autorizada de auditoria

- **Objetivo**: consultar historico somente no escopo permitido, em ordem deterministica.
- **Dependencias**: T13, T20.
- **Conceitos**: audit read policy, paginação futura, escopo privado.
- **Tamanho**: P.
- **Criterios de aceite relacionados**: Audit - consulta autorizada e consulta nao autorizada.
- **Testes**: API permite escopo autorizado, rejeita escopo nao autorizado e nao retorna eventos privados.

## EPIC E5 - Qualidade operacional

### T22 - Automatizar suites e validacoes do monorepo

- **Objetivo**: padronizar comandos para build, testes unitarios, integracao, API e E2E em containers quando necessario.
- **Dependencias**: T02, T04, T06, T15.
- **Conceitos**: test pyramid, reproducibilidade, CI futura.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Testing Strategy; cada AC implementado deve possuir teste adequado ao risco.
- **Testes**: executar cada suite isoladamente e em conjunto; falha de um teste retorna codigo nao zero.

### T23 - Adicionar logs estruturados, metricas e backup local documentado

- **Objetivo**: tornar falhas, conflitos, auditoria e saude do PostgreSQL observaveis no ambiente local.
- **Dependencias**: T20, T22.
- **Conceitos**: observabilidade basica, health check, backup/restore, sem dados sensiveis.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Roadmap Fase 5 e Security Baseline; falhas comuns detectaveis e reproduziveis.
- **Testes**: health check; verificacao de correlation id; backup e restore local; busca automatizada por token/senha em logs de teste.

## EPIC E6 - Evolucao posterior

### T24 - Implementar SignalR com grupos autorizados

- **Objetivo**: publicar mudancas confirmadas de board/card apenas a conexoes autorizadas, apos o Kanban estar estavel.
- **Dependencias**: T13, T18, T20, T23.
- **Conceitos**: SignalR, grupo privado, revalidacao, versao monotona.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Realtime - alteracao autorizada publicada, isolamento, reconexao apos revogacao e evento atrasado.
- **Testes**: integracao de grupos; reconexao sem membership; evento atrasado nao regride estado; conflito nao publica falso sucesso.

### T25 - Especificar e implementar chat isolado do Kanban

- **Objetivo**: adicionar conversas e mensagens privadas somente depois do realtime, sem alterar cards diretamente.
- **Dependencias**: T24, T20, T13.
- **Conceitos**: Conversation, Message, paginação, retencao, autorizacao.
- **Tamanho**: M.
- **Criterios de aceite relacionados**: Chat - membro envia mensagem, leitura sem acesso e mensagem fora do projeto.
- **Testes**: unitarios de ordenacao; API de autorizacao; integracao de associação projeto/board; evento realtime para participantes autorizados.

### T26-T34 - Plano detalhado de evolucao

As tasks de ordenacao, busca/paginacao, convites, notificacoes in-app e escala condicional estao detalhadas em [docs/06-execution-plan.md](06-execution-plan.md), com dependencias e gates. T24 continua bloqueada ate o gate F5; notificacoes dependem de eventos autorizados e chat depende de Realtime.

## Caminho critico e paralelismo

Caminho critico e gates atualizados: `T01 -> T02 -> T04 -> T06 -> T07 -> T08 -> T12 -> T13 -> T14 -> T15 -> T16 -> T17 -> T18 -> T19 -> T20 -> T21 -> T22 -> T23`.

Paralelismo seguro: depois de T01/T02, T03 e T04 podem ser trabalhadas em paralelo; depois de T06, T05 pode ser finalizada junto de T07 apenas se os cenarios BDD forem mantidos como contrato; depois de T13, T14 e a preparacao de testes de T17 podem avançar antes da implementacao de T15. T20 depende dos produtores de eventos e nao deve ser tratada como uma fundacao generica sem esses contratos.

SignalR, notificacoes e chat sao deliberadamente posteriores: T24 depende do modelo de autorizacao, concorrencia, auditoria e do gate F5; T29-T30 dependem do transporte/eventos autorizados; T25/T31-T33 dependem de Realtime e nao bloqueiam o MVP. A movimentacao normal do card fica entre columns do mesmo board; troca de board e futura e explicita, se houver regra de negocio.
