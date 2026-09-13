using MeuKanBan.Application.Common;
using MeuKanBan.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MeuKanBan.Api.Errors;

/// <summary>
/// Limite global de excecoes: erros de contrato/dominio recebem status e codigo
/// estavel; falhas inesperadas viram 500 generico, com stack trace e detalhes
/// internos somente no log protegido, sem segredos na resposta
/// (docs/specs/api-contracts.md).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;
        switch (exception)
        {
            case DomainRuleException domainRule:
                logger.LogWarning("Regra de negocio rejeitou a operacao. RuleCode={RuleCode}", domainRule.RuleCode);
                problem = ProblemDetailsWriter.Create(context,
                    StatusCodes.Status422UnprocessableEntity,
                    "Regra de negocio violada",
                    "A operacao foi rejeitada por uma regra de negocio.");
                problem.Extensions["code"] = domainRule.RuleCode;
                break;
            case ConflictException:
                logger.LogWarning("Conflito de estado ao processar a operacao.");
                problem = ProblemDetailsWriter.Create(context,
                    StatusCodes.Status409Conflict,
                    "Conflito de estado",
                    "O estado atual do recurso conflita com a operacao solicitada. Recarregue e tente novamente.");
                break;
            case NotFoundException:
                problem = ProblemDetailsWriter.Create(context,
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    "O recurso solicitado nao foi encontrado.");
                break;
            case BadHttpRequestException:
                logger.LogWarning("Requisicao malformada rejeitada no limite da aplicacao.");
                problem = ProblemDetailsWriter.Create(context,
                    StatusCodes.Status400BadRequest,
                    "Requisicao invalida",
                    "A requisicao esta malformada.");
                break;
            default:
                // OBS-1 (FB-T03-01): a MENSAGEM da excecao pode carregar dados
                // sensiveis e NAO vai para o log. Registramos o tipo da excecao
                // e o stack trace (frames nao contem a mensagem); a excecao em
                // si nao e passada ao logger porque seu ToString() inclui a
                // mensagem bruta.
                logger.LogError(
                    "Falha inesperada ao processar a requisicao. ExceptionType={ExceptionType}\n{StackTrace}",
                    exception.GetType().FullName,
                    exception.StackTrace);
                problem = ProblemDetailsWriter.Create(context,
                    StatusCodes.Status500InternalServerError,
                    "Erro interno",
                    "Ocorreu um erro inesperado. Tente novamente mais tarde.");
                break;
        }

        await ProblemDetailsWriter.WriteAsync(context, problem, cancellationToken);
        return true;
    }
}
