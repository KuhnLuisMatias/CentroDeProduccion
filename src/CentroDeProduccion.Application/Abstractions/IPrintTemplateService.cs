namespace CentroDeProduccion.Application.Abstractions;

/// <summary>
/// Renders an HTML print template (remito, orden de carga, etc.) by loading the template file
/// from <c>wwwroot/templates/print</c> and resolving <c>{{Property.Nested}}</c> placeholders
/// against the supplied model.
/// </summary>
public interface IPrintTemplateService
{
    string Render<T>(T model, string templateName);
}
