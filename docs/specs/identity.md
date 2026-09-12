# Identity

## Tipo e objetivo

Feature de fundacao de identidade. O objetivo e permitir cadastro e sessao renovavel sem transformar credenciais ou tokens em acesso reutilizavel indevidamente.

## Contexto e problema

O cadastro aberto precisa permitir entrada simples no sistema sem transformar credenciais, sessões ou tokens em uma fonte de acesso indevido. O usuário deve conseguir autenticar-se e renovar a sessão sem expor credenciais reutilizáveis.

## Escopo

### Dentro

- Cadastro aberto com credenciais válidas.
- Login e emissão de access token JWT de curta duração.
- Refresh token rotativo, persistido somente como hash.
- Logout, revogação de sessão e detecção de reutilização de refresh token.
- Auditoria dos eventos de identidade e segurança relevantes.
- Papéis globais `GlobalAdmin`, `ProjectOwner`, `ProjectAdmin`, `Member` e `Viewer`, quando aplicáveis ao modelo de autorização.

### Fora

- Login social, SSO, MFA e recuperação de senha por e-mail.
- Gestão de identidade fora da aplicação.
- Definição das permissões detalhadas de cada projeto, tratada em `project-membership.md`.

## Decisoes e premissas

- O cadastro e aberto e o identificador de login e unico segundo a politica aprovada.
- A API e stateless; a sessao persistente necessaria para refresh e revogacao fica no PostgreSQL.
- Access JWT e curto; refresh token e rotativo e persistido somente como hash.
- Auditoria existe desde o inicio para cadastro, login, refresh, reuse, revogacao e logout.
- DDD explicito define ownership de identidade e sessao; BDD leve registra os comportamentos; TDD seletivo cobre credenciais e rotacao.

## Conceitos aprendidos

- **Identidade**: sujeito autenticavel, separado de membership em projeto.
- **Credencial protegida**: senha transformada por politica de hash, nunca material reutilizavel em texto puro.
- **Access token**: credencial curta para requisicoes, nao persistida como sessao reutilizavel.
- **Refresh token**: segredo de renovacao devolvido ao cliente, persistido apenas como hash.
- **Familia de refresh**: cadeia de tokens rotacionados que pode ser revogada por reuse.
- **Claim minima**: dado necessario para identificar o sujeito e validar o token, nao permissao completa.

## Casos de uso

1. Visitante cria identidade com credencial valida.
2. Identidade ativa faz login e recebe access JWT curto e refresh token.
3. Cliente renova a sessao uma vez usando refresh valido.
4. Sistema detecta reuse, revoga a familia e exige novo login.
5. Usuario encerra a sessao; repeticoes nao reativam a familia.
6. Auditoria registra o resultado sem armazenar material secreto.

## Regras de negócio

- E-mail ou identificador de login deve ser único segundo a política de identidade.
- Credenciais inválidas não criam sessão autenticada.
- O access token não é persistido como credencial reutilizável.
- O access token é curto, assinado por segredo fornecido pela configuração do ambiente e contém somente claims mínimas para identificar o sujeito, a sessão quando necessária, o emissor, a audiência, a emissão e a expiração.
- Claims de papel não substituem autorização resource-based; membership, recurso, projeto e estado continuam sendo avaliados pelo contexto dono.
- O refresh token em texto puro não é armazenado no banco nem escrito em logs.
- Segredos de assinatura, connection strings e credenciais de infraestrutura ficam fora do repositório; exemplos versionados contêm apenas placeholders.
- Refresh token usado é invalidado e substituído por novo token na mesma família.
- Reutilização de token já rotacionado revoga a família da sessão e exige novo login.
- Logout revoga a sessão aplicável e não pode reativá-la por repetição da requisição.
- A duração exata dos tokens é configuração de segurança; o access token deve ser curto e o refresh token deve possuir expiração absoluta e por inatividade.

## Critérios de aceitação

### Cenário: cadastro válido

- **Dado** que o identificador ainda não está cadastrado
- **Quando** o visitante envia credenciais que atendem à política vigente
- **Então** uma identidade é criada
- **E** a senha não é armazenada em texto puro
- **E** o evento de cadastro é auditado

### Cenário: login válido

- **Dado** que a identidade está ativa
- **Quando** o usuário informa credenciais válidas
- **Então** a API retorna um JWT de curta duração e um refresh token
- **E** o refresh token persistido é apenas seu hash
- **E** o login bem-sucedido é auditado

### Cenário: login inválido

- **Dado** que as credenciais não são válidas
- **Quando** o usuário tenta entrar
- **Então** a API rejeita a operação sem emitir tokens
- **E** não revela se o identificador existe
- **E** registra o resultado conforme a política de auditoria

### Cenário: refresh rotativo

- **Dado** que o refresh token está válido, não revogado e dentro da família correta
- **Quando** o usuário solicita renovação
- **Então** o token usado é invalidado
- **E** um novo access token e um novo refresh token são emitidos
- **E** apenas o hash do novo refresh token é persistido

### Cenário: reutilização de refresh token

- **Dado** que um refresh token já foi rotacionado
- **Quando** ele é apresentado novamente
- **Então** a família da sessão é revogada
- **E** nenhum novo token é emitido
- **E** o incidente é auditado

### Cenário: logout

- **Dado** que existe uma sessão ativa
- **Quando** o usuário solicita logout
- **Então** a sessão ou família aplicável é revogada
- **E** tentativas posteriores de refresh são rejeitadas

### Cenário: claims mínimas

- **Dado** um login válido
- **Quando** a API emite o access token
- **Então** o token contém apenas as claims necessárias para autenticação e roteamento de autorização
- **E** não contém senha, refresh token, hash de senha ou dados privados de projeto
- **E** a validade é curta e configurável

### Cenário: segredo fora do repositório

- **Dado** um ambiente sem arquivo de configuração versionado com valores reais
- **Quando** a API inicia
- **Então** obtém os segredos obrigatórios por ambiente ou mecanismo local de secrets
- **E** falha de forma explícita quando um segredo obrigatório está ausente
- **E** não imprime o valor do segredo em logs

### Cenário: revogação idempotente

- **Dado** uma sessão já revogada
- **Quando** o usuário repete logout ou uma operação de revogação equivalente
- **Então** a operação não reativa a sessão
- **E** refresh posterior continua rejeitado
- **E** o resultado auditável distingue revogação já existente de uma nova sessão criada

## Decomposição T05-T09

- **T05**: transformar cadastro, login válido e login inválido nos cenários BDD executáveis desta spec, incluindo senha protegida, emissão de tokens e resposta indistinguível para identificador desconhecido.
- **T06**: implementar cadastro aberto, identificador único, credencial protegida e auditoria do cadastro.
- **T07**: implementar login de identidade ativa, access JWT curto, refresh token e persistência somente do hash.
- **T08**: implementar rotação na mesma família, invalidar token usado e revogar a família ao detectar reuse.
- **T09**: implementar logout, revogação auditada e comportamento idempotente; refresh posterior deve continuar rejeitado.

Cada etapa preserva os critérios desta spec; nenhuma etapa pode introduzir armazenamento de refresh em texto puro ou segredos no repositório.

## Dependências e metodologia

- Depende do modelo de auditoria definido em `audit.md` e das regras de autorização de `project-membership.md`.
- O produto usa DDD explícito; comportamentos críticos usam BDD leve/Gherkin; TDD é aplicado seletivamente a rotação, revogação e exposição de credenciais.
- Stack de execução confirmada: Angular, ASP.NET Core, EF Core, PostgreSQL e containers.

## Testes

- Unidade para política de senha, claims mínimas, expiração, hash e transições de sessão.
- Integração com PostgreSQL para unicidade, rotação, reuse, revogação e atomicidade.
- API para cadastro, login, refresh e logout, cobrindo sucesso e rejeição sem enumeração.
- Testes de segurança verificam ausência de senha, access token, refresh token, hashes e secrets em banco, logs, auditoria, fixtures e respostas.
- Teste concorrente cobre duas tentativas de refresh com o mesmo token e confirma que reuse revoga a família.

## Definition of Done

- [ ] Cadastro aberto, login, refresh, reuse e logout possuem cenários BDD rastreáveis a T05-T09.
- [ ] Senha e refresh token nunca são persistidos em texto puro.
- [ ] Access JWT é curto, configurável e contém claims mínimas.
- [ ] Reutilização detectada revoga a família e não emite novo token.
- [ ] Logout e revogação são idempotentes e auditados.
- [ ] Segredos ficam fora do repositório e não aparecem em logs ou respostas.
- [ ] Testes cobrem sucesso, falha, concorrência e ausência de enumeração.
- [ ] Auditoria registra eventos relevantes sem dados sensíveis.

## Riscos

- Claims demais no JWT podem vazar dados ou criar autorização obsoleta.
- Rotação concorrente pode aceitar duas vezes o mesmo refresh se a transação não for atômica.
- Armazenamento inseguro no cliente pode expor refresh a XSS ou CSRF; a decisão depende da política de cliente.
- Duração inadequada de tokens pode aumentar exposição ou fricção de login.

## Decisões ainda abertas

- Duração exata do access token, refresh token e janela de inatividade.
- Local de armazenamento do refresh token no navegador e mitigação de XSS/CSRF.
- Algoritmo e parâmetros do hash de senha e do fingerprint/hash do refresh.
- Claims exatas além do conjunto mínimo obrigatório.
- Política de revogação de uma sessão versus toda a família em cada operação de logout.
- Confirmação de e-mail, recuperação de senha, MFA e login social continuam fora do escopo atual.
