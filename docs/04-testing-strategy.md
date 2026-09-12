# Testing Strategy

## Objetivo

Detectar cedo violações de invariantes, vazamentos entre projetos, perda de dados concorrente e regressao nos contratos HTTP sem exigir uma arquitetura distribuida.

## Piramide

- **Unit**: agregados, invariantes, matriz de papeis, rotacao/revogacao, minimizacao de auditoria e ordenacao.
- **Integration**: EF Core com PostgreSQL, migrations, atomicidade, indice/constraint e concorrencia real.
- **API**: contratos HTTP, status, payloads, autenticacao, autorizacao e `409 Conflict`.
- **E2E**: jornadas criticas no Angular, desde cadastro/login ate board/card e recuperacao de conflito.

## BDD leve

Cenarios Given/When/Then em `docs/specs/` sao a referencia comportamental. Toda task de uma fatia deve apontar para seus cenarios e converter os casos criticos em testes executaveis.

## Prioridade obrigatoria

1. Cadastro, login, refresh rotation, reutilizacao e logout.
2. Isolamento entre projetos, membership revogado e viewer sem edicao.
3. Boards/cards, pertencimento e movimentacao entre boards do mesmo projeto.
4. Interleavings concorrentes, atomicidade e ausencia de update parcial.
5. Auditoria append-only, escopo de consulta e ausencia de segredos.
6. Realtime e chat somente nas fases posteriores.

## Dados e ambiente

Testes de integracao devem usar PostgreSQL containerizado e dados isolados por teste ou suite. Segredos reais nunca entram em fixtures, logs, snapshots ou repositorio.

## Definition of Done de testes

Cada criterio de aceite implementado tem ao menos um teste adequado ao risco; testes de autorizacao cobrem permitido e negado; conflitos provam que o estado nao sofreu alteracao parcial; testes falhos sao reproduziveis localmente.
