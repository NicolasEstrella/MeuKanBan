# MeuKanBan

MeuKanBan e uma aplicacao de gestao de trabalho para projetos privados, com multiplos boards e cards.

## Direcao do produto

- Frontend: Angular.
- API: ASP.NET Core.
- Persistencia: EF Core + PostgreSQL.
- Repositorio: monorepo.
- Desenvolvimento local: componentes containerizados com Docker Compose.
- Arquitetura: modular monolith com fronteiras DDD explicitas.
- Autenticacao: cadastro aberto, JWT curto e refresh token rotativo persistido somente como hash.
- Autorizacao: `GlobalAdmin`, `ProjectOwner`, `ProjectAdmin`, `Member` e `Viewer`.
- Projetos: privados por padrao.
- Concorrencia: `Version bigint`; conflitos retornam HTTP `409 Conflict`.
- Auditoria: presente desde a primeira entrega.
- Realtime e chat: evolucao posterior ao nucleo Kanban.
- Branch principal: `main`.

## Estado atual

O monorepo possui a estrutura de aplicacao criada na T02: solucao .NET 10
(`MeuKanBan.slnx`) com `Api`, `Application`, `Domain` e `Infrastructure`,
projetos de teste xUnit em `tests/` e a aplicacao Angular em
`frontend/angular-app/`. A API e minima nesta fase: apenas health checks
(`/health/live` e `/health/ready`, este ultimo verificando o PostgreSQL).

O Docker Compose declara os tres servicos da fundacao com Dockerfiles reais,
rede interna, volume nomeado e health checks. Segredos locais ficam no `.env`
(nao versionado); o repositorio contem apenas `.env.example` com placeholders.

A especificacao existente esta em `docs/specs/` e o modelo de dominio inicial esta em `docs/domain/domain-model.md`.

## Estrutura

```text
docs/              Documentacao de produto, arquitetura, roadmap e decisoes
docs/decisions/    Registro de decisoes evolutivo
infra/postgres/    Artefatos futuros de infraestrutura do PostgreSQL
src/Api/           Camada HTTP ASP.NET Core
src/Application/  Casos de uso e contratos de aplicacao
src/Domain/       Modelo e regras de dominio
src/Infrastructure/ Persistencia e adaptadores
frontend/angular-app/ Aplicacao Angular
tests/             Testes unitarios, integracao, API e E2E
```

## Inicio local

Pre-requisitos: Docker Desktop, um `.env` local criado a partir do exemplo e o
arquivo de senha do PostgreSQL (Docker secret local, nao versionado):

```bash
cp .env.example .env   # ajuste POSTGRES_DB/POSTGRES_USER se necessario
# crie o arquivo de senha (apenas a senha, em uma linha):
Set-Content infra/postgres/secrets/postgres_password 'sua-senha-local' -NoNewline
```

A senha NUNCA fica no `.env` nem em variavel de ambiente dos containers: o
Compose a monta como arquivo em `/run/secrets/postgres_password`. Assim,
`docker compose config` e `docker inspect` exibem apenas caminhos, nunca o
valor. Para usar outro caminho, defina `POSTGRES_PASSWORD_FILE` no `.env`.

Suba o ambiente completo (PostgreSQL, API e frontend):

```bash
docker compose build
docker compose up -d
docker compose ps      # aguarde os tres servicos healthy
```

Endpoints de verificacao:

- Frontend: http://localhost:4200
- API readiness (processo + PostgreSQL): http://localhost:8080/health/ready
- API liveness (apenas processo): http://localhost:8080/health/live

### Validacao segura e smoke test

Valide a topologia sem expor segredos (`--quiet` nao imprime a saida):

```bash
docker compose config --quiet
```

O smoke test versionado comprova a stack ponta a ponta - config sem vazamento,
subida healthy, health checks, runtime config do frontend e recuperacao do
PostgreSQL - sem destruir o volume de dados (nunca executa `down -v`):

```bash
pwsh tests/Smoke/t02-compose-smoke.ps1   # -SkipBuild para reusar imagens
```

Os dados do PostgreSQL persistem no volume nomeado `postgres-data` entre
reinicios (`docker compose down` preserva). A remocao do volume e uma acao
explicita e destrutiva:

```bash
docker compose down -v   # APAGA todos os dados locais do PostgreSQL
```

### Inner loop nativo (sem container para api/frontend)

Com o toolchain local (.NET 10 SDK, Node 24), o desenvolvimento com hot reload
roda nativamente e apenas o PostgreSQL fica em container:

```bash
docker compose up -d postgres
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=meukanban_db;Username=<user>;Password=<senha>" --project src/Api
dotnet watch run --project src/Api
cd frontend/angular-app; npm install; npm start
```

A URL da API consumida pelo frontend vem de
`frontend/angular-app/public/assets/config.json` (desenvolvimento nativo) ou da
variavel `API_URL` do ambiente (container, via `config.json.template`). Nenhuma
URL ou credencial de producao e embutida na imagem ou no repositorio. No fluxo
nativo, a senha segue no user-secrets; o suporte a `Postgres:PasswordFile` da
API e opcional e usado apenas no Compose.

Consulte `docs/02-roadmap.md` e `docs/05-security-baseline.md` antes
de iniciar implementacao.

## Convencoes

- Branch principal: `main`.
- Mudancas de dominio devem preservar as invariantes documentadas.
- Casos criticos usam cenarios BDD leves e testes automatizados seletivos.
- Nao introduzir Java, Spring, RabbitMQ, Kafka ou microsservicos no baseline.
