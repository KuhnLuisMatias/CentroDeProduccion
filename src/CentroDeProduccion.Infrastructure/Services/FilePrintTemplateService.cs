using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CentroDeProduccion.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace CentroDeProduccion.Infrastructure.Services;

/// <summary>
/// Loads an HTML print template from <c>wwwroot/templates/print/{templateName}.html</c> and
/// resolves <c>{{Property.Nested}}</c> placeholders against the supplied model via reflection.
/// Throws <see cref="InvalidOperationException"/> if the template file is missing or a
/// placeholder cannot be resolved.
/// </summary>
public partial class FilePrintTemplateService : IPrintTemplateService
{
    private readonly string _templatesRoot;

    public FilePrintTemplateService(IWebHostEnvironment environment)
    {
        var webRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        _templatesRoot = Path.Combine(webRoot, "templates", "print");
    }

    public string Render<T>(T model, string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("Template name must not be empty", nameof(templateName));
        }

        var filePath = Path.Combine(_templatesRoot, templateName + ".html");
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException(
                $"Print template '{templateName}' not found at '{filePath}'.");
        }

        var template = File.ReadAllText(filePath);

        return PlaceholderRegex().Replace(template, match =>
        {
            var propertyPath = match.Groups[1].Value;
            var value = ResolveValue(model, propertyPath);
            return value ?? string.Empty;
        });
    }

    private static string? ResolveValue(object? model, string propertyPath)
    {
        if (model == null)
        {
            return null;
        }

        object? current = model;
        foreach (var segment in propertyPath.Split('.'))
        {
            if (current == null)
            {
                return null;
            }

            var property = current.GetType().GetProperty(segment,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Print template placeholder '{propertyPath}' cannot be resolved: type " +
                    $"'{current.GetType().Name}' has no property '{segment}'.");
            }

            current = property.GetValue(current);
        }

        return current?.ToString();
    }

    [GeneratedRegex(@"\{\{\s*([\w.]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();
}
