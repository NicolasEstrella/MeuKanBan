# T02 - Monorepo, servicos e containers

## Tipo e objetivo

Feature de fundacao operacional. O objetivo e permitir que uma pessoa obtenha um ambiente local reproduzivel com Angular, API ASP.NET Core e PostgreSQL em containers, usando configuracao externa e sem transformar o modular monolith em microsservicos.

O problema real e a divergencia de ambiente entre maquinas e a falta de um caminho deterministico para validar a API contra o banco desde o inicio.

## Escopo

### Dentro

- Estrutura de monorepo para frontend Angular, API ASP.NET Core, testes, infraestrutura e documentacao.
- Servicos `frontend`, `api` e `postgres` no Docker Compose.
- Dockerfiles versionados para frontend e API, com base e etapas adequadas ao ambiente.
- Variaveis de ambiente e mecanismo local de secrets sem valores reais versionados.
- Rede interna, volumes de desenvolvimento e health checks.
- Inicializacao reproduzivel e criterio de parada quando dependencias essenciais nao estao saudaveis.

### Fora

- Deploy de producao, Kubernetes, service mesh, broker, cache ou microsservicos.
- Pipeline CI/CD completo.
- Politica final de backup, escala ou alta disponibilidade.
- Implementacao de funcionalidades de Identity, Kanban ou Chat.

## Decisoes e premissas

- O Compose local inclui os tres componentes; PostgreSQL e dependencia compartilhada da API.
- A API permanece um processo modular; containers nao definem bounded contexts.
- O frontend comunica com a API por URL configuravel, sem URL fixa de producao no codigo.
- Persistencia PostgreSQL usa volume nomeado no desenvolvimento para sobreviver a reinicializacao comum.
- Health check de PostgreSQL verifica disponibilidade real do banco; API verifica seu processo e sua dependencia essencial conforme o contrato definido em T02.
- A senha do PostgreSQL e fornecida por Docker secret baseado em arquivo (`infra/postgres/secrets/`, nao versionado, caminho configuravel via `POSTGRES_PASSWORD_FILE`), montado em `/run/secrets/postgres_password`. O Postgres consome via `POSTGRES_PASSWORD_FILE` e a API compoe a connection string lendo o arquivo no startup; nenhum segredo trafega em variavel de ambiente, portanto `docker compose config` e `docker inspect` exibem apenas caminhos.
- Imagens devem usar versoes controladas e conter somente os runtimes necessarios para os componentes definidos nesta spec. Imagens base sao fixadas por digest (`tag@sha256:...`), verificado via `docker buildx imagetools inspect` na data do pin.

## Conceitos aprendidos

- **Monorepo**: uma arvore Git com componentes e testes do produto.
- **Servico Compose**: unidade de processo local, nao bounded context.
- **Health check**: sinal verificavel de prontidao ou vitalidade.
- **Rede interna**: comunicacao entre servicos por nome de servico.
- **Volume**: persistencia explicitamente escolhida para desenvolvimento.
- **Configuracao externa**: valores fornecidos pelo ambiente, secrets ou override local nao versionado.

## Casos de uso

1. Desenvolvedor clona o monorepo e inicia o Compose com o comando documentado.
2. PostgreSQL inicializa com credenciais locais fornecidas externamente.
3. API aguarda ou reporta claramente a indisponibilidade do banco e disponibiliza health check.
4. Frontend sobe e usa a URL configurada para chamar a API.
5. Desenvolvedor derruba e recria containers sem perder dados do volume nomeado, salvo limpeza explicita.
6. Desenvolvedor valida a topologia com `docker compose config` sem revelar segredos reais.

## Criterios de aceite

### Cenario: configuracao valida do Compose

- **Dado** um checkout limpo e os valores locais obrigatorios fornecidos por ambiente/secrets
- **Quando** o desenvolvedor executa `docker compose config`
- **Entao** a configuracao e aceita sem placeholders obrigatorios nao resolvidos
- **E** a saida nao contem segredos reais

### Cenario: subida reproduzivel

- **Dado** Docker Desktop disponivel e nenhum estado anterior necessario
- **Quando** o desenvolvedor executa o comando documentado para subir o ambiente
- **Entao** os servicos `frontend`, `api` e `postgres` iniciam com nomes e rede definidos
- **E** a API conecta ao PostgreSQL pelo nome do servico

### Cenario: dependencia indisponivel

- **Dado** que PostgreSQL esta parado ou nao saudavel
- **Quando** a API tenta iniciar ou responder ao health check
- **Entao** o estado de saude indica falha de dependencia
- **E** nenhum falso positivo informa que a API esta pronta para operar

### Cenario: persistencia de desenvolvimento

- **Dado** que o Compose foi reiniciado sem remover o volume nomeado
- **Quando** PostgreSQL sobe novamente
- **Entao** os dados de desenvolvimento persistem
- **E** remover o volume e uma acao explicita e documentada

### Cenario: comunicacao do frontend

- **Dado** o frontend em execucao e a URL da API configurada pelo ambiente
- **Quando** o frontend inicia uma chamada de verificacao
- **Entao** a chamada usa a URL configurada
- **E** nenhuma URL ou credencial de producao e embutida na imagem ou no repositorio

### Cenario: stack prevista

- **Dado** os Dockerfiles e o Compose do projeto
- **Quando** eles sao inspecionados
- **Entao** os processos executados sao Angular, ASP.NET Core e PostgreSQL
- **E** nenhum runtime adicional e necessario para a topologia aprovada

## Testes

- `docker compose config --quiet` em checkout limpo com arquivo local de exemplo (valida sem imprimir valores).
- Smoke test versionado (`tests/Smoke/t02-compose-smoke.ps1`): subida, health checks, runtime config do frontend, conectividade API/PostgreSQL e recuperacao do banco, sem destruir o volume.
- Teste de reinicio com volume e teste separado de limpeza explicita.
- Verificacao de que variaveis obrigatorias ausentes produzem erro claro.
- Busca automatizada por connection strings reais, JWT keys, senhas e tokens em arquivos versionados.
- Inspecao de imagens para confirmar bases e versoes controladas (pin por digest verificado).

## Definition of Done

- [ ] Monorepo possui locais documentados para Angular, API, testes e infraestrutura.
- [ ] Compose declara frontend, API, PostgreSQL, rede e volume.
- [ ] Dockerfiles constroem e iniciam os componentes corretos.
- [ ] Variaveis obrigatorias, defaults locais seguros e secrets estao documentados.
- [ ] Health checks cobrem PostgreSQL e API, com dependencias observaveis.
- [ ] Smoke test comprova inicializacao e conexao da API ao PostgreSQL.
- [ ] Segredos reais nao aparecem no Git, logs de inicializacao ou imagem.
- [ ] Documentacao explica subida, parada, reinicio e limpeza do ambiente.
- [ ] Nenhuma funcionalidade de aplicacao foi implementada nesta etapa.

## Riscos

- Health check mal definido pode mascarar banco indisponivel.
- Volume local pode reter estado invalido entre testes.
- Configuracao embutida no frontend pode expor valores que deveriam ser apenas de servidor.
- Imagens nao fixadas podem mudar sem revisao.

## Decisoes ainda abertas

- Versoes exatas de Node, Angular, .NET, PostgreSQL e imagens base.
- Estrategia de hot reload e bind mounts para desenvolvimento.
- Se migrations rodam na inicializacao da API ou por comando explicito em T04.
- Endpoint e profundidade dos health checks de prontidao.
- Forma de fornecer secrets no CI futuro (local Windows resolvido: Docker secret baseado em arquivo).
