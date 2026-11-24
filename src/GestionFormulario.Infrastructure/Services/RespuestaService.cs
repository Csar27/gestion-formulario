using FluentValidation;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Respuestas;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Domain.Enums;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionFormulario.Infrastructure.Services;

public sealed class RespuestaService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IValorCampoValidator valorValidator,
    IValidator<GuardarRespuestaRequest> validator,
    TimeProvider timeProvider) : IRespuestaService
{
    public async Task<RespuestaDto> GuardarAsync(
        GuardarRespuestaRequest request,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var usuarioId = currentUser.UserId
            ?? throw new AppException(401, "NO_AUTENTICADO", "Se requiere autenticación.");

        var formulario = await db.Formularios
            .AsNoTracking()
            .Include(x => x.Secciones).ThenInclude(x => x.Campos)
            .FirstOrDefaultAsync(x => x.Id == request.FormularioId, cancellationToken)
            ?? throw new AppException(404, "FORMULARIO_NO_ENCONTRADO", "El formulario no existe.");

        await ValidarAccesoFormularioAsync(request.FormularioId, cancellationToken);

        var respuesta = await ObtenerRespuestaParaGuardarAsync(request, usuarioId, cancellationToken);
        var campos = formulario.Secciones.SelectMany(x => x.Campos).ToList();
        var camposPorId = campos.ToDictionary(x => x.Id);
        var valoresEnviados = request.Valores.ToList();

        if (valoresEnviados.Select(x => x.CampoId).Distinct().Count() != valoresEnviados.Count)
        {
            throw new AppException(400, "VALOR_DUPLICADO", "Un campo no puede enviarse dos veces.");
        }

        foreach (var valor in valoresEnviados.Where(x => !camposPorId.ContainsKey(x.CampoId)))
        {
            throw new AppException(400, "CAMPO_NO_VALIDO", $"El campo {valor.CampoId} no pertenece al formulario.");
        }

        var valoresExistentes = respuesta.Valores.ToDictionary(x => x.CampoId);
        var todosLosValores = valoresExistentes.ToDictionary(x => x.Key, x => x.Value.Valor);
        foreach (var valor in valoresEnviados)
        {
            todosLosValores[valor.CampoId] = valor.Valor ?? string.Empty;
        }

        var resultados = new List<(Campo Campo, ResultadoValidacion Resultado)>();

        if (request.Finalizar)
        {
            foreach (var campo in campos)
            {
                todosLosValores.TryGetValue(campo.Id, out var raw);
                resultados.Add((campo, await ValidarCampoAsync(campo, raw, campo.Requerido, todosLosValores, cancellationToken)));
            }
        }
        else if (request.SeccionId.HasValue)
        {
            var seccion = formulario.Secciones.FirstOrDefault(x => x.Id == request.SeccionId.Value)
                ?? throw new AppException(404, "SECCION_NO_ENCONTRADA", "La sección no existe en el formulario.");

            foreach (var campo in seccion.Campos)
            {
                todosLosValores.TryGetValue(campo.Id, out var raw);
                if (raw is null && !campo.Requerido)
                {
                    continue;
                }

                resultados.Add((campo, await ValidarCampoAsync(campo, raw, campo.Requerido, todosLosValores, cancellationToken)));
            }
        }
        else
        {
            foreach (var valor in valoresEnviados)
            {
                var campo = camposPorId[valor.CampoId];
                resultados.Add((campo, await ValidarCampoAsync(campo, valor.Valor, false, todosLosValores, cancellationToken)));
            }
        }

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var (campo, resultado) in resultados)
        {
            if (valoresExistentes.TryGetValue(campo.Id, out var valorExistente) &&
                (valorExistente.Valor != resultado.Valor || valorExistente.ValorTexto != resultado.ValorTexto))
            {
                db.RespuestaValorHistoriales.Add(new RespuestaValorHistorial
                {
                    Id = Guid.NewGuid(),
                    RespuestaId = respuesta.Id,
                    CampoId = campo.Id,
                    ValorAnterior = valorExistente.Valor,
                    ValorAnteriorTexto = valorExistente.ValorTexto,
                    ValorNuevo = resultado.Valor,
                    ValorNuevoTexto = resultado.ValorTexto,
                    Fecha = ahora,
                    UsuarioId = usuarioId
                });
            }
        }

        AplicarValores(respuesta, valoresExistentes, resultados);

        if (request.Finalizar)
        {
            respuesta.Estado = EstadoRespuesta.Completa;
        }

        await db.SaveChangesAsync(cancellationToken);
        var guardada = await ObtenerRespuestaDetalleAsync(respuesta.Id, cancellationToken);
        return Map(guardada);
    }

    public async Task<RespuestaDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var respuesta = await ObtenerRespuestaDetalleAsync(id, cancellationToken);
        await ValidarPropietarioAsync(respuesta, cancellationToken);
        return Map(respuesta);
    }

    public async Task<IReadOnlyList<RespuestaValorHistorialDto>> GetHistorialAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var respuesta = await db.Respuestas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "RESPUESTA_NO_ENCONTRADA", "La respuesta no existe.");

        await ValidarPropietarioAsync(respuesta, cancellationToken);

        var historial = await db.RespuestaValorHistoriales
            .AsNoTracking()
            .Where(x => x.RespuestaId == id)
            .OrderByDescending(x => x.Fecha)
            .Select(x => new RespuestaValorHistorialDto(
                x.Id,
                x.CampoId,
                db.Campos.Where(c => c.Id == x.CampoId).Select(c => c.Etiqueta).FirstOrDefault() ?? string.Empty,
                x.ValorAnterior,
                x.ValorAnteriorTexto,
                x.ValorNuevo,
                x.ValorNuevoTexto,
                x.Fecha,
                x.UsuarioId))
            .ToListAsync(cancellationToken);

        return historial;
    }

    public async Task<PagedResult<RespuestaListItemDto>> ListAsync(
        RespuestaListQuery query,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = currentUser.UserId
            ?? throw new AppException(401, "NO_AUTENTICADO", "Se requiere autenticación.");

        var pagina = Math.Max(1, query.Page);
        var tamano = Math.Clamp(query.PageSize, 1, 100);

        var filtro = db.Respuestas
            .AsNoTracking()
            .Include(x => x.Formulario)
            .Include(x => x.Valores)
            .AsQueryable();

        if (!currentUser.EsAdmin)
        {
            filtro = filtro.Where(x => x.UsuarioId == usuarioId);
        }

        if (query.FormularioId.HasValue)
        {
            filtro = filtro.Where(x => x.FormularioId == query.FormularioId.Value);
        }

        if (query.Desde.HasValue)
        {
            filtro = filtro.Where(x => x.Fecha >= query.Desde.Value);
        }

        if (query.Hasta.HasValue)
        {
            var limite = query.Hasta.Value.Date.AddDays(1);
            filtro = filtro.Where(x => x.Fecha < limite);
        }

        if (!string.IsNullOrWhiteSpace(query.Estado) &&
            Enum.TryParse<EstadoRespuesta>(query.Estado, ignoreCase: true, out var estado))
        {
            filtro = filtro.Where(x => x.Estado == estado);
        }

        var total = await filtro.CountAsync(cancellationToken);
        var items = await filtro
            .OrderByDescending(x => x.Fecha)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(x => new RespuestaListItemDto(
                x.Id,
                x.FormularioId,
                x.Formulario.Nombre,
                x.Fecha,
                x.Estado.ToString(),
                x.Valores.Count,
                x.UsuarioId))
            .ToListAsync(cancellationToken);

        return new PagedResult<RespuestaListItemDto>(items, total, pagina, tamano);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var respuesta = await db.Respuestas.FindAsync([id], cancellationToken)
            ?? throw new AppException(404, "RESPUESTA_NO_ENCONTRADA", "La respuesta no existe.");

        await ValidarPropietarioAsync(respuesta, cancellationToken);

        if (!currentUser.EsAdmin && respuesta.Estado == EstadoRespuesta.Completa)
        {
            throw new AppException(409, "RESPUESTA_NO_ELIMINABLE", "No puedes eliminar una respuesta completada.");
        }

        db.Respuestas.Remove(respuesta);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Respuesta> ObtenerRespuestaParaGuardarAsync(
        GuardarRespuestaRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (!request.Id.HasValue)
        {
            var nueva = new Respuesta
            {
                Id = Guid.NewGuid(),
                FormularioId = request.FormularioId,
                UsuarioId = usuarioId,
                Estado = EstadoRespuesta.Borrador,
                Fecha = timeProvider.GetUtcNow().UtcDateTime
            };
            db.Respuestas.Add(nueva);
            return nueva;
        }

        var respuesta = await db.Respuestas
            .Include(x => x.Valores)
            .FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
            ?? throw new AppException(404, "RESPUESTA_NO_ENCONTRADA", "La respuesta no existe.");

        if (respuesta.FormularioId != request.FormularioId)
        {
            throw new AppException(400, "FORMULARIO_DISTINTO", "La respuesta pertenece a otro formulario.");
        }

        await ValidarPropietarioAsync(respuesta, cancellationToken);

        return respuesta;
    }

    private async Task ValidarAccesoFormularioAsync(Guid formularioId, CancellationToken cancellationToken)
    {
        if (currentUser.EsAdmin)
        {
            return;
        }

        var roles = currentUser.Roles.ToList();
        var permitido = await db.Formularios
            .Where(x => x.Id == formularioId)
            .Select(x => !x.FormulariosRoles.Any(r => r.PuedeLlenar) ||
                         x.FormulariosRoles.Any(r => r.PuedeLlenar && roles.Contains(r.RolId)))
            .FirstOrDefaultAsync(cancellationToken);

        if (!permitido)
        {
            throw new AppException(403, "FORMULARIO_NO_PERMITIDO", "No tienes acceso a este formulario.");
        }
    }

    private Task ValidarPropietarioAsync(Respuesta respuesta, CancellationToken cancellationToken)
    {
        if (!currentUser.EsAdmin &&
            !string.Equals(respuesta.UsuarioId, currentUser.UserId, StringComparison.Ordinal))
        {
            throw new AppException(404, "RESPUESTA_NO_ENCONTRADA", "La respuesta no existe.");
        }

        return Task.CompletedTask;
    }

    private async Task<ResultadoValidacion> ValidarCampoAsync(
        Campo campo,
        string? raw,
        bool obligatorio,
        IReadOnlyDictionary<Guid, string> todosLosValores,
        CancellationToken cancellationToken)
    {
        string? valorPadre = null;
        if (campo.CampoPadreId.HasValue &&
            todosLosValores.TryGetValue(campo.CampoPadreId.Value, out var padre))
        {
            valorPadre = padre;
        }

        return await valorValidator.ValidarAsync(campo, raw, obligatorio, valorPadre, cancellationToken);
    }

    private static void AplicarValores(
        Respuesta respuesta,
        Dictionary<Guid, RespuestaValor> existentes,
        IReadOnlyList<(Campo Campo, ResultadoValidacion Resultado)> resultados)
    {
        foreach (var (campo, resultado) in resultados)
        {
            var existe = existentes.TryGetValue(campo.Id, out var valorExistente);

            if (resultado.Valor.Length == 0)
            {
                if (existe)
                {
                    respuesta.Valores.Remove(valorExistente!);
                    existentes.Remove(campo.Id);
                }
                continue;
            }

            if (existe)
            {
                valorExistente!.Valor = resultado.Valor;
                valorExistente.ValorTexto = resultado.ValorTexto;
            }
            else
            {
                var nuevo = new RespuestaValor
                {
                    RespuestaId = respuesta.Id,
                    CampoId = campo.Id,
                    Valor = resultado.Valor,
                    ValorTexto = resultado.ValorTexto
                };
                respuesta.Valores.Add(nuevo);
                existentes[campo.Id] = nuevo;
            }
        }
    }

    private async Task<Respuesta> ObtenerRespuestaDetalleAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Respuestas
            .AsNoTracking()
            .Include(x => x.Formulario)
            .Include(x => x.Valores).ThenInclude(x => x.Campo)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "RESPUESTA_NO_ENCONTRADA", "La respuesta no existe.");

    private static RespuestaDto Map(Respuesta respuesta) => new(
        respuesta.Id,
        respuesta.FormularioId,
        respuesta.Formulario?.Nombre ?? string.Empty,
        respuesta.Fecha,
        respuesta.UsuarioId,
        respuesta.Estado.ToString(),
        respuesta.Valores
            .OrderBy(x => x.Campo?.Orden)
            .Select(x => new RespuestaValorDto(
                x.CampoId,
                x.Campo?.Etiqueta ?? string.Empty,
                x.Valor,
                x.ValorTexto))
            .ToList());
}
