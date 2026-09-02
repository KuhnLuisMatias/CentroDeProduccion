using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Data.Seed;

/// <summary>
/// Seeds the baseline set of <see cref="UnidadMedida"/> reference rows. Idempotent and additive:
/// only units whose deterministic id is not already present are inserted, so re-running on a
/// populated database adds missing rows without duplicating existing ones.
/// </summary>
public static class UnidadMedidaSeed
{
    // Deterministic ids so FK references in future seeds/migrations are stable across environments.
    public static readonly Guid KilogramoId = new("11111111-1111-1111-1111-111111111101");
    public static readonly Guid GramoId = new("11111111-1111-1111-1111-111111111102");
    public static readonly Guid LitroId = new("11111111-1111-1111-1111-111111111103");
    public static readonly Guid MililitroId = new("11111111-1111-1111-1111-111111111104");
    public static readonly Guid UnidadId = new("11111111-1111-1111-1111-111111111105");
    public static readonly Guid DocenaId = new("11111111-1111-1111-1111-111111111106");
    public static readonly Guid CajaId = new("11111111-1111-1111-1111-111111111107");
    public static readonly Guid BidonId = new("11111111-1111-1111-1111-111111111108");
    public static readonly Guid SachetId = new("11111111-1111-1111-1111-111111111109");
    public static readonly Guid PaqueteId = new("11111111-1111-1111-1111-111111111110");
    public static readonly Guid LataId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid BotellaId = new("11111111-1111-1111-1111-111111111112");
    public static readonly Guid PorcionId = new("11111111-1111-1111-1111-111111111113");
    public static readonly Guid PouchId = new("11111111-1111-1111-1111-111111111114");
    public static readonly Guid PilonId = new("11111111-1111-1111-1111-111111111115");

    private static readonly UnidadMedida[] Todas =
    {
        new() { Id = KilogramoId, Nombre = "Kilogramo", Simbolo = "kg", Tipo = TipoUnidadMedida.Masa },
        new() { Id = GramoId, Nombre = "Gramo", Simbolo = "g", Tipo = TipoUnidadMedida.Masa },
        new() { Id = LitroId, Nombre = "Litro", Simbolo = "L", Tipo = TipoUnidadMedida.Volumen },
        new() { Id = MililitroId, Nombre = "Mililitro", Simbolo = "mL", Tipo = TipoUnidadMedida.Volumen },
        new() { Id = UnidadId, Nombre = "Unidad", Simbolo = "Uni", Tipo = TipoUnidadMedida.Conteo },
        new() { Id = DocenaId, Nombre = "Docena", Simbolo = "Doc", Tipo = TipoUnidadMedida.Conteo },
        new() { Id = CajaId, Nombre = "Caja", Simbolo = "Cj", Tipo = TipoUnidadMedida.Conteo },
        new() { Id = BidonId, Nombre = "Bidon", Simbolo = "Bid", Tipo = TipoUnidadMedida.Volumen },
        new() { Id = SachetId, Nombre = "Sachet", Simbolo = "Sachet", Tipo = TipoUnidadMedida.Conteo },
        new() { Id = PaqueteId, Nombre = "Paquete", Simbolo = "Paq", Tipo = TipoUnidadMedida.Conteo },
        new() { Id = LataId, Nombre = "Lata", Simbolo = "Lata", Tipo = TipoUnidadMedida.Conteo },
        new() { Id = BotellaId, Nombre = "Botella", Simbolo = "Bot", Tipo = TipoUnidadMedida.Volumen },
        new() { Id = PorcionId, Nombre = "Porción", Simbolo = "Prc", Tipo = TipoUnidadMedida.Conteo },
        new() { Id = PouchId, Nombre = "Pouch", Simbolo = "Pouch", Tipo = TipoUnidadMedida.Conteo },
        new() { Id = PilonId, Nombre = "Pilon", Simbolo = "Pilon", Tipo = TipoUnidadMedida.Conteo }
    };

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var existingIds = await db.UnidadesMedida.Select(u => u.Id).ToHashSetAsync(cancellationToken);
        var faltantes = Todas.Where(u => !existingIds.Contains(u.Id)).ToList();

        if (faltantes.Count == 0)
        {
            return;
        }

        db.UnidadesMedida.AddRange(faltantes);
        await db.SaveChangesAsync(cancellationToken);
    }
}
