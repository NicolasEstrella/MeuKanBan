# Project Membership

## Contexto e problema

Projetos são privados. A colaboração só é segura quando a visibilidade e as ações são decididas pelo projeto e pelo recurso, e não apenas por autenticação ou papel global. Membership revogado não pode continuar vendo ou alterando conteúdo.

## Escopo

### Dentro

- Criação e administração de projetos privados.
- Associação de usuários a projetos com papel por projeto.
- Papéis `ProjectOwner`, `ProjectAdmin`, `Member` e `Viewer`, além da consideração controlada de `GlobalAdmin`.
- Convites ou inclusão de membros conforme fluxo definido pelo produto.
- Ativação, alteração de papel, revogação e consulta de membership.
- Autorização resource-based para projetos, boards, cards, auditoria e realtime.

### Fora

- Compartilhamento público ou projetos públicos.
- Federação de organizações, grupos externos e SSO.
- Permissões por campo de card ou por coluna, salvo decisão posterior.

## Regras de negócio

- Projeto novo é privado por padrão.
- Um usuário sem membership ativo não pode listar, consultar ou modificar recursos do projeto.
- Cada projeto possui um único `ProjectOwner` ativo, salvo regra explícita de transferência documentada.
- `ProjectAdmin` administra o projeto conforme as permissões definidas, mas não assume ownership automaticamente.
- `Member` pode colaborar nos boards/cards permitidos; `Viewer` pode consultar sem editar.
- `GlobalAdmin` não obtém acesso automático ao conteúdo privado; operações globais sobre conteúdo exigem política explícita e auditoria.
- A API deve resolver o recurso e seu projeto antes de autorizar operações quando o identificador não for suficiente.
- Acesso é negado por padrão em caso de membership ausente, inativo ou ambíguo.

## Critérios de aceitação

### Cenário: isolamento entre projetos

- **Dado** que o usuário é membro ativo do projeto A e não possui membership no projeto B
- **Quando** ele lista ou consulta boards/cards do projeto B
- **Então** a API rejeita o acesso
- **E** nenhum dado do projeto B é retornado

### Cenário: membership revogado

- **Dado** que o usuário tinha acesso ao projeto
- **Quando** seu membership é revogado
- **Então** novas consultas e comandos para o projeto são rejeitados
- **E** conexões ou grupos realtime aplicáveis deixam de receber atualizações
- **E** a revogação é auditada

### Cenário: viewer tenta editar

- **Dado** que o usuário possui papel `Viewer`
- **Quando** tenta criar, editar, mover ou arquivar um board/card
- **Então** a operação é rejeitada
- **E** o estado do recurso permanece inalterado

### Cenário: administrador altera membership

- **Dado** que o solicitante possui permissão de administração no projeto
- **Quando** adiciona um membro ou altera seu papel dentro das regras permitidas
- **Então** a mudança é aplicada
- **E** a operação é auditada
- **E** o novo escopo vale nas próximas decisões de autorização

### Cenário: usuário sem escopo tenta inferir existência

- **Dado** que o usuário não possui acesso ao projeto
- **Quando** consulta um identificador de projeto ou recurso privado
- **Então** a resposta não expõe conteúdo nem detalhes que permitam confirmar dados privados

## Dependências e metodologia

- Depende de `identity.md`, `kanban.md`, `realtime.md` e `audit.md`.
- DDD explícito define o projeto e membership como regras de domínio; BDD leve cobre fronteiras de autorização; TDD seletivo cobre matriz de papéis, revogação e acesso cruzado.
