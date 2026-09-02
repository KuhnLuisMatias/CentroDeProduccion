using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Shouldly;

namespace CentroDeProduccion.Tests.Infrastructure;

/// <summary>
/// Model-shape checks for Slice 2 (design D1, D6, D8). These read the EF Core model only, so
/// they open no connection and need no database engine — they run anywhere the SDK is installed.
/// </summary>
public class SchemaModelTests : IDisposable
{
    private readonly AppDbContext _db = new(LocalDb.ModelOnlyOptions());

    public void Dispose() => _db.Dispose();

    [Fact]
    public void TipoMovimientoStock_HasExactlyTenValues_TransferenciaRemoved()
    {
        var values = Enum.GetValues<TipoMovimientoStock>();

        values.Length.ShouldBe(10);
        values.ShouldNotContain(v => v.ToString() == "Transferencia");
    }

    [Fact]
    public void Categoria_UniqueIndex_IsScopedToAmbitoAndNombre()
    {
        var entityType = _db.Model.FindEntityType(typeof(Categoria))!;
        var uniqueIndex = entityType.GetIndexes().Single(i => i.IsUnique);

        uniqueIndex.Properties.Select(p => p.Name).ShouldBe(new[] { "Ambito", "Nombre" });
    }
}
