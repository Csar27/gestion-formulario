using System.Text;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Catalogos;
using GestionFormulario.Infrastructure.Persistence;
using GestionFormulario.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionFormulario.Tests;

public sealed class CatalogoServiceTests
{
    [Fact]
    public async Task ItemDeCatalogoDependiente_RequierePadreDelNivelSuperior()
    {
        await using var db = CrearDb(nameof(ItemDeCatalogoDependiente_RequierePadreDelNivelSuperior));
        var service = CrearServicio(db);
        var pais = await service.CreateAsync(NuevoCatalogo("Países"), default);
        var departamento = await service.CreateAsync(NuevoCatalogo("Departamentos", pais.Id), default);
        var colombia = await service.CreateItemAsync(
            pais.Id,
            new CatalogoItemCreateRequest("CO", "Colombia", 1, true, null),
            default);

        var sinPadre = await Assert.ThrowsAsync<AppException>(() => service.CreateItemAsync(
            departamento.Id,
            new CatalogoItemCreateRequest("ANT", "Antioquia", 1, true, null),
            default));
        Assert.Equal("PADRE_ITEM_REQUERIDO", sinPadre.Code);

        var padreInvalido = await Assert.ThrowsAsync<AppException>(() => service.CreateItemAsync(
            departamento.Id,
            new CatalogoItemCreateRequest("ANT", "Antioquia", 1, true, Guid.NewGuid()),
            default));
        Assert.Equal("PADRE_ITEM_INVALIDO", padreInvalido.Code);

        var antioquia = await service.CreateItemAsync(
            departamento.Id,
            new CatalogoItemCreateRequest("ANT", "Antioquia", 1, true, colombia.Id),
            default);
        Assert.Equal(colombia.Id, antioquia.PadreItemId);
    }

    [Fact]
    public async Task Opciones_FiltranPorPadreYTexto()
    {
        await using var db = CrearDb(nameof(Opciones_FiltranPorPadreYTexto));
        var service = CrearServicio(db);
        var pais = await service.CreateAsync(NuevoCatalogo("Países"), default);
        var departamento = await service.CreateAsync(NuevoCatalogo("Departamentos", pais.Id), default);
        var colombia = await service.CreateItemAsync(
            pais.Id, new CatalogoItemCreateRequest("CO", "Colombia", 1, true, null), default);
        var espana = await service.CreateItemAsync(
            pais.Id, new CatalogoItemCreateRequest("ES", "España", 2, true, null), default);
        await service.CreateItemAsync(
            departamento.Id, new CatalogoItemCreateRequest("ANT", "Antioquia", 1, true, colombia.Id), default);
        await service.CreateItemAsync(
            departamento.Id, new CatalogoItemCreateRequest("MAD", "Madrid", 1, true, espana.Id), default);

        Assert.Empty(await service.ObtenerOpcionesAsync(departamento.Id, null, null, true, default));

        var deColombia = await service.ObtenerOpcionesAsync(departamento.Id, colombia.Id, null, true, default);
        Assert.Single(deColombia);
        Assert.Equal("ANT", deColombia[0].Codigo);

        var filtradas = await service.ObtenerOpcionesAsync(departamento.Id, espana.Id, "mad", true, default);
        Assert.Single(filtradas);
        Assert.Equal("Madrid", filtradas[0].Texto);
    }

    [Fact]
    public async Task ImportarCsv_ReemplazarDesactivaItemsAusentes()
    {
        await using var db = CrearDb(nameof(ImportarCsv_ReemplazarDesactivaItemsAusentes));
        var service = CrearServicio(db);
        var catalogo = await service.CreateAsync(NuevoCatalogo("Colores"), default);

        var primera = await service.ImportarAsync(
            catalogo.Id,
            NuevaImportacion("Codigo,Texto,Orden\nRO, Rojo, 1\nAZ, Azul, 2\n", false),
            default);
        Assert.Equal(2, primera.Creadas);
        Assert.Empty(primera.Errores);

        var segunda = await service.ImportarAsync(
            catalogo.Id,
            NuevaImportacion("Codigo,Texto,Orden\nRO, Rojo intenso, 1\n", true),
            default);
        Assert.Equal(1, segunda.Actualizadas);
        Assert.Equal(1, segunda.Desactivadas);

        var detalle = await service.GetAsync(catalogo.Id, default);
        Assert.Equal(2, detalle.Items.Count);
        Assert.Contains(detalle.Items, x => x.Codigo == "RO" && x.Texto == "Rojo intenso" && x.Activo);
        Assert.Contains(detalle.Items, x => x.Codigo == "AZ" && !x.Activo);
    }

    [Fact]
    public async Task CambiarPadreAUnDescendiente_SeRechaza()
    {
        await using var db = CrearDb(nameof(CambiarPadreAUnDescendiente_SeRechaza));
        var service = CrearServicio(db);
        var padre = await service.CreateAsync(NuevoCatalogo("Nivel 1"), default);
        var hijo = await service.CreateAsync(NuevoCatalogo("Nivel 2", padre.Id), default);

        var excepcion = await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(
            padre.Id,
            new CatalogoUpdateRequest("Nivel 1", null, true, hijo.Id),
            default));
        Assert.Equal("CATALOGO_CICLICO", excepcion.Code);
    }

    private static CatalogoCreateRequest NuevoCatalogo(string nombre, Guid? padreId = null) =>
        new(nombre, null, true, padreId);

    private static ImportarCatalogoRequest NuevaImportacion(string contenido, bool reemplazar) =>
        new(new MemoryStream(Encoding.UTF8.GetBytes(contenido)), "catalogo.csv", reemplazar);

    private static CatalogoService CrearServicio(AppDbContext db) => new(
        db,
        new CatalogoCreateRequestValidator(),
        new CatalogoUpdateRequestValidator(),
        new CatalogoItemCreateRequestValidator(),
        new CatalogoItemUpdateRequestValidator(),
        new ImportarCatalogoRequestValidator());

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
