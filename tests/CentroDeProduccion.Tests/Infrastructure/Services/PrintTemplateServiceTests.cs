using CentroDeProduccion.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Infrastructure.Services;

/// <summary>
/// Verifies <see cref="FilePrintTemplateService"/> resolves {{Property}} and {{Prop.Nested}}
/// placeholders against the model via reflection, and fails loudly when a placeholder cannot be
/// resolved or the template file is missing.
/// </summary>
public class PrintTemplateServiceTests
{
    private sealed class Modelo
    {
        public string Nombre { get; set; } = string.Empty;
        public Cliente Cliente { get; set; } = new();
    }

    private sealed class Cliente
    {
        public string RazonSocial { get; set; } = string.Empty;
    }

    private static FilePrintTemplateService CrearServicio(string webRoot, out string templatesRoot)
    {
        templatesRoot = Path.Combine(webRoot, "templates", "print");
        Directory.CreateDirectory(templatesRoot);
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.WebRootPath.Returns(webRoot);
        environment.ContentRootPath.Returns(webRoot);
        return new FilePrintTemplateService(environment);
    }

    private static string TempRoot() => Path.Combine(Path.GetTempPath(), "cdp-tpl-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Render_PlaceholderSimple_ResuelveContraElModelo()
    {
        var root = TempRoot();
        var service = CrearServicio(root, out var templatesRoot);
        File.WriteAllText(Path.Combine(templatesRoot, "test-simple.html"), "Hola {{Nombre}}");

        var html = service.Render(new Modelo { Nombre = "Mundo" }, "test-simple");

        html.ShouldBe("Hola Mundo");
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Render_PlaceholderAnidado_ResuelvePropiedadNested()
    {
        var root = TempRoot();
        var service = CrearServicio(root, out var templatesRoot);
        File.WriteAllText(Path.Combine(templatesRoot, "test-nested.html"), "Cliente: {{Cliente.RazonSocial}}");

        var html = service.Render(new Modelo { Cliente = new Cliente { RazonSocial = "ACME SA" } }, "test-nested");

        html.ShouldBe("Cliente: ACME SA");
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Render_PlaceholderDesconocido_LanzaInvalidOperationException()
    {
        var root = TempRoot();
        var service = CrearServicio(root, out var templatesRoot);
        File.WriteAllText(Path.Combine(templatesRoot, "test-unknown.html"), "{{NoExiste}}");

        var ex = Should.Throw<InvalidOperationException>(() => service.Render(new Modelo(), "test-unknown"));

        ex.Message.ShouldContain("NoExiste");
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Render_TemplateInexistente_LanzaInvalidOperationException()
    {
        var root = TempRoot();
        var service = CrearServicio(root, out _);

        var ex = Should.Throw<InvalidOperationException>(() => service.Render(new Modelo(), "no-existe"));

        ex.Message.ShouldContain("no-existe");
        Directory.Delete(root, recursive: true);
    }
}
