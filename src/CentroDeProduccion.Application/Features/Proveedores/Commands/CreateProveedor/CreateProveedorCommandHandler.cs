using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Proveedores.Commands.CreateProveedor;

public class CreateProveedorCommandHandler
{
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateProveedorCommand> _validator;

    public CreateProveedorCommandHandler(
        IProveedorRepository proveedorRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateProveedorCommand> validator)
    {
        _proveedorRepository = proveedorRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CreateProveedorResponse>> HandleAsync(CreateProveedorCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateProveedorResponse>(errors.First());
        }

        if (await _proveedorRepository.ExistsWithCuitAsync(command.Cuit, null, cancellationToken))
        {
            return Result.Failure<CreateProveedorResponse>(
                Error.Conflict("CUIT_ALREADY_EXISTS", "Ya existe un proveedor con ese CUIT"));
        }

        var proveedor = new Proveedor
        {
            Id = Guid.NewGuid(),
            NombreRazonSocial = command.NombreRazonSocial,
            Cuit = command.Cuit,
            Direccion = command.Direccion,
            Telefono = command.Telefono,
            WhatsApp = command.WhatsApp,
            Email = command.Email,
            PersonaContacto = command.PersonaContacto,
            HorarioAtencion = command.HorarioAtencion,
            CategoriasProvee = command.CategoriasProvee,
            TipoFactura = command.TipoFactura,
            Observaciones = command.Observaciones,
            Activo = true,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        await _proveedorRepository.AddAsync(proveedor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProveedorResponse(
            proveedor.Id,
            proveedor.NombreRazonSocial,
            proveedor.Cuit,
            proveedor.Direccion,
            proveedor.Telefono,
            proveedor.WhatsApp,
            proveedor.Email,
            proveedor.PersonaContacto,
            proveedor.HorarioAtencion,
            proveedor.CategoriasProvee,
            proveedor.TipoFactura,
            proveedor.Observaciones);
    }
}
