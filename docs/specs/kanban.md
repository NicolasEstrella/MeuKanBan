# Kanban

## Objetivo, contexto e problema

A colaboração precisa organizar trabalho em projetos privados sem perder contexto. O núcleo do produto é um projeto com múltiplos boards, nos quais cada card pertence a exatamente um board e pode ser editado ou movido sem sobrescrever silenciosamente uma alteração concorrente.

No MVP, a movimentação deve representar apenas a mudança de posição de um card dentro do board ao qual ele pertence. Permitir troca de board antes de existir uma regra de negócio, uma autorização e critérios próprios para essa operação criaria uma transição de contexto sem contrato funcional definido.

## Escopo

### Dentro do MVP

- CRUD de projetos, boards e cards conforme autorização.
- Múltiplos boards por projeto.
- Cards pertencentes a exatamente um board.
- Campos mínimos de card: título, descrição, posição, estado de arquivamento e metadados de autoria/atualização.
- Criação, edição, movimentação entre columns do mesmo board e arquivamento.
- Ordenação determinística dos cards dentro de cada column.
- Controle de versão para comandos mutáveis e resposta `409 Conflict` em conflito.
- Auditoria das mudanças relevantes.

### Fora do MVP

- Movimentação de um card para outro board, inclusive outro board do mesmo projeto.
- Chat, comentários em tempo real e notificações push, especificados separadamente.
- Anexos, automações, etiquetas avançadas, dependências entre cards e busca full-text.
- Exclusão física como comportamento padrão; retenção e restauração exigem decisão posterior.

### Evolução futura condicionada

A movimentação entre boards somente poderá ser incluída em uma evolução futura quando houver, em especificação própria:

- uma regra de negócio que defina quando a troca de board é válida;
- uma regra de autorização que defina quem pode executá-la;
- critérios de aceite específicos para a troca de board, incluindo validações de origem e destino, auditoria e concorrência;
- testes próprios para os cenários permitidos e rejeitados.

Até que esses itens sejam aprovados, qualquer tentativa de trocar o board do card permanece fora do MVP e deve ser rejeitada sem alterar dados.

## Regras de negócio

- Board pertence a exatamente um projeto.
- Card pertence a exatamente um board em qualquer instante.
- Card só pode ser movimentado entre columns do seu board atual durante o MVP.
- Uma operação de movimentação para qualquer outro board, mesmo que pertença ao mesmo projeto, não é válida no MVP.
- Não é permitido mover card entre projetos por comando de movimentação.
- Operações mutáveis exigem membership e permissão compatíveis com o papel.
- Cards arquivados não aparecem na visão ativa padrão, mas permanecem consultáveis por filtro autorizado.
- Toda alteração confirmada incrementa a versão do recurso de forma atômica.
- Conflito de versão não altera o estado persistido e retorna `409 Conflict`.
- Uma tentativa de troca de board não deve alterar o board, a column, a posição ou a versão do card.

## Casos de uso

### UC-01: criar card em um board

Usuário autorizado cria um card e o associa ao board informado. O card recebe uma versão inicial, uma posição válida e registro de auditoria.

### UC-02: mover card entre columns do mesmo board

Usuário autorizado informa o card, a column destino pertencente ao mesmo board e a versão esperada. O sistema atualiza a column e a posição, preserva o board do card, incrementa a versão e registra a alteração.

### UC-03: rejeitar tentativa de troca de board no MVP

Usuário tenta informar como destino uma column de outro board. O sistema rejeita a operação, independentemente de os boards pertencerem ao mesmo projeto, e preserva integralmente o estado do card.

### UC-04: arquivar card

Usuário autorizado arquiva o card com a versão atual. O card deixa a visão ativa, continua disponível para consulta autorizada e a alteração é auditada.

## Critérios de aceitação

### Cenário: criar board em projeto autorizado

- **Dado** que o usuário tem permissão de edição no projeto
- **Quando** cria um board válido
- **Então** o board é associado ao projeto informado
- **E** o board aparece apenas para usuários autorizados
- **E** a criação é auditada

### Cenário: criar card

- **Dado** que o board pertence ao projeto e o usuário pode editar o projeto
- **Quando** cria um card válido
- **Então** o card pertence exatamente ao board informado
- **E** recebe uma versão inicial
- **E** a criação é auditada

### Cenário: mover card entre columns do mesmo board

- **Dado** que o card pertence ao board de origem
- **E** a column destino pertence ao mesmo board do card
- **E** a versão esperada ainda é a versão persistida
- **Quando** o usuário move o card
- **Então** a mudança de column e posição é confirmada
- **E** o card continua pertencendo ao mesmo board
- **E** a versão é incrementada
- **E** a ordem dos cards permanece determinística
- **E** a alteração é auditada

### Cenário: impedir movimento para outro board do mesmo projeto

- **Dado** que o card pertence ao board de origem
- **E** a column destino pertence a outro board do mesmo projeto
- **Quando** o usuário tenta mover o card
- **Então** a operação é rejeitada por estar fora do MVP
- **E** o card permanece no board, na column, na posição e na versão originais
- **E** nenhuma alteração de movimentação é auditada como confirmada

### Cenário: impedir movimento para outro projeto

- **Dado** que a column destino pertence a um board de outro projeto
- **Quando** o usuário tenta mover o card
- **Então** a operação é rejeitada
- **E** o card permanece no board original
- **E** o estado persistido e a versão não são alterados

### Cenário: rejeitar troca de board sem regra futura aprovada

- **Dado** que não existe regra de negócio e autorização aprovadas para troca de board no MVP
- **Quando** qualquer usuário tenta trocar o board de um card
- **Então** a operação é rejeitada
- **E** nenhuma validação de sucesso de troca de board é aplicável ao MVP
- **E** o card permanece associado a exatamente um board

### Cenário: arquivar card

- **Dado** que o usuário possui permissão de edição
- **Quando** arquiva o card com a versão atual
- **Então** o card deixa a visão ativa
- **E** continua disponível para consulta autorizada
- **E** a alteração é auditada

### Cenário: viewer em operação mutável

- **Dado** que o usuário possui papel `Viewer`
- **Quando** tenta alterar board, column ou card
- **Então** a API rejeita a operação sem alterar dados

## Testes requeridos

- Testar que um card recém-criado pertence a exatamente um board.
- Testar que uma movimentação entre columns do mesmo board atualiza a posição, mantém o board e incrementa a versão.
- Testar que uma movimentação para column de outro board do mesmo projeto é rejeitada e não altera board, column, posição ou versão.
- Testar que uma movimentação para column de outro projeto é rejeitada e não altera o card.
- Testar que conflito de versão retorna `409 Conflict` sem persistir a movimentação.
- Testar que usuário `Viewer` não consegue mover card, alterar column ou trocar board.
- Testar que movimentações confirmadas e rejeitadas respeitam a auditoria definida.
- Manter um teste de regressão que falhe caso uma operação do MVP aceite destino em outro board.

## Definition of Done

- A regra “card pertence exatamente a um board” está documentada e coberta por teste.
- O único fluxo de movimentação aprovado no MVP é entre columns do mesmo board.
- Tentativas de mover para outro board, inclusive no mesmo projeto, são rejeitadas sem alteração persistida.
- Não existe critério de aceite, caso de uso ou teste do MVP que considere troca de board bem-sucedida.
- Controle de versão, autorização, ordenação e auditoria estão cobertos nos cenários aplicáveis.
- A evolução de troca de board está explicitamente fora do MVP e condicionada a regra de negócio, autorização e critérios próprios.
- A especificação permanece compatível com `project-membership.md`, `optimistic-concurrency.md` e `audit.md`.

## Dependências e metodologia

- Depende de `project-membership.md`, `optimistic-concurrency.md` e `audit.md`.
- Kanban é a primeira entrega funcional. SignalR e chat só entram depois dele.
- DDD explícito modela as invariantes de projeto, board e card; BDD leve cobre os comandos críticos; TDD seletivo cobre movimentação, rejeição de troca de board e invariantes.
