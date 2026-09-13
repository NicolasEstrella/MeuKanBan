using System.ComponentModel.DataAnnotations;
using MeuKanBan.Application.Common;
using MeuKanBan.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuKanBan.Api.Diagnostics;

/// <summary>
/// Endpoints de DIAGNOSTICO dos contratos de erro (T03). Nao sao endpoints de
/// negocio: so existem quando Diagnostics:ContractsTestHooks:Enabled=true
/// (testes de API); fora desse modo o controlador e removido do pipeline por
/// convencao e o esquema de autenticacao de teste nao e registrado.
/// </summary>
[ApiController]
[Route("diagnostics/contracts")]
public sealed class DiagnosticsContractsController : ControllerBase
{
    public const string ProbePolicy = "ContractsProbe";

    /// <summary>Demonstra 400 por validacao estrutural e por JSON malformado.</summary>
    [HttpPost("echo")]
    public IActionResult Echo(EchoRequest request) => Ok(new { request.Name });

    /// <summary>Demonstra 401 quando nao ha credencial valida.</summary>
    [HttpGet("authorized")]
    [Authorize]
    public IActionResult Authorized() => Ok(new { ok = true });

    /// <summary>Demonstra 403 para identidade autenticada sem permissao.</summary>
    [HttpGet("forbidden")]
    [Authorize(Policy = ProbePolicy)]
    public IActionResult Forbidden() => Ok(new { ok = true });

    /// <summary>Demonstra 404 por recurso inexistente.</summary>
    [HttpGet("not-found")]
    public IActionResult NotFoundProbe() =>
        throw new NotFoundException("Recurso de diagnostico inexistente.");

    /// <summary>Demonstra 409 por conflito de estado/versao.</summary>
    [HttpGet("conflict")]
    public IActionResult ConflictProbe() =>
        throw new ConflictException("Versao divergente no recurso de diagnostico.");

    /// <summary>Demonstra 422 com codigo de regra estavel.</summary>
    [HttpGet("business-rule")]
    public IActionResult BusinessRule() =>
        throw new DomainRuleException("diagnostics.sample-rule", "Regra de exemplo violada.");

    /// <summary>Demonstra 500 generico sem vazamento de detalhes internos.</summary>
    [HttpGet("unexpected")]
    public IActionResult Unexpected() =>
        throw new InvalidOperationException("Falha interna com dado sensivel simulado: SuperSecret123.");

    public sealed class EchoRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string? Name { get; set; }
    }
}
