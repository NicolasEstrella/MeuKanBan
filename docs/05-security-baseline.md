# Security Baseline

## Identidade e sessoes

- Cadastro aberto com identificador unico e senha protegida por hash forte.
- Access JWT curto e nao persistido como credencial reutilizavel.
- Refresh token rotativo, com somente hash persistido.
- Reutilizacao de token rotacionado revoga a familia e exige novo login.
- Logout revoga a sessao/familia aplicavel.
- Duracoes exatas e armazenamento no cliente permanecem decisoes adiadas.

## Autorizacao

- Acesso negado por padrao.
- Toda decisao considera usuario, acao, recurso, projeto, membership ativo, papeis e estado.
- Projetos sao privados por padrao.
- `GlobalAdmin` nao acessa automaticamente conteudo privado.
- Membership revogado nao autoriza consultas, comandos ou grupos realtime.
- A API resolve o recurso e seu projeto antes de autorizar quando necessario.

## Dados sensiveis

Nao armazenar ou registrar senha em texto puro, access token, refresh token em texto puro, hash de senha, segredos de assinatura ou connection strings reais. Payloads de auditoria devem ser minimizados.

## Concorrencia e auditoria

Toda alteracao mutavel relevante exige `Version` esperada. Divergencia retorna `409 Conflict`, nao faz retry cego e nao persiste update parcial. Auditoria e append-only, escopada e registra sucesso, rejeicao relevante e conflito.

## Operacao local

Segredos locais ficam fora do commit, preferencialmente por mecanismo de secrets/env. PostgreSQL local do Compose usa credencial apenas para desenvolvimento. Logs devem ser estruturados e nao conter credenciais.

## Checklist antes de liberar uma fatia

- [ ] Cenarios permitido e negado testados.
- [ ] Acesso cruzado entre projetos coberto.
- [ ] Membership inativo coberto.
- [ ] Dados sensiveis ausentes em respostas e logs.
- [ ] Auditoria confirma ator, acao, recurso, projeto, resultado e correlation id quando disponivel.
- [ ] Falhas de autenticacao/autorizacao nao permitem enumeracao indevida.
- [ ] Dependencias e imagens containerizadas possuem versao controlada.
