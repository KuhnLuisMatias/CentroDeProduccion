using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Categorias.Commands.CreateCategoria;

public class CreateCategoriaCommandHandler
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoriaCommand> _validator;

    public CreateCategoriaCommandHandler(
        ICategoriaRepository categoriaRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateCategoriaCommand> validator)
    {
        _categoriaRepository = categoriaRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CreateCategoriaResponse>> HandleAsync(CreateCategoriaCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateCategoriaResponse>(errors.First());
        }

        if (await _categoriaRepository.ExistsWithNameInAmbitoAsync(command.Nombre, command.Ambito, null, cancellationToken))
        {
            return Result.Failure<CreateCategoriaResponse>(
                Error.Conflict("CATEGORIA_ALREADY_EXISTS", "Ya existe una categoría con ese nombre en ese ámbito"));
        }

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            Ambito = command.Ambito,
            Activo = true
        };

        await _categoriaRepository.AddAsync(categoria, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateCategoriaResponse(categoria.Id, categoria.Nombre, categoria.Ambito);
    }
}
