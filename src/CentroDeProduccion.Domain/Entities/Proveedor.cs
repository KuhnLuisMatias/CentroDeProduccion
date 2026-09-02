using CentroDeProduccion.Domain.Services;
namespace CentroDeProduccion.Domain.Entities;

public class Proveedor
{
    public Guid Id { get; set; }
    public string NombreRazonSocial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? PersonaContacto { get; set; }
    public string? HorarioAtencion { get; set; }
    public string CategoriasProvee { get; set; } = string.Empty; // lista separada por coma
    public string TipoFactura { get; set; } = "A"; // A, B, C
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}
