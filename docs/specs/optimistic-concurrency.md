# Optimistic Concurrency

## Contexto e problema

Dois colaboradores podem editar o mesmo card a partir de leituras diferentes. Sem controle de concorrência, a última gravação pode apagar silenciosamente o trabalho anterior e destruir contexto. A solução de negócio é exigir uma versão conhecida e tornar o conflito explícito.

## Escopo

### Dentro

- Token de versão monotônico para cards e demais entidades editáveis relevantes.
- Comandos de edição, movimentação e arquivamento condicionados à versão esperada.
- Resposta HTTP `409 Conflict` para versão divergente.
- Nenhum retry cego de comando mutável.
- Dados suficientes para o cliente recarregar e apresentar decisão ao usuário.
- Auditoria de sucesso e conflito.

### Fora

- Resolução automática de conflitos por merge de campos.
- Lock pessimista, edição exclusiva ou fila de comandos.
- Sincronização realtime, coberta em `realtime.md`.

## Regras de negócio

- A versão devolvida na leitura é a versão que o cliente deve enviar no comando mutável.
- A confirmação só ocorre se o recurso ainda possuir a versão esperada.
- A atualização e o incremento de versão são atômicos.
- Conflito não altera o recurso e não incrementa sua versão.
- Uma resposta de conflito não deve induzir repetição automática sem nova leitura e decisão explícita.
- A mesma regra se aplica a alteração de conteúdo, posição, board e arquivamento.

## Critérios de aceitação

### Cenário: primeira atualização vence

- **Dado** que dois clientes leram o card na versão 7
- **Quando** o cliente A envia uma alteração com versão esperada 7
- **Então** a alteração é confirmada
- **E** a versão persistida passa a 8

### Cenário: segunda atualização entra em conflito

- **Dado** que o card já está na versão 8
- **Quando** o cliente B envia uma alteração com versão esperada 7
- **Então** a API responde `409 Conflict`
- **E** o estado do card permanece o produzido pelo cliente A
- **E** nenhuma alteração parcial é persistida
- **E** o conflito é auditado

### Cenário: movimento também valida versão

- **Dado** que o cliente possui versão desatualizada do card
- **Quando** tenta mover o card para outro board do mesmo projeto
- **Então** a API responde `409 Conflict`
- **E** o card não muda de board nem de posição

### Cenário: cliente recupera conflito

- **Dado** que um comando recebeu `409 Conflict`
- **Quando** o cliente recarrega o recurso
- **Então** recebe o estado e a versão mais recentes
- **E** pode submeter uma nova decisão com essa versão

## Dependências e metodologia

- Depende do modelo de card em `kanban.md` e dos eventos em `audit.md`.
- TDD seletivo é obrigatório para interleavings concorrentes, atomicidade e ausência de atualização parcial; BDD leve documenta os cenários acima; DDD define a versão como invariante do agregado editável.
