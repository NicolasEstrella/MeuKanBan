# Project Charter

## Visao

MeuKanBan e uma aplicacao web para organizar trabalho colaborativo em projetos privados. O primeiro valor entregue e um Kanban seguro, auditavel e resistente a edicoes concorrentes.

## Objetivos

- Permitir cadastro aberto e autenticacao segura.
- Isolar projetos privados por membership e permissao resource-based.
- Organizar trabalho em multiplos boards por projeto.
- Garantir que cada card pertença a exatamente um board.
- Evitar sobrescrita silenciosa com concorrencia otimista e `409 Conflict`.
- Registrar mudancas relevantes desde o inicio.
- Manter uma base modular que possa evoluir para realtime e chat sem microsservicos prematuros.

## Escopo inicial

Dentro: Identity & Access, Projects & Membership, Authorization, Boards & Cards, Audit, containers locais, testes e observabilidade basica.

Posterior: SignalR/realtime, notificacoes evoluidas e Chat.

Fora do baseline: Java, Spring, RabbitMQ, Kafka, microsservicos, Kubernetes, SSO, MFA, login social, busca distribuida, event sourcing e workflow configuravel.

## Stack e topologia

Angular + ASP.NET Core + EF Core + PostgreSQL em monorepo. A API e um modular monolith com contexto e ownership de dados explicitos. O ambiente local e containerizado por Docker Compose.

## Papeis

`GlobalAdmin`, `ProjectOwner`, `ProjectAdmin`, `Member` e `Viewer`. Papel nao equivale sozinho a autorizacao: a decisao considera identidade, membership, projeto, recurso, acao e estado.

## Metodologia

DDD explicito para fronteiras, agregados e invariantes. BDD leve para comportamentos criticos. TDD seletivo para autorizacao, concorrencia, rotacao de sessoes e invariantes de agregados.

## Restricoes e lacunas

A branch principal e `main`. Permanecem abertas a duracao exata dos tokens, armazenamento do refresh token no cliente, permissao efetiva de `GlobalAdmin`, ownership, ordenacao de cards, retencao de auditoria/chat e politica de deploy.

## Definition of Done do charter

A estrutura inicial existe, as decisoes confirmadas estao registradas e cada proxima entrega pode ser rastreada ao backlog e aos criterios das specs em `docs/specs/`.
