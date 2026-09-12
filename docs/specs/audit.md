# Audit

## Contexto e problema

Projetos privados e colaboração concorrente exigem investigação confiável: quem fez o quê, em qual recurso, quando e com qual resultado. Sem auditoria desde o início, incidentes de acesso indevido e conflitos ficam sem contexto e não podem ser reconstruídos com segurança.

## Escopo

### Dentro

- Registro append-only, consultável por escopo autorizado.
- Eventos de identidade, sessão, autorização, membership, projeto, board, card e concorrência.
- Ator, ação, recurso, projeto, timestamp UTC, resultado e correlation id quando disponíveis.
- Registro de sucesso, rejeição relevante e conflito de concorrência.
- Minimização de dados e proteção contra inclusão de segredos.
- Política clara de consistência para eventos críticos.

### Fora

- SIEM, retenção legal específica, exportação regulatória e análise avançada.
- Auditoria de cada leitura comum de card, salvo exigência posterior.
- Armazenamento de tokens, senhas, hashes de senha ou payloads desnecessários.

## Regras de negócio

- Registros existentes não são editados nem removidos pela operação normal.
- Escrita de auditoria é controlada pelo contexto de auditoria, mesmo que os eventos sejam produzidos por outros módulos.
- Alterações essenciais de segurança e domínio devem ter política transacional explícita; para o MVP, devem ser confirmadas junto da operação ou por mecanismo transacional equivalente.
- Auditoria não pode conceder acesso ao recurso auditado a quem não teria autorização para consultá-lo.
- Dados antes/depois são minimizados e não incluem segredos.
- Falha na auditoria de operação crítica deve ser tratada conforme política de consistência definida, não silenciosamente ignorada.

## Eventos mínimos

- Cadastro, login bem-sucedido e falho quando aplicável, refresh, revogação e logout.
- Criação/alteração de projeto.
- Inclusão, alteração de papel e revogação de membership.
- Criação, edição, movimentação e arquivamento de board/card.
- Conflitos `409 Conflict` e tentativas relevantes de autorização negada.
- Decisões administrativas e mudanças de configuração de segurança.

## Critérios de aceitação

### Cenário: alteração de card auditada

- **Dado** que o usuário possui permissão e a alteração é confirmada
- **Quando** o card é criado, editado, movido ou arquivado
- **Então** existe um registro com ator, ação, recurso, projeto, timestamp UTC e resultado
- **E** o registro não contém token ou senha

### Cenário: conflito auditado

- **Dado** que a versão esperada diverge da versão persistida
- **Quando** a API responde `409 Conflict`
- **Então** o conflito é registrado com recurso, projeto, ator quando conhecido e resultado
- **E** nenhuma mudança falsa é descrita como confirmada

### Cenário: consulta autorizada

- **Dado** que o usuário possui permissão de consulta de auditoria no projeto
- **Quando** consulta o histórico
- **Então** recebe apenas eventos dentro do escopo permitido
- **E** os registros retornam em ordem determinística

### Cenário: consulta não autorizada

- **Dado** que o usuário não possui permissão para consultar auditoria do projeto
- **Quando** tenta acessar o histórico
- **Então** a API rejeita a operação
- **E** não expõe eventos privados

## Dependências e metodologia

- Depende de todos os contextos que produzem eventos, especialmente `identity.md`, `project-membership.md`, `kanban.md` e `optimistic-concurrency.md`.
- Auditoria existe desde a primeira entrega do produto.
- DDD trata auditoria como contexto transversal com contrato próprio; BDD leve cobre eventos obrigatórios; TDD seletivo cobre append-only, minimização e atomicidade.
