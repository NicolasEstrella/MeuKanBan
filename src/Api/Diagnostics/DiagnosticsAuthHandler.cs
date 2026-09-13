using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MeuKanBan.Api.Diagnostics;

/// <summary>
/// Esquema de autenticacao exclusivo dos test hooks de contrato: autentica via
/// header X-Test-Identity sem conceder claims de permissao. Registrado apenas
/// quando Diagnostics:ContractsTestHooks:Enabled=true (testes de API); nunca
/// em producao. A autenticacao real chega em T07.
/// </summary>
public sealed class DiagnosticsAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Diagnostics";
    public const string IdentityHeader = "X-Test-Identity";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = Request.Headers[IdentityHeader].ToString();
        if (string.IsNullOrWhiteSpace(identity) || identity.Length > 64)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, identity) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
