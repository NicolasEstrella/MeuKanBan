# Decision Log

Registro resumido das decisoes confirmadas e das lacunas que ainda nao devem ser tratadas como requisitos.

| ID | Decisao | Estado | Consequencia |
|---|---|---|---|
| D-001 | Angular + ASP.NET Core + EF Core + PostgreSQL | Accepted | stack unica do baseline |
| D-002 | Monorepo em branch principal `main` | Accepted | componentes e testes evoluem juntos |
| D-003 | Modular monolith em vez de microsservicos | Accepted | modulos explicitos no mesmo processo |
| D-004 | Docker Compose para ambiente local | Accepted | API, Angular e PostgreSQL serao containerizados |
| D-005 | Cadastro aberto com JWT e refresh rotation | Accepted | tokens curtos, revogacao e hash de refresh |
| D-006 | Papeis globais/de projeto definidos | Accepted | `GlobalAdmin`, `ProjectOwner`, `ProjectAdmin`, `Member`, `Viewer` |
| D-007 | Projetos privados por padrao | Accepted | autorizacao resource-based e deny by default |
| D-008 | Multiplos boards por projeto; card em um board | Accepted | invariantes de ownership no dominio |
| D-009 | Concorrencia otimista via `Version bigint` | Accepted | conflito HTTP `409`, sem update parcial |
| D-010 | Auditoria desde o inicio | Accepted | registro append-only e minimizado |
| D-011 | SignalR e chat depois do Kanban | Accepted | nao bloqueiam o MVP |
| D-012 | Duracao exata dos tokens | Proposed | validar politica de risco |
| D-013 | Armazenamento do refresh token no cliente | Proposed | decidir conforme XSS/CSRF e UX |
| D-014 | Poderes de `GlobalAdmin` e ownership | Proposed | fechar matriz de autorizacao |
| D-015 | Retencao/exportacao de auditoria e chat | Proposed | validar privacidade/LGPD |
| D-016 | Cache, broker, busca distribuida e extracao de modulo | Proposed | somente apos metricas e necessidade comprovada |

Nenhuma decisao deste log autoriza Java, Spring, RabbitMQ, Kafka ou microsservicos no desenho atual.
