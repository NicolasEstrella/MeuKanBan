# Realtime

## Contexto e problema

Colaboradores precisam perceber mudanças relevantes sem depender de atualização manual contínua, mas a entrega em tempo real não pode ampliar o escopo do núcleo Kanban nem vazar eventos de projetos privados. O transporte realtime será adicionado depois do Kanban.

## Escopo

### Dentro

- Atualizações de boards/cards autorizados via SignalR.
- Grupos ou canais limitados ao projeto/board autorizado.
- Entrada, permanência e saída condicionadas ao membership vigente.
- Publicação de eventos somente após a mudança de domínio ser confirmada.
- Reconexão que revalida identidade e autorização.
- Compatibilidade com o controle de versão e `409` da API.

### Fora

- Chat e mensagens, definidos em `chat.md`.
- Presença detalhada, digitação, chamadas ou colaboração de cursor.
- Garantia de entrega exatamente uma vez.
- Substituição da API HTTP para comandos de escrita.

## Regras de negócio

- Não existe canal realtime público para conteúdo privado.
- O cliente não pode escolher livremente um grupo sem validação do servidor.
- Revogação de membership impede novos eventos e remove o usuário dos grupos aplicáveis.
- Evento realtime informa que o recurso mudou; não autoriza o cliente a ignorar a versão.
- Falha de entrega não desfaz a alteração persistida; o cliente deve poder recarregar via API.
- Eventos de conflito não publicam uma alteração inexistente como se fosse sucesso.

## Critérios de aceitação

### Cenário: alteração autorizada é publicada

- **Dado** que dois usuários possuem acesso ao mesmo projeto
- **Quando** um deles confirma uma alteração de card pela API
- **Então** o outro recebe um evento do projeto/board autorizado
- **E** o evento contém identificador e versão nova ou informação equivalente para recarga

### Cenário: usuário não autorizado não recebe evento

- **Dado** que o usuário não possui membership ativo no projeto
- **Quando** ocorre uma alteração nesse projeto
- **Então** ele não recebe o evento
- **E** não consegue ingressar no grupo correspondente

### Cenário: reconexão após revogação

- **Dado** que o usuário perdeu o membership enquanto estava desconectado
- **Quando** tenta reconectar
- **Então** a autorização é reavaliada
- **E** a conexão não ingressa no grupo privado

### Cenário: cliente recebe evento atrasado

- **Dado** que o cliente recebeu uma versão menor que a versão local conhecida
- **Quando** processa o evento
- **Então** não regride o estado local
- **E** pode recarregar o recurso para obter a versão vigente

## Dependências e metodologia

- Depende de `project-membership.md`, `kanban.md`, `optimistic-concurrency.md` e `audit.md`.
- SignalR/realtime só começa após o núcleo Kanban estar entregue.
- DDD mantém realtime como contexto de transporte; BDD leve cobre isolamento e reconexão; TDD seletivo cobre autorização de grupos e ordenação básica de versões.
