# Architecture Overview

## Decisao resumida

O sistema usa um modular monolith ASP.NET Core com Angular e PostgreSQL. Modulos possuem modelos, casos de uso e ownership de persistencia claros, mas compartilham processo e banco no baseline.

## Contextos e responsabilidades

| Contexto | Responsabilidade |
|---|---|
| Identity & Access | cadastro, credenciais, sessoes, tokens e papeis globais |
| Projects & Membership | projetos privados e associacao de usuarios |
| Authorization | policies resource-based e acesso por projeto/recurso |
| Boards & Cards | boards, cards, posicao, movimentacao e arquivamento |
| Audit | registros append-only de seguranca e dominio |
| Notifications | contratos futuros de entrega sem broker obrigatorio |
| Realtime | SignalR posterior para atualizacoes autorizadas |
| Chat | conversas posteriores, isoladas do estado de cards |

## Fluxo de dependencias

```text
Angular -> API ASP.NET Core -> Application -> Domain
                                      -> Infrastructure -> EF Core -> PostgreSQL
```

Controllers traduzem HTTP para casos de uso. A camada de dominio decide invariantes. A infraestrutura implementa persistencia e adaptadores. Modulos nao acessam diretamente entidades ou repositorios internos de outros modulos.

## Dados e concorrencia

PostgreSQL e o armazenamento principal. O banco pode iniciar com schema operacional compartilhado, mantendo ownership logico por modulo e migrations rastreaveis. Entidades editaveis usam `Version bigint`; comandos mutaveis exigem a versao lida e conflitos retornam `409 Conflict` sem alteracao parcial.

## Seguranca transversal

A API e stateless. Access JWT e curto; refresh token e rotativo, revogavel e persistido somente como hash. Projetos sao privados por padrao. Auditoria e registrada para eventos de identidade, autorizacao, membership, projeto, board, card e conflito.

## Containers locais

O Compose ja reserva PostgreSQL. API e Angular serao adicionados quando seus projetos forem criados. Containerizacao nao significa microsservicos: os componentes permanecem em um monorepo e a API permanece um processo modular.

## Evolucao

Depois do Kanban: SignalR com revalidacao de autorizacao, depois chat. Cache, filas, replicas ou extracao de modulo somente apos metricas e fronteiras estaveis. Nao ha requisito inicial para RabbitMQ, Kafka, Elasticsearch ou Kubernetes.
