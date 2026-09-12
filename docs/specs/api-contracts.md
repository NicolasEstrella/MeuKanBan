# T03 - Contratos HTTP, erros e configuracao

## Tipo e objetivo

Feature de contrato transversal. O objetivo e tornar erros da API previsiveis, rastreaveis e seguros, com Problem Details, correlation id, validacao, configuracao externa e tratamento explicito dos status relevantes.

A necessidade real e permitir que Angular, testes e operacao distingam entrada invalida, falta de autenticacao, falta de permissao, recurso ausente, conflito, regra de negocio e falha inesperada sem depender de mensagens ad hoc.

## Escopo

### Dentro

- Formato Problem Details para erros HTTP.
- Correlation id recebido, gerado, propagado e devolvido quando permitido.
- Validacao de entrada e separacao de erro de contrato de erro de dominio.
- Configuracao tipada por ambiente e secrets fora do repositorio.
- Contratos para `400`, `401`, `403`, `404`, `409`, `422` e `500`.
- Correlation id disponivel para logs e auditoria quando aplicavel.

### Fora

- Implementacao de endpoints de cadastro, projetos ou Kanban.
- Definicao completa de autorizacao por papel.
- Observabilidade avancada, metricas e tracing distribuido.
- Formato de sucesso especifico de cada feature.
- Exposicao de stack trace ou detalhes internos ao cliente.

## Decisoes e premissas

- Erros HTTP usam `application/problem+json` quando houver corpo.
- Cada erro tem `type`, `title`, `status`, `detail` seguro e `instance` ou identificador equivalente conforme convencao adotada.
- Um campo de extensao `correlationId` e permitido e deve ser estavel entre resposta, log e auditoria da requisicao.
- O cliente pode enviar um correlation id valido; na ausencia, a API gera um. Valores invalidos nao devem permitir injecao em logs.
- `401` significa identidade ausente ou credencial invalida; `403` significa identidade conhecida sem permissao.
- `404` nao deve revelar existencia de recurso privado quando a politica de anti-enumeracao exigir resposta indistinguivel.
- `409` representa conflito de estado/concorrencia; `422` representa entrada semanticamente valida no formato, mas rejeitada por regra de negocio.
- `500` nao inclui stack trace, segredo ou detalhes de infraestrutura.
- DDD explicito, BDD leve e TDD seletivo permanecem a metodologia registrada.

## Conceitos aprendidos

- **Problem Details**: contrato de erro legivel por maquina.
- **Correlation ID**: identificador de uma requisicao e de seus registros relacionados.
- **Erro de contrato**: payload ausente, formato invalido ou campo estrutural incorreto.
- **Erro de autorizacao**: ausencia de identidade ou permissao.
- **Conflito**: estado esperado diverge do estado atual, sem update parcial.
- **Erro de dominio**: regra de negocio rejeita uma operacao semanticamente compreensivel.
- **Secret**: valor sensivel fornecido pelo ambiente, nunca por configuracao versionada.

## Casos de uso

1. Um cliente envia uma requisicao invalida e recebe erro estruturado sem excecao interna.
2. Uma requisicao autenticada carrega o mesmo correlation id na resposta e logs correlatos.
3. Um comando mutavel encontra versao divergente e recebe `409` com dados seguros para recuperacao.
4. Uma regra de negocio rejeita uma operacao valida estruturalmente e recebe `422`.
5. Uma excecao nao tratada e convertida em `500`, com detalhes internos somente no log protegido.
6. A API inicia sem segredos versionados, obtendo configuracao de ambiente/secrets.

## Contrato de status

| Status | Uso | Conteudo minimo observavel |
|---|---|---|
| 400 | Requisicao malformada ou validacao estrutural | Problem Details e campos invalidos sem segredo |
| 401 | Ausencia ou invalidade de autenticacao | Problem Details generico; sem enumerar identidade |
| 403 | Identidade autenticada sem permissao | Problem Details sem dados do recurso privado |
| 404 | Recurso inexistente ou ocultado por anti-enumeracao | Problem Details seguro |
| 409 | Conflito de versao/estado ou unicidade concorrente | Problem Details, correlation id e dados seguros de recuperacao |
| 422 | Regra de negocio rejeita payload estruturalmente valido | Problem Details com codigo de regra estavel |
| 500 | Falha inesperada | Problem Details generico; stack trace apenas no log protegido |

## Criterios de aceite BDD

### Cenario: erro estrutural

- **Dado** um payload ausente ou com formato invalido
- **Quando** a API recebe a requisicao
- **Entao** responde `400` com `application/problem+json`
- **E** informa campos invalidos sem incluir segredo ou stack trace

### Cenario: autenticacao ausente

- **Dado** um endpoint protegido e nenhuma credencial valida
- **Quando** o cliente faz a requisicao
- **Entao** recebe `401`
- **E** o corpo nao confirma se um identificador especifico existe

### Cenario: autorizacao negada

- **Dado** um usuario autenticado sem permissao no recurso
- **Quando** tenta a operacao
- **Entao** recebe `403` ou `404` conforme a politica de anti-enumeracao do recurso
- **E** nao recebe dados privados do recurso

### Cenario: conflito

- **Dado** um comando com estado ou versao esperada incompatível com o estado atual
- **Quando** o comando e processado
- **Entao** recebe `409`
- **E** o response possui correlation id
- **E** nenhuma alteracao parcial e persistida

### Cenario: regra de negocio

- **Dado** um payload estruturalmente valido que viola uma regra de dominio
- **Quando** a API processa o comando
- **Entao** recebe `422`
- **E** o erro possui codigo de regra estavel para o cliente e os testes

### Cenario: correlation id

- **Dado** um correlation id valido enviado pelo cliente
- **Quando** a API conclui com sucesso ou erro controlado
- **Entao** o mesmo id aparece na resposta, logs correlatos e auditoria quando houver evento auditavel

### Cenario: falha inesperada

- **Dado** uma excecao nao tratada durante uma requisicao
- **Quando** a API a captura no limite global
- **Entao** responde `500` com detalhe generico
- **E** registra diagnostico interno correlacionado sem senha, token ou connection string

### Cenario: segredo ausente

- **Dado** que um secret obrigatorio nao foi fornecido pelo ambiente
- **Quando** a API inicia
- **Entao** falha de forma explicita e diagnostica qual configuracao esta ausente sem imprimir seu valor
- **E** nenhum secret e obtido de arquivo versionado

## Testes

- Testes de API para cada status e content type.
- Teste de round trip do correlation id fornecido e gerado.
- Teste de sanitizacao contra caracteres de log e valores sensiveis.
- Teste de excecao inesperada com resposta generica e log correlacionado.
- Teste de configuracao: secret presente, ausente e placeholder proibido.
- Teste de autorizacao verifica que 403/404 nao vazam existencia de projeto privado.

## Definition of Done

- [ ] Problem Details e extensoes estao documentados com exemplos seguros.
- [ ] Correlation id tem origem, formato, propagacao e comportamento de ausencia definidos.
- [ ] Os sete status possuem uso e teste objetivo.
- [ ] Validacao separa erro estrutural, autorizacao, conflito e regra de negocio.
- [ ] Falhas 500 nao expõem stack trace ou segredos.
- [ ] Configuracao obrigatoria vem do ambiente/secrets e nao do Git.
- [ ] Auditoria pode associar evento a correlation id quando disponivel.
- [ ] Cenarios BDD estao prontos para virar testes de API.
- [ ] A topologia de runtime permanece limitada aos componentes aprovados para o produto.

## Riscos

- Usar `detail` como contrato textual fragil pode quebrar clientes; codigo de erro deve ser estavel.
- Responder sempre `404` ou sempre `403` pode conflitar com a politica de anti-enumeracao de cada recurso.
- Correlation id controlado pelo cliente pode contaminar logs se nao houver validacao.
- Configuracao divergente entre desenvolvimento, testes e producao pode mascarar falhas.

## Decisoes ainda abertas

- Padrao final para `type`, `instance` e nomes de extensoes.
- Formato e limite do correlation id.
- Lista de codigos de erro de dominio por contexto.
- Quais recursos usam `404` para ocultar existencia em vez de `403`.
- Catalogo de secrets obrigatorios e mecanismo de validacao de startup.
