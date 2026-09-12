# Roadmap

O detalhamento executavel, com fases F0-F9, blocos B01-B25, tasks, dependencias, gates e primeiras 10 sessoes, esta em [docs/06-execution-plan.md](06-execution-plan.md). Este documento permanece como visao resumida; a ordem operacional e a Definition of Done devem ser consultadas no plano.

Roadmap por fatias verticais. A fundacao e totalmente containerizada desde o inicio, com a topologia de `postgres`, `api` e `frontend` declarada no Docker Compose. Cada fase deve terminar com testes, documentacao minima e demonstracao local. A ordem respeita DDD e BDD leve.

| Fase | Entrega | Saida minima |
|---|---|---|
| 0 | Fundacao | monorepo, containers, configuracao, health checks e conexao API/PostgreSQL |
| 1 | Identity & Access | cadastro, login, JWT curto, refresh rotativo, logout e auditoria |
| 2 | Projects & Authorization | projetos privados, memberships, papeis e policies resource-based |
| 3 | Boards & Cards MVP | boards, columns, cards, movimentacao entre columns do mesmo board, arquivamento e filtros basicos |
| 4 | Concorrencia e auditoria | `Version bigint`, `409 Conflict`, atomicidade e historico autorizado |
| 5 | Qualidade operacional | suites unitarias, integracao, API/E2E, logs, metricas, backup e docs |
| 6 | Evolucao Kanban | ordenacao robusta, busca/paginacao, convites e UX recorrente |
| 7 | Realtime e notificacoes | SignalR autorizado e notificacoes in-app depois do nucleo Kanban |
| 8 | Chat | conversas, mensagens, autorizacao, retencao e eventos realtime depois de Realtime |
| 9 | Escala seletiva | somente cache, eventos/notificacoes ou replicas justificados por metricas |

## Criterios de passagem

Nao avancar para a proxima fase se a anterior nao possuir testes para os criterios de aceite relevantes, auditoria quando aplicavel e uma forma reproduzivel de execucao local.

## Decisoes adiadas

Duracao dos tokens, armazenamento no navegador, poderes de `GlobalAdmin`, ownership, ordenacao de cards, retencao LGPD, provedor de e-mail, hospedagem e necessidades de escala permanecem propostas ate haver evidencia.
