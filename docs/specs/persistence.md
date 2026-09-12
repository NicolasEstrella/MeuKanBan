# T04 - Persistencia inicial com EF Core e PostgreSQL

## Tipo e objetivo

Feature de fundacao de dados. O objetivo e estabelecer a conexao reproduzivel entre a API e PostgreSQL, migrations controladas e ownership modular de persistencia sem antecipar tabelas de features ainda nao modeladas.

A necessidade real e garantir que o estado do dominio possa ser criado, versionado, atualizado em UTC e protegido por constraints desde o primeiro banco vazio, sem esconder acoplamento entre contextos.

## Escopo

### Dentro

- EF Core e provider PostgreSQL configurados para a API.
- DbContext modular ou composicao equivalente com ownership explicito por contexto.
- Migrations versionadas e aplicaveis a banco vazio.
- Timestamps persistidos e comparados em UTC.
- Identificadores UUID para entidades persistidas conforme modelo.
- `Version bigint` monotona para entidades mutaveis relevantes.
- Constraints iniciais de unicidade, pertencimento, nulidade, estado e relacionamento.
- Conexao ao PostgreSQL containerizado e estrategia de aplicacao de migration.
- Validacao de banco limpo e estrategia documentada de rollback/forward migration.

### Fora

- Tabelas completas de Identity, Projects, Boards, Cards ou Audit alem do minimo necessario para validar a fundacao.
- Seed de dados de produto.
- Sharding, replicas de leitura, event sourcing ou schemas fisicos obrigatorios por modulo.
- Implementacao de concorrencia de cada caso de uso; apenas o contrato de `Version` e estabelecido.

## Decisoes e premissas

- PostgreSQL e a persistencia principal do baseline.
- EF Core e o acesso de persistencia; dominio nao depende de EF Core diretamente quando isso violar a fronteira adotada.
- Um banco e schema operacional compartilhado sao aceitos inicialmente, com ownership logico por modulo e migrations rastreaveis.
- Cada modulo controla seus mapeamentos, DbSets e alteracoes de schema; migrations nao devem criar tabelas de outro ownership sem decisao explicita.
- UUID e o identificador externo estavel; a geracao deve ser segura e consistente entre aplicacao e banco.
- `Version bigint` inicia em valor definido pelo contrato e incrementa atomicamente apenas em alteracao confirmada.
- Timestamps de dominio e auditoria sao UTC; conversoes para apresentacao ocorrem fora da persistencia.
- Migrations devem ser aplicaveis a banco vazio e a upgrades conhecidos sem depender de estado manual oculto.

## Conceitos aprendidos

- **DbContext modular**: unidade de mapeamento e acesso que respeita ownership do modulo.
- **Migration**: alteracao versionada e reproduzivel do schema.
- **UUID**: identificador estavel sem semantica de sequencia para o cliente.
- **UTC**: referencia unica para timestamps persistidos.
- **Version**: token `bigint` monotono usado em concorrencia otimista.
- **Constraint**: garantia estrutural de unicidade, nulidade, referencia e pertencimento.
- **Banco limpo**: PostgreSQL sem schema previo capaz de receber todas as migrations da linha atual.

## Casos de uso

1. O ambiente Compose fornece PostgreSQL e a API abre conexao usando configuracao externa.
2. Um banco vazio recebe migrations na ordem registrada.
3. Uma nova migration e gerada apenas para mudanca de modelo aprovada e pertencente ao modulo correto.
4. Uma entidade recebe UUID, timestamps UTC e `Version bigint` quando mutavel.
5. Constraint impede duplicidade de identificador e referencias invalidas.
6. Testes de integracao executam contra PostgreSQL real containerizado, nao apenas provider em memoria.

## Constraints iniciais

- Identificadores UUID sao `NOT NULL` e unicos quando representam chave primaria.
- Campos obrigatorios do dominio sao `NOT NULL`.
- Identificadores naturais sujeitos a unicidade possuem constraint ou indice unico no escopo correto.
- Foreign keys impedem referencias a projeto, board, card, usuario ou membership inexistentes quando o relacionamento for modelado.
- Relacionamentos com escopo de projeto impedem, no modelo escolhido, card apontar para board de outro projeto.
- `Version` e `NOT NULL`, `bigint` e nao diminui em update confirmado.
- Datas persistidas nao usam horario local implicito.
- Exclusao fisica ou cascade so e permitida quando a regra do contexto a definir explicitamente; nao e default para auditoria.

## Criterios de aceite

### Cenario: banco vazio

- **Dado** um PostgreSQL sem schema do produto
- **Quando** a estrategia documentada de migration e executada
- **Entao** todas as migrations da linha atual aplicam sem intervencao manual oculta
- **E** a API consegue abrir conexao apos a aplicacao

### Cenario: migration reproduzivel

- **Dado** um banco vazio e a mesma versao do repositorio
- **Quando** as migrations sao executadas duas vezes em ambientes isolados
- **Entao** ambos os schemas resultantes possuem a mesma estrutura esperada
- **E** a segunda execucao nao cria duplicidade nem depende de ordem manual

### Cenario: UTC

- **Dado** um timestamp criado ou atualizado pela aplicacao
- **Quando** o valor e persistido e lido novamente
- **Entao** ele representa UTC
- **E** nenhum horario local do host altera o instante armazenado

### Cenario: UUID

- **Dado** uma entidade persistivel com identificador publico
- **Quando** ela e criada
- **Entao** recebe UUID nao nulo e estavel
- **E** uma segunda entidade nao recebe o mesmo identificador

### Cenario: Version

- **Dado** um recurso mutavel com `Version` persistida
- **Quando** uma alteracao e confirmada
- **Entao** a versao aumenta uma vez de forma atomica
- **E** uma operacao rejeitada nao altera a versao

### Cenario: constraint

- **Dado** um payload que viola unicidade, nulidade ou foreign key modelada
- **Quando** a aplicacao tenta persistir o estado
- **Entao** a operacao e rejeitada de forma controlada
- **E** nao fica estado parcial no banco

### Cenario: PostgreSQL real

- **Dado** a suite de integracao em ambiente de teste
- **Quando** ela executa persistencia e migrations
- **Entao** usa PostgreSQL containerizado
- **E** nao depende de comportamento exclusivo de provider em memoria

### Cenario: ownership de persistencia

- **Dado** uma migration ou mapeamento de entidade
- **Quando** ele e revisado
- **Entao** pertence a um contexto identificado
- **E** nao cria ou altera estrutura de outro contexto sem decisao documentada

## Testes

- Teste de migration em banco vazio e upgrade a partir de um schema conhecido.
- Teste de conexao e health check contra PostgreSQL do Compose.
- Testes de constraints de unicidade, FK, nulidade e escopo.
- Testes de round trip UTC e UUID.
- Testes de concorrencia de `Version` no PostgreSQL real, incluindo nenhuma alteracao parcial.
- Verificacao de que migrations nao contem segredos ou connection strings.
- Teste de rollback ou, se rollback nao for suportado, validacao da estrategia forward-only documentada.

## Definition of Done

- [ ] EF Core e provider PostgreSQL estao definidos para o baseline.
- [ ] Ownership de DbContext, mapeamentos e migrations esta documentado.
- [ ] Banco vazio recebe migrations de forma reproduzivel.
- [ ] UTC, UUID e `Version bigint` possuem tipos e regras verificaveis.
- [ ] Constraints iniciais cobrem nulidade, unicidade, FK e pertencimento modelado.
- [ ] Integracao usa PostgreSQL containerizado.
- [ ] Estrategia de rollback ou forward-only esta registrada.
- [ ] Dados de teste e migrations nao contem secrets.
- [ ] Nenhuma tabela de feature nao modelada foi antecipada sem necessidade.

## Riscos

- Um DbContext unico pode reintroduzir acoplamento apesar dos nomes de modulos.
- Provider em memoria pode esconder divergencias de constraint e concorrencia.
- Gerar UUID em locais diferentes pode causar inconsistencias se a politica nao for fechada.
- Migration automatica no startup pode dificultar diagnostico e controle de deploy.
- Cascade delete mal definido pode apagar dados que deveriam ser auditaveis.

## Decisoes ainda abertas

- Um DbContext por contexto, um contexto de escrita composto ou outra composicao modular.
- Schema fisico unico versus schemas PostgreSQL por modulo.
- Geracao de UUID na aplicacao ou no banco.
- Valor inicial, estrategia de incremento e mapeamento EF do `Version`.
- Migrations no startup versus comando operacional explicito.
- Politica de delete para cada agregado e estrategia de arquivamento.
- Tabelas minimas exatas que serao criadas em T04 sem antecipar T06/T12/T15.
