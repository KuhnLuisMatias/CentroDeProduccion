using Shouldly;

namespace CentroDeProduccion.Tests;

/// <summary>
/// Verifies the deliverables promised by the "Pulido y cierre" phase exist at the repo root:
/// the PWA shell files under wwwroot, the README, and the user documentation.
/// </summary>
public class PresenceTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CentroDeProduccion.slnx")))
        {
            dir = dir.Parent;
        }

        dir.ShouldNotBeNull("no se encontró CentroDeProduccion.slnx subiendo desde el directorio actual");
        return dir!.FullName;
    }

    private static string WwwRoot => Path.Combine(RepoRoot(), "src", "CentroDeProduccion.Api", "wwwroot");

    [Fact]
    public void ManifestJson_ExisteEnWwwRoot()
    {
        File.Exists(Path.Combine(WwwRoot, "manifest.json")).ShouldBeTrue();
    }

    [Fact]
    public void ServiceWorker_ExisteEnWwwRoot()
    {
        File.Exists(Path.Combine(WwwRoot, "sw.js")).ShouldBeTrue();
    }

    [Fact]
    public void Readme_ExisteEnLaRaizDelRepositorio()
    {
        File.Exists(Path.Combine(RepoRoot(), "README.md")).ShouldBeTrue();
    }

    [Fact]
    public void DocumentacionDeUsuario_Existe()
    {
        var docs = Path.Combine(RepoRoot(), "docs");
        File.Exists(Path.Combine(docs, "user", "guia-inventario.md")).ShouldBeTrue();
        File.Exists(Path.Combine(docs, "onboarding.md")).ShouldBeTrue();
        File.Exists(Path.Combine(docs, "capacitacion.md")).ShouldBeTrue();
    }
}
