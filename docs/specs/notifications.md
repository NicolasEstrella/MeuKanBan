# Notifications

## Contexto e problema

Colaboradores precisam tomar conhecimento de mudanças relevantes sem consultar todos os boards continuamente. Notificações devem reduzir perda de contexto sem conceder acesso ao conteúdo privado nem se tornar requisito para confirmar uma operação de domínio.

## Escopo

### Dentro

- Contrato de notificação para eventos relevantes de projeto, membership e colaboração.
- Preferência por destinatário quando esse conceito for introduzido.
- Estado mínimo de leitura quando houver inbox no produto.
- Entrega in-app e integração com eventos realtime quando disponíveis.
- Respeito ao escopo de projeto e à revogação de acesso.

### Fora

- E-mail, SMS, push móvel e integrações externas no primeiro corte.
- Motor de recomendação, digest inteligente e regras de prioridade avançadas.
- Notificação antes da confirmação da transação de domínio.

## Regras de negócio

- Notificação não cria nem altera recursos de domínio.
- Evento de notificação deve referenciar apenas recursos que o destinatário pode acessar no momento da entrega/consulta.
- Falha de notificação não desfaz uma alteração de board/card.
- Eventos críticos de segurança não devem depender somente de notificação para auditoria.
- Membership revogado impede novas notificações privadas desse projeto.
- Duplicidade de entrega não pode causar duplicidade de alteração de domínio.

## Critérios de aceitação

### Cenário: alteração relevante gera notificação

- **Dado** que uma alteração de card foi confirmada
- **E** o destinatário possui acesso ao projeto
- **Quando** o contrato de notificação é processado
- **Então** uma notificação é disponibilizada ao destinatário conforme o canal habilitado
- **E** a notificação não contém segredo ou dados fora do escopo autorizado

### Cenário: falha de entrega

- **Dado** que a alteração de domínio foi confirmada
- **Quando** o processamento de notificação falha
- **Então** o estado do card permanece confirmado
- **E** a falha pode ser observada/reprocessada segundo a política definida

### Cenário: acesso revogado

- **Dado** que uma notificação privada ainda não foi lida
- **Quando** o destinatário perde acesso ao projeto
- **Então** a consulta da notificação não expõe conteúdo privado
- **E** o comportamento de remoção ou mascaramento segue a política de retenção definida

## Dependências e metodologia

- Depende de `project-membership.md`, `audit.md`, `realtime.md` e dos eventos de `kanban.md`/`chat.md`.
- Notificações podem começar com processamento interno; broker externo não é requisito inicial.
- DDD define notificações como contexto separado; BDD leve cobre autorização e falhas; TDD seletivo cobre idempotência e isolamento.
