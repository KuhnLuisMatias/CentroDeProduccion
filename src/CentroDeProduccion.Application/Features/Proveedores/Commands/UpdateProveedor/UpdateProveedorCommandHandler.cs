using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Proveedores.Commands.UpdateProveedor;

public class UpdateProveedorCommandHandler
{
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateProveedorCommand> _validator;

    public UpdateProveedorCommandHandler(
        IProveedorRepository proveedorRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateProveedorCommand> validator)
    {
        _proveedorRepository = proveedorRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateProveedorCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var proveedor = await _proveedorRepository.GetByIdAsync(command.Id, cancellationToken);
        if (proveedor == null)
        {
            return Result.Failure(Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado"));
        }

        if (await _proveedorRepository.ExistsWithCuitAsync(command.Cuit, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("CUIT_ALREADY_EXISTS", "Ya existe otro proveedor con ese CUIT"));
        }

        proveedor.NombreRazonSocial = command.NombreRazonSocial;
        proveedor.Cuit = command.Cuit;
        proveedor.Direccion = command.Direccion;
        proveedor.Telefono = command.Telefono;
        proveedor.WhatsApp = command.WhatsApp;
        proveedor.Email = command.Email;
        proveedor.PersonaContacto = command.PersonaContacto;
        proveedor.HorarioAtencion = command.HorarioAtencion;
        proveedor.CategoriasProvee = command.CategoriasProvee;
        proveedor.TipoFactura = command.TipoFactura;
        proveedor.Observaciones = command.Observaciones;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
