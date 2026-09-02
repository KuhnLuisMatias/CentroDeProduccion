using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Categorias.Commands.DeactivateCategoria;

public class DeactivateCategoriaCommandHandler
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateCategoriaCommandHandler(
        ICategoriaRepository categoriaRepository,
        IInsumoRepository insumoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IUnitOfWork unitOfWork)
    {
        _categoriaRepository = categoriaRepository;
        _insumoRepository = insumoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeactivateCategoriaCommand command, CancellationToken cancellationToken = default)
    {
        var categoria = await _categoriaRepository.GetByIdAsync(command.Id, cancellationToken);
        if (categoria == null)
        {
            return Result.Failure(Error.NotFound("CATEGORIA_NOT_FOUND", "Categoría no encontrada"));
        }

        var usadaPorInsumos = await _insumoRepository.ExistsActiveWithCategoriaAsync(categoria.Id, cancellationToken);
        var usadaPorProductos = await _productoTerminadoRepository.ExistsActiveWithCategoriaAsync(categoria.Id, cancellationToken);

        if (usadaPorInsumos || usadaPorProductos)
        {
            return Result.Failure(Error.Conflict("CATEGORIA_EN_USO", "La categoría está siendo usada por uno o más registros"));
        }

        categoria.Activo = false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
