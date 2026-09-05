using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;
using CentroDeProduccion.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CentroDeProduccion.Application.Features.Recetas.Commands.UpdateReceta;

public class UpdateRecetaCommandHandler
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateRecetaCommand> _validator;
    private readonly ILogger<UpdateRecetaCommandHandler> _logger;

    public UpdateRecetaCommandHandler(
        IRecetaRepository recetaRepository,
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateRecetaCommand> validator,
        ILogger<UpdateRecetaCommandHandler> logger)
    {
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(UpdateRecetaCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var receta = await _recetaRepository.GetByIdWithDetallesAsync(command.Id, cancellationToken);
        if (receta == null)
        {
            return Result.Failure(Error.NotFound("RECETA_NOT_FOUND", "Receta no encontrada"));
        }

        // TODO(diag): remove after concurrency investigation
        _logger.LogInformation(
            "CONCURRENCY-DIAG loaded receta={Id} dbRowVersion={RowVersion} version={Version} insumos={Count}",
            receta.Id,
            Convert.ToBase64String(receta.RowVersion),
            receta.Version,
            receta.Insumos.Count);

        if (await _recetaRepository.ExistsWithSkuAsync(command.CodigoSku, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("SKU_ALREADY_EXISTS", "Ya existe otra receta con ese SKU"));
        }

        // Snapshot the current definition before mutating (spec §3.5 versioning)
        receta.Versiones.Add(new RecetaVersion
        {
            RecetaId = receta.Id,
            Version = receta.Version,
            Nombre = receta.Nombre,
            CodigoSku = receta.CodigoSku,
            DetallesJson = System.Text.Json.JsonSerializer.Serialize(receta.Insumos.Select(i => new
            {
                i.InsumoId,
                i.RecetaOrigenId,
                i.CantidadNecesaria,
                i.UnidadMedidaId,
                i.Observaciones
            })),
            FechaCreacion = RelojDeNegocio.Ahora
        });

        receta.Version += 1;

        receta.Nombre = command.Nombre;
        receta.CodigoSku = command.CodigoSku;
        receta.CategoriaId = command.CategoriaId;
        receta.UnidadMedidaId = command.UnidadMedidaId;
        receta.Descripcion = command.Descripcion;
        receta.Estado = command.Estado;

        // Line units are derived server-side: the client-sent unit is not trusted.
        var unidadesPorLinea = await RecetaLineaUnidades.DerivarAsync(
            _insumoRepository, _recetaRepository, command.Insumos, cancellationToken);
        if (unidadesPorLinea.IsFailure)
        {
            return Result.Failure(unidadesPorLinea.Error);
        }

        // Replace insumos: EF Core deletes orphans (required FK + Cascade) on collection removal.
        receta.Insumos.Clear();
        for (var i = 0; i < command.Insumos.Count; i++)
        {
            var detalle = command.Insumos[i];
            receta.Insumos.Add(new RecetaInsumo
            {
                RecetaId = receta.Id,
                InsumoId = detalle.InsumoId,
                RecetaOrigenId = detalle.RecetaOrigenId,
                CantidadNecesaria = detalle.CantidadNecesaria,
                UnidadMedidaId = unidadesPorLinea.Value[i],
                Observaciones = detalle.Observaciones
            });
        }

        // TODO(diag): remove after concurrency investigation
        _logger.LogInformation(
            "CONCURRENCY-DIAG saving receta={Id} trackedRowVersion={RowVersion}",
            receta.Id,
            Convert.ToBase64String(receta.RowVersion));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
