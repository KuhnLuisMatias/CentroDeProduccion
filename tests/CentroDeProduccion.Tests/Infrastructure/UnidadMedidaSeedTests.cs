using CentroDeProduccion.Infrastructure.Data;
using CentroDeProduccion.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CentroDeProduccion.Tests.Infrastructure;

/// <summary>
/// Seed behaviour for Slice 2, against a real SQL Server engine so the insert and the
/// idempotency guard are exercised the way production runs them.
/// </summary>
[Trait("Category", "SqlServer")]
public class UnidadMedidaSeedTests : IAsyncLifetime
{
    private const int BaselineUnitCount = 15;

    private AppDbContext _db = null!;

    public async Task InitializeAsync() => _db = await LocalDb.CreateAsync(nameof(UnidadMedidaSeedTests));

    public async Task DisposeAsync() => await LocalDb.DropAsync(_db);

    [Fact]
    public async Task OnEmptyDatabase_InsertsBaselineRows()
    {
        await UnidadMedidaSeed.SeedAsync(_db);

        var count = await _db.UnidadesMedida.CountAsync();
        count.ShouldBe(BaselineUnitCount);
    }

    [Fact]
    public async Task WhenRowsAlreadyExist_IsIdempotent()
    {
        await UnidadMedidaSeed.SeedAsync(_db);
        await UnidadMedidaSeed.SeedAsync(_db);

        var count = await _db.UnidadesMedida.CountAsync();
        count.ShouldBe(BaselineUnitCount);
    }
}
