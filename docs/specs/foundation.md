# T01 - Fundacao DDD e linguagem ubiqua

## Tipo e objetivo

Feature de fundacao arquitetural. O objetivo e estabelecer uma linguagem comum e fronteiras verificaveis para que as etapas seguintes implementem o MeuKanBan como um modular monolith, sem CRUD acoplado e sem antecipar microsservicos.

A necessidade real e reduzir ambiguidade sobre ownership, invariantes e dependencias antes de criar API, persistencia e identidade. O resultado esperado e uma referencia de dominio que possa ser revisada por produto, desenvolvimento e QA.

## Escopo

### Dentro

- Linguagem ubiqua para identidade, sessao, projeto, membership, board, card, autorizacao, auditoria e correlation id.
- Bounded contexts iniciais: Identity & Access, Projects & Membership, Authorization, Boards & Cards, Audit, Notifications, Realtime e Chat.
- Ownership de agregados e regras de negocio.
- Dependencias permitidas entre modulos e contratos entre eles.
- Regras DDD para agregados, value objects, eventos e consultas.
- Definition of Done da fundacao.

### Fora

- Implementacao de entidades, endpoints, tabelas ou containers.
- Matriz final de permissoes por acao, a ser fechada em T10.
- Design detalhado de SignalR, chat, notificacoes, ordenacao de cards e retencao de auditoria.
- Extracao de microsservicos ou bancos fisicamente separados.

## Decisoes e premissas

- A arquitetura e um modular monolith em uma API ASP.NET Core, com Angular e PostgreSQL.
- Cada contexto e dono de seu modelo, invariantes, casos de uso e persistencia logica.
- `Authorization` decide acesso resource-based, mas nao usurpa invariantes de `Projects`, `Boards & Cards` ou `Identity & Access`.
- `Audit` recebe fatos confirmados e nao concede autorizacao.
- Realtime, Notifications e Chat nao ampliam acesso e entram depois do nucleo Kanban.
- O banco pode ser compartilhado no baseline; ownership logico e migrations por modulo permanecem obrigatorios.
- A metodologia e DDD explicito, BDD leve para comportamentos criticos e TDD seletivo para invariantes, autorizacao, concorrencia e rotacao de sessao.

## Conceitos aprendidos

- **Identity**: identidade autenticavel; nao e membership.
- **Refresh Session**: sessao e familia de rotacao; guarda apenas material nao reutilizavel.
- **Project**: raiz do escopo privado.
- **Project Membership**: vinculo ativo/inativo e papel no projeto.
- **Board**: organizacao pertencente a um projeto.
- **Card**: unidade de trabalho pertencente a exatamente um board.
- **Resource Scope**: identidade, acao, recurso e projeto usados na autorizacao.
- **Audit Entry**: fato append-only, minimizado, com resultado e correlation id quando disponivel.
- **Version**: versao monotona exigida por comandos mutaveis.

## Casos de uso

1. Um caso de uso de Identity cria identidade ou renova sessao sem expor credenciais a outros contextos.
2. Um caso de uso de Projects cria projeto privado e gerencia membership conforme regras aprovadas.
3. Um caso de uso de Boards & Cards cria, edita, move ou arquiva trabalho apenas dentro do projeto correto.
4. Authorization resolve o recurso e decide permitir ou negar por escopo e papel.
5. Audit registra o fato confirmado, rejeicao relevante ou conflito sem receber senha, token ou segredo.
6. Realtime e Chat, quando implementados, consomem contratos autorizados sem alterar o estado do Kanban diretamente.

## Dependencias permitidas

- `Identity & Access` pode fornecer identidade autenticada e claims; nao conhece cards ou auditoria interna.
- `Projects & Membership` pode consultar identidade por contrato e fornece membership/escopo; nao acessa credenciais.
- `Authorization` pode consultar contratos de identidade, membership e recurso; nao grava entidades desses contextos.
- `Boards & Cards` pode exigir uma decisao de autorizacao e publicar fatos para Audit; nao acessa tabelas de Identity ou Membership diretamente.
- `Audit` pode receber fatos de qualquer contexto por contrato; nao chama o fluxo de negocio para decidir acesso.
- `Notifications`, `Realtime` e `Chat` dependem de fatos/contratos autorizados, nunca de queries internas ou acesso direto a tabelas de outro modulo.

Nao sao permitidos imports de entidades internas, repositorios, DbSets ou migrations de outro contexto, nem consultas SQL cruzando ownership sem um contrato explicito.

## Criterios de aceite

### Cenario: fronteiras dos contextos

- **Dado** o mapa de contextos definido nesta spec
- **Quando** um modulo precisar de dado de outro contexto
- **Entao** o acesso ocorre por contrato publicado
- **E** o modulo consumidor nao referencia entidade, repositorio ou DbSet interno do modulo dono

### Cenario: invariantes no modulo dono

- **Dado** um comando para criar ou alterar um recurso
- **Quando** a regra de pertencimento ou estado for avaliada
- **Entao** a decisao final pertence ao contexto dono do agregado
- **E** Authorization apenas decide se o ator pode executar a operacao

### Cenario: isolamento privado

- **Dado** um projeto privado e um usuario sem escopo autorizado
- **Quando** qualquer caso de uso tentar consultar ou alterar seu board ou card
- **Entao** a operacao e negada
- **E** nenhum contexto auxiliar concede acesso por papel global, notificacao ou realtime

### Cenario: fatos de auditoria

- **Dado** um evento de seguranca ou dominio confirmado, rejeitado ou conflitante
- **Quando** o fato for enviado ao contexto Audit
- **Entao** ele inclui ator quando conhecido, acao, recurso, projeto quando aplicavel, resultado, timestamp UTC e correlation id quando disponivel
- **E** nao inclui senha, token, hash de senha ou segredo

## Testes

- Checklist de termos e aliases proibidos revisado com o modelo de dominio.
- Teste arquitetural verifica que dependencias atravessam apenas contratos permitidos.
- Teste de unidade ou tabela verifica ownership e invariantes centrais.
- Cenarios BDD de isolamento, pertencimento, conflito e auditoria sao revisados como contratos para T03-T09 e etapas posteriores.
- Validacao manual confirma que SignalR/chat aparecem como evolucao posterior, nao como dependencia da fundacao.

## Definition of Done

- [ ] Linguagem ubiqua publicada e sem termos conflitantes com `domain-model.md`.
- [ ] Cada agregado tem contexto dono e fronteira explicita.
- [ ] Dependencias permitidas e proibidas estao documentadas.
- [ ] Invariantes centrais sao rastreaveis a cenarios testaveis.
- [ ] Auditoria, seguranca e correlation id aparecem desde a fundacao.
- [ ] T01 nao exige microsservico, broker, SignalR ou chat.
- [ ] Documento revisado contra ADR, charter, security baseline e testing strategy.
- [ ] Nenhum codigo de aplicacao foi criado nesta etapa.

## Riscos

- Fronteiras iniciais podem mudar conforme o dominio seja aprendido.
- Um banco compartilhado facilita o inicio, mas pode incentivar queries cruzadas.
- `Authorization` pode acumular regras demais se o ownership nao for revisado em cada caso de uso.
- Claims podem ser confundidas com permissao completa; a autorizacao resource-based continua obrigatoria.

## Decisoes ainda abertas

- Matriz final de permissoes por papel e acao, T10.
- Transferencia e cardinalidade de `ProjectOwner`.
- Estrategia de schemas fisicos por contexto.
- Politica detalhada de retencao e consulta de auditoria.
- Condicoes objetivas para extrair um modulo para outro processo.
