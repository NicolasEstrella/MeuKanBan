# Chat

## Contexto e problema

A colaboração pode exigir conversas ligadas ao contexto de um projeto ou board, mas chat introduz retenção, autorização, ordenação e notificações. Por isso, é uma capacidade posterior ao Kanban e ao transporte realtime, sem bloquear a organização inicial do trabalho.

## Escopo

### Dentro

- Conversas privadas vinculadas a um projeto e, quando definido, a um board.
- Mensagens persistidas com autor, horário e identificador estável.
- Leitura e envio condicionados ao membership ativo.
- Ordenação determinística e paginação de histórico.
- Auditoria de ações administrativas e eventos de segurança relevantes.
- Eventos realtime de novas mensagens somente para participantes autorizados.

### Fora

- Conversas públicas fora de projetos.
- Mensagens efêmeras, edição avançada, reações, anexos, chamadas e moderação automatizada.
- Definição de retenção legal ou exportação, que requer decisão própria.

## Regras de negócio

- Toda conversa pertence a um projeto privado.
- Uma mensagem não pode ser enviada por usuário sem membership ativo.
- Revogação de membership impede leitura e envio posteriores conforme a política de retenção definida.
- Mensagens persistidas não podem ser alteradas silenciosamente; edição e remoção, se aprovadas, devem deixar evidência.
- Chat não altera o estado de cards diretamente.
- A publicação realtime não substitui a persistência nem a autorização por requisição.

## Critérios de aceitação

### Cenário: membro envia mensagem

- **Dado** que o usuário possui membership ativo e a conversa pertence ao projeto
- **Quando** envia uma mensagem não vazia dentro dos limites definidos
- **Então** a mensagem é persistida com autor e horário
- **E** participantes autorizados podem receber o evento realtime

### Cenário: usuário sem acesso tenta ler

- **Dado** que o usuário não possui membership ativo no projeto
- **Quando** consulta o histórico da conversa
- **Então** a API rejeita a leitura
- **E** não retorna mensagens privadas

### Cenário: mensagem fora do projeto

- **Dado** que a conversa e o recurso de destino pertencem a projetos diferentes
- **Quando** o usuário tenta associá-los
- **Então** a operação é rejeitada
- **E** nenhuma associação inválida é persistida

## Dependências e metodologia

- Depende de `identity.md`, `project-membership.md`, `realtime.md`, `notifications.md` e `audit.md`.
- Chat só será iniciado depois do Kanban e do realtime básico.
- DDD explícito modela conversa e mensagem; BDD leve cobre isolamento; TDD seletivo cobre ordenação, paginação e autorização.
