using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Respuestas;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Domain.Enums;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionFormulario.Infrastructure.Services;

public sealed class ValorCampoValidator(AppDbContext db) : IValorCampoValidator
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-ES");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ResultadoValidacion> ValidarAsync(
        Campo campo,
        string? valor,
        bool obligatorio,
        string? valorPadre = null,
        CancellationToken cancellationToken = default)
    {
        var texto = valor?.Trim();

        if (string.IsNullOrWhiteSpace(texto))
        {
            if (obligatorio)
            {
                throw new AppException(400, "CAMPO_REQUERIDO", $"El campo '{campo.Etiqueta}' es obligatorio.");
            }

            return new ResultadoValidacion(string.Empty, null);
        }

        switch (campo.Tipo)
        {
            case TipoCampo.Texto:
                return new ResultadoValidacion(texto, texto);

            case TipoCampo.Email:
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        texto, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
                {
                    throw new AppException(400, "VALOR_INVALIDO", $"El campo '{campo.Etiqueta}' debe ser un correo válido.");
                }
                return new ResultadoValidacion(texto, texto);

            case TipoCampo.Fecha:
                if (!DateOnly.TryParseExact(
                        texto,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var fecha))
                {
                    throw new AppException(400, "VALOR_INVALIDO", $"El campo '{campo.Etiqueta}' debe ser una fecha válida (AAAA-MM-DD).");
                }
                return new ResultadoValidacion(
                    fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    fecha.ToString("d", Cultura));

            case TipoCampo.Hora:
                if (!TimeOnly.TryParseExact(
                        texto,
                        new[] { "HH:mm", "HH:mm:ss" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var hora))
                {
                    throw new AppException(400, "VALOR_INVALIDO", $"El campo '{campo.Etiqueta}' debe ser una hora válida (HH:mm).");
                }
                return new ResultadoValidacion(
                    hora.ToString("HH:mm", CultureInfo.InvariantCulture),
                    hora.ToString("HH:mm", Cultura));

            case TipoCampo.Entero:
                if (!int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entero))
                {
                    throw new AppException(400, "VALOR_INVALIDO", $"El campo '{campo.Etiqueta}' debe ser un entero.");
                }
                ValidarRango(campo, entero);
                return new ResultadoValidacion(
                    entero.ToString(CultureInfo.InvariantCulture),
                    entero.ToString("N0", Cultura));

            case TipoCampo.Decimal:
            case TipoCampo.Moneda:
            case TipoCampo.Porcentaje:
                if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
                {
                    throw new AppException(400, "VALOR_INVALIDO", $"El campo '{campo.Etiqueta}' debe ser un número.");
                }
                if (campo.Tipo == TipoCampo.Porcentaje && (numero < 0 || numero > 100))
                {
                    throw new AppException(400, "VALOR_FUERA_RANGO", "El porcentaje debe estar entre 0 y 100.");
                }
                ValidarRango(campo, numero);
                return new ResultadoValidacion(
                    numero.ToString(CultureInfo.InvariantCulture),
                    FormatearNumero(campo.Tipo, numero));

            case TipoCampo.Archivo:
                return new ResultadoValidacion(texto, texto);

            case TipoCampo.Lista:
                var opciones = await ObtenerOpcionesAsync(campo, valorPadre, cancellationToken);
                var opcion = opciones.FirstOrDefault(
                    x => string.Equals(x.Valor, texto, StringComparison.OrdinalIgnoreCase));
                if (opcion is null)
                {
                    throw new AppException(
                        400,
                        "OPCION_NO_VALIDA",
                        $"El valor '{texto}' no es una opción válida para '{campo.Etiqueta}'.");
                }
                return new ResultadoValidacion(opcion.Valor, opcion.Etiqueta);

            default:
                throw new AppException(400, "TIPO_CAMPO_NO_SOPORTADO", $"Tipo de campo no soportado: {campo.Tipo}.");
        }
    }

    private static void ValidarRango(Campo campo, decimal valor)
    {
        if (campo.Min is decimal min && valor < min)
        {
            throw new AppException(
                400,
                "VALOR_FUERA_RANGO",
                $"El campo '{campo.Etiqueta}' debe ser mayor o igual que {min.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (campo.Max is decimal max && valor > max)
        {
            throw new AppException(
                400,
                "VALOR_FUERA_RANGO",
                $"El campo '{campo.Etiqueta}' debe ser menor o igual que {max.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    private static string FormatearNumero(TipoCampo tipo, decimal valor) => tipo switch
    {
        TipoCampo.Moneda => valor.ToString("C2", Cultura),
        TipoCampo.Porcentaje => $"{valor.ToString("F2", Cultura)} %",
        _ => valor.ToString("N2", Cultura)
    };

    private async Task<IReadOnlyList<OpcionInterna>> ObtenerOpcionesAsync(
        Campo campo,
        string? valorPadre,
        CancellationToken cancellationToken)
    {
        if (campo.FuenteOpciones == FuenteOpciones.Catalogo)
        {
            if (campo.CatalogoId is null)
            {
                throw new AppException(400, "CATALOGO_NO_CONFIGURADO", "El campo no tiene catálogo asignado.");
            }

            var catalogoPadreId = await db.Catalogos
                .AsNoTracking()
                .Where(c => c.Id == campo.CatalogoId.Value)
                .Select(c => c.CatalogoPadreId)
                .FirstOrDefaultAsync(cancellationToken);

            if (catalogoPadreId.HasValue && !campo.CampoPadreId.HasValue)
            {
                return Array.Empty<OpcionInterna>();
            }

            var query = db.CatalogoItems
                .AsNoTracking()
                .Where(x => x.CatalogoId == campo.CatalogoId.Value && x.Activo);

            if (campo.CampoPadreId.HasValue || catalogoPadreId.HasValue)
            {
                if (!Guid.TryParse(valorPadre, out var padreItemId))
                {
                    return Array.Empty<OpcionInterna>();
                }

                if (catalogoPadreId.HasValue)
                {
                    var padreValido = await db.CatalogoItems
                        .AsNoTracking()
                        .AnyAsync(
                            x => x.Id == padreItemId &&
                                 x.CatalogoId == catalogoPadreId.Value &&
                                 x.Activo,
                            cancellationToken);
                    if (!padreValido)
                    {
                        return Array.Empty<OpcionInterna>();
                    }
                }

                query = query.Where(x => x.PadreItemId == padreItemId);
            }

            var items = await query.OrderBy(x => x.Orden).ThenBy(x => x.Texto).ToListAsync(cancellationToken);
            return items.Select(x => new OpcionInterna(x.Id.ToString(), x.Texto)).ToList();
        }

        if (string.IsNullOrWhiteSpace(campo.OpcionesJson))
        {
            return Array.Empty<OpcionInterna>();
        }

        var opciones = JsonSerializer.Deserialize<List<OpcionJson>>(campo.OpcionesJson, JsonOptions)
            ?? new List<OpcionJson>();
        return opciones.Select(x => new OpcionInterna(x.Valor, x.Etiqueta)).ToList();
    }

    private sealed record OpcionInterna(string Valor, string Etiqueta);

    private sealed class OpcionJson
    {
        [JsonPropertyName("valor")]
        public string Valor { get; set; } = string.Empty;

        [JsonPropertyName("etiqueta")]
        public string Etiqueta { get; set; } = string.Empty;
    }
}
