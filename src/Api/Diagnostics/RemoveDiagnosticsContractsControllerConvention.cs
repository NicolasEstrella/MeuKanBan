using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MeuKanBan.Api.Diagnostics;

/// <summary>
/// Remove o controlador de diagnostico do pipeline quando os test hooks de
/// contrato estao desabilitados (padrao, inclusive producao).
/// </summary>
public sealed class RemoveDiagnosticsContractsControllerConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var controller = application.Controllers.FirstOrDefault(
            c => c.ControllerType == typeof(DiagnosticsContractsController));
        if (controller is not null)
        {
            application.Controllers.Remove(controller);
        }
    }
}
