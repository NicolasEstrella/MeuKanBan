# Contribuindo

## Fluxo de trabalho

1. Crie uma branch curta a partir de `main`.
2. Escolha uma task do backlog em `docs/03-backlog.md`.
3. Confirme dependencias e criterios de aceite antes de iniciar.
4. Mantenha mudancas pequenas e focadas na task.
5. Atualize testes e documentacao junto com o comportamento alterado.
6. Execute as validacoes da task antes de abrir a revisao.
7. Integre somente mudancas que preservem os limites do modular monolith.

## Principios tecnicos

- Modele regras no contexto dono; nao coloque invariantes de negocio em controllers.
- Use autorizacao resource-based e negacao por padrao.
- Nao atravesse o modulo acessando entidades, tabelas ou repositorios internos diretamente.
- Operacoes mutaveis devem respeitar `Version` e retornar `409 Conflict` em conflito.
- Auditoria deve ser append-only, minimizada e sem credenciais, tokens ou segredos.
- Mantenha o acesso ao PostgreSQL encapsulado pela infraestrutura e EF Core.
- Realtime, notificacoes e chat nao ampliam autorizacao nem substituem a API de escrita.

## Commits e pull requests

- Use mensagens objetivas e descreva o comportamento alterado.
- Inclua referencia a task e aos criterios de aceite cobertos.
- Descreva testes executados e eventuais lacunas.
- Nao inicialize outro repositorio dentro do monorepo e nao altere a branch principal diretamente.

## Decisoes abertas

Quando uma mudanca depender de uma decisao ainda adiada, registre a lacuna no pull request e atualize `docs/decisions/decision-log.md` antes de tratar a hipotese como requisito.
