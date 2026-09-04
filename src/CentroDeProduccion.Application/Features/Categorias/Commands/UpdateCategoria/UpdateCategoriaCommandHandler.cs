using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Categorias.Commands.UpdateCategoria;

public class UpdateCategoriaCommandHandler
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateCategoriaCommand> _validator;

    public UpdateCategoriaCommandHandler(
        ICategoriaRepository categoriaRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateCategoriaCommand> validator)
    {
        _categoriaRepository = categoriaRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateCategoriaCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var categoria = await _categoriaRepository.GetByIdAsync(command.Id, cancellationToken);
        if (categoria == null)
        {
            return Result.Failure(Error.NotFound("CATEGORIA_NOT_FOUND", "Categoría no encontrada"));
        }

        if (await _categoriaRepository.ExistsWithNameInAmbitoAsync(command.Nombre, command.Ambito, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("CATEGORIA_ALREADY_EXISTS", "Ya existe otra categoría con ese nombre en ese ámbito"));
        }

        categoria.Nombre = command.Nombre;
        categoria.Ambito = command.Ambito;
        categoria.Activo = command.Activo;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
