using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Respuestas;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Domain.Enums;
using GestionFormulario.Infrastructure.Persistence;
using GestionFormulario.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionFormulario.Tests;

public sealed class ValorCampoValidatorTests
{
    [Fact]
    public async Task Moneda_NormalizaValorYTextoEnEspanol()
    {
        await using var db = CrearDb(nameof(Moneda_NormalizaValorYTextoEnEspanol));
        var validator = new ValorCampoValidator(db);
        var campo = Campo(TipoCampo.Moneda);

        var resultado = await validator.ValidarAsync(campo, "1234.56", true);

        Assert.Equal("1234.56", resultado.Valor);
        Assert.Contains("1.234,56", resultado.ValorTexto);
        Assert.Contains("€", resultado.ValorTexto);
    }

    [Fact]
    public async Task Porcentaje_ValidaRangoYFormato()
    {
        await using var db = CrearDb(nameof(Porcentaje_ValidaRangoYFormato));
        var validator = new ValorCampoValidator(db);
        var campo = Campo(TipoCampo.Porcentaje);

        var resultado = await validator.ValidarAsync(campo, "15", true);
        Assert.Equal("15", resultado.Valor);
        Assert.Equal("15,00 %", resultado.ValorTexto);

        var excepcion = await Assert.ThrowsAsync<AppException>(
            () => validator.ValidarAsync(campo, "101", true));
        Assert.Equal("VALOR_FUERA_RANGO", excepcion.Code);
    }

    [Fact]
    public async Task Fecha_NormalizaYValidaFormato()
    {
        await using var db = CrearDb(nameof(Fecha_NormalizaYValidaFormato));
        var validator = new ValorCampoValidator(db);
        var campo = Campo(TipoCampo.Fecha);

        var resultado = await validator.ValidarAsync(campo, "2026-11-24", true);
        Assert.Equal("2026-11-24", resultado.Valor);
        Assert.Equal("24/11/2026", resultado.ValorTexto);

        var excepcion = await Assert.ThrowsAsync<AppException>(
            () => validator.ValidarAsync(campo, "24/11/2026", true));
        Assert.Equal("VALOR_INVALIDO", excepcion.Code);
    }

    [Fact]
    public async Task Hora_NormalizaYValidaFormato()
    {
        await using var db = CrearDb(nameof(Hora_NormalizaYValidaFormato));
        var validator = new ValorCampoValidator(db);
        var campo = Campo(TipoCampo.Hora);

        var resultado = await validator.ValidarAsync(campo, "14:30:45", true);
        Assert.Equal("14:30", resultado.Valor);
        Assert.Equal("14:30", resultado.ValorTexto);

        var excepcion = await Assert.ThrowsAsync<AppException>(
            () => validator.ValidarAsync(campo, "25:99", true));
        Assert.Equal("VALOR_INVALIDO", excepcion.Code);
    }

    [Fact]
    public async Task CampoRequeridoVacio_LanzaCodigoEstable()
    {
        await using var db = CrearDb(nameof(CampoRequeridoVacio_LanzaCodigoEstable));
        var validator = new ValorCampoValidator(db);

        var excepcion = await Assert.ThrowsAsync<AppException>(
            () => validator.ValidarAsync(Campo(TipoCampo.Texto), "  ", true));

        Assert.Equal("CAMPO_REQUERIDO", excepcion.Code);
    }

    [Fact]
    public async Task EmailInvalido_LanzaCodigoEstable()
    {
        await using var db = CrearDb(nameof(EmailInvalido_LanzaCodigoEstable));
        var validator = new ValorCampoValidator(db);

        var excepcion = await Assert.ThrowsAsync<AppException>(
            () => validator.ValidarAsync(Campo(TipoCampo.Email), "correo-mal", true));

        Assert.Equal("VALOR_INVALIDO", excepcion.Code);
    }

    [Fact]
    public async Task ListaManual_RechazaOpcionNoValida()
    {
        await using var db = CrearDb(nameof(ListaManual_RechazaOpcionNoValida));
        var validator = new ValorCampoValidator(db);
        var campo = Campo(TipoCampo.Lista);
        campo.OpcionesJson = "[{\"valor\":\"SI\",\"etiqueta\":\"Sí\"}]";

        var valido = await validator.ValidarAsync(campo, "SI", true);
        Assert.Equal("Sí", valido.ValorTexto);

        var excepcion = await Assert.ThrowsAsync<AppException>(
            () => validator.ValidarAsync(campo, "TALVEZ", true));
        Assert.Equal("OPCION_NO_VALIDA", excepcion.Code);
    }

    [Fact]
    public async Task CatalogoDependiente_FiltraPorItemPadre()
    {
        await using var db = CrearDb(nameof(CatalogoDependiente_FiltraPorItemPadre));
        var validator = new ValorCampoValidator(db);
        var catalogoId = Guid.NewGuid();
        var padreId = Guid.NewGuid();
        var otroPadreId = Guid.NewGuid();

        db.Catalogos.Add(new Catalogo { Id = catalogoId, Nombre = "Ciudades", Activo = true });
        db.CatalogoItems.AddRange(
            new CatalogoItem { Id = Guid.NewGuid(), CatalogoId = catalogoId, Codigo = "MAD", Texto = "Madrid", PadreItemId = padreId, Activo = true },
            new CatalogoItem { Id = Guid.NewGuid(), CatalogoId = catalogoId, Codigo = "BCN", Texto = "Barcelona", PadreItemId = otroPadreId, Activo = true });
        await db.SaveChangesAsync();

        var campo = Campo(TipoCampo.Lista);
        campo.FuenteOpciones = FuenteOpciones.Catalogo;
        campo.CatalogoId = catalogoId;
        campo.CampoPadreId = Guid.NewGuid();

        var hijo = db.CatalogoItems.First(x => x.Codigo == "MAD");
        var resultado = await validator.ValidarAsync(campo, hijo.Id.ToString(), true, padreId.ToString());
        Assert.Equal("Madrid", resultado.ValorTexto);

        var excepcion = await Assert.ThrowsAsync<AppException>(
            () => validator.ValidarAsync(campo, hijo.Id.ToString(), true, otroPadreId.ToString()));
        Assert.Equal("OPCION_NO_VALIDA", excepcion.Code);
    }

    private static Campo Campo(TipoCampo tipo) => new()
    {
        Id = Guid.NewGuid(),
        SeccionId = Guid.NewGuid(),
        Etiqueta = "Campo de prueba",
        Tipo = tipo,
        Requerido = true,
        Orden = 1
    };

    private static AppDbContext CrearDb(string nombre) => new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(nombre).Options,
        new UsuarioPrueba("test-user"),
        TimeProvider.System);

    private sealed class UsuarioPrueba(string? userId) : ICurrentUserService
    {
        public string? UserId { get; } = userId;
        public IReadOnlyList<string> Roles { get; } = new[] { "Admin" };
    }
}
