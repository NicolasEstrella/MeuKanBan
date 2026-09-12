# Domain Model

## Objetivo

Este documento define a linguagem ubíqua e as fronteiras iniciais do domínio do MeuKanBan. O sistema permite colaboração segura em projetos privados, organiza trabalho em boards/cards e evita perda de contexto, acesso indevido e sobrescrita silenciosa de alterações.

## Bounded contexts

| Contexto | Responsabilidade | Dono das regras principais |
|---|---|---|
| Identity & Access | Identidades, credenciais, sessões, tokens e papéis globais | identidade e sessão |
| Projects & Membership | Projetos privados e associação de usuários | projeto e membership |
| Boards & Cards | Organização e edição do trabalho | board e card |
| Authorization | Decisão resource-based usando identidade, projeto e recurso | políticas, sem usurpar invariantes |
| Audit | Registro append-only de ações relevantes | eventos de auditoria |
| Notifications | Entrega de avisos derivados de eventos | preferências e entrega |
| Realtime | Transporte de atualizações autorizadas | grupos e conexão |
| Chat | Conversas e mensagens privadas | conversa e mensagem |

O baseline é um monólito modular com fronteiras explícitas e um PostgreSQL compartilhado. O desenho não exige microsserviços nem separação física de banco.

## Agregados e entidades

### User / Identity

Representa a identidade autenticável. Possui credenciais protegidas, estado de ativação e sessões de refresh. Não representa membership em um projeto.

### Refresh Session

Representa uma sessão e sua família de rotação. Guarda somente material não reutilizável em texto puro, estado de revogação, expiração e metadados necessários para detectar reutilização.

### Project

Raiz do escopo privado. Possui nome, estado e referência ao owner conforme as regras de ownership. Um projeto contém vários boards e memberships.

### Project Membership

Vínculo de um usuário com um projeto, estado ativo/inativo, papel e metadados de alteração. Papéis de projeto são `ProjectOwner`, `ProjectAdmin`, `Member` e `Viewer`.

### Board

Raiz de organização dentro de um projeto. Cada board pertence a exatamente um projeto. Um projeto pode ter múltiplos boards.

### Card

Unidade de trabalho pertencente a exatamente um board. Pode ser editado, movido dentro do projeto e arquivado. Possui `Version` monotônica usada para concorrência otimista.

### Conversation / Message

Contexto posterior para conversas vinculadas a um projeto e opcionalmente a um board. Mensagens têm autor, timestamp e identificador estável; chat não altera cards diretamente.

### Audit Entry

Registro append-only de evento relevante, com ator, ação, recurso, projeto, timestamp UTC, resultado e correlation id quando disponível. Não é um recurso editável comum.

### Notification

Representa um aviso derivado de uma alteração ou evento. Não autoriza acesso nem altera o recurso de origem.

## Value objects e conceitos

- `ProjectId`, `BoardId`, `CardId`, `UserId` e `ConversationId`: identificadores estáveis e tipados no domínio.
- `Role`: papel global ou papel de projeto, nunca uma autorização completa por si só.
- `MembershipStatus`: ativo ou inativo conforme regras do projeto.
- `Version`: valor monotônico requerido em comandos mutáveis.
- `ResourceScope`: projeto e recurso usados na decisão de autorização.
- `CorrelationId`: ligação operacional entre requisição, mudança e auditoria.

## Invariantes centrais

1. Projeto privado só é visível a usuários com escopo autorizado.
2. Membership inativo não autoriza novas operações.
3. Um board pertence a exatamente um projeto.
4. Um card pertence a exatamente um board.
5. Um card só pode ser movido entre boards do mesmo projeto.
6. Operação mutável exige papel compatível e versão esperada.
7. Versão divergente produz `409 Conflict` e não altera o estado.
8. Auditoria relevante não contém segredos e não é editada pela operação normal.
9. Realtime e notificações nunca ampliam autorização.
10. Chat é posterior ao núcleo Kanban e não mistura estado de conversa com estado de card.

## Domain events conceituais

- `UserRegistered`
- `SessionIssued`, `SessionRefreshed`, `SessionRevoked`
- `ProjectCreated`, `ProjectUpdated`
- `MembershipGranted`, `MembershipRoleChanged`, `MembershipRevoked`
- `BoardCreated`, `BoardUpdated`, `BoardArchived`
- `CardCreated`, `CardUpdated`, `CardMoved`, `CardArchived`
- `ConcurrencyConflictDetected`
- `ConversationCreated`, `MessagePosted`
- `NotificationCreated`

Os eventos representam fatos confirmados. Um conflito de concorrência é um resultado auditável, não uma alteração de card.

## Regras de autorização

A decisão considera, no mínimo, identidade, ação, recurso, projeto, membership ativo, papel global, papel no projeto e estado do recurso. O acesso é negado por padrão. `GlobalAdmin` possui capacidades globais explícitas, mas não acesso automático ao conteúdo privado.

## Ordem de entrega

1. Identity & Access, Projects & Membership, Authorization, Audit e Boards & Cards.
2. Concorrência otimista integrada ao núcleo Kanban.
3. Realtime com SignalR após o Kanban.
4. Chat e notificações evoluídas após as bases anteriores.

## Metodologia e stack

A modelagem segue DDD explícito. BDD leve/Gherkin registra comportamentos críticos e serve de contrato com QA. TDD é aplicado seletivamente a autorização, concorrência, rotação de sessão e invariantes dos agregados.

Stack confirmada: Angular, ASP.NET Core, EF Core, PostgreSQL e execução containerizada. Não há código de aplicação neste documento.
