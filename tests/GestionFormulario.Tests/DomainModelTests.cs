using GestionFormulario.Application.Common;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Domain.Enums;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionFormulario.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void Permiso_Codigo_UsaFormatoModuloPuntoAccion()
    {
        Assert.Equal("Catalogos.Editar", Permiso.BuildCodigo(Modulo.Catalogos, Accion.Editar));
        Assert.Equal(35, Enum.GetValues<Modulo>().Length * Enum.GetValues<Accion>().Length);
    }

    [Fact]
    public async Task Campo_Dependiente_PuedePersistirConCatalogoYPconPadre()
    {
        await using var db = CrearDb(nameof(Campo_Dependiente_PuedePersistirConCatalogoYPconPadre));
        var catalogo = new Catalogo { Id = Guid.NewGuid(), Nombre = "Ciudades", Activo = true };
        var campoPadre = new Campo { Id = Guid.NewGuid(), Etiqueta = "Departamento", Tipo = TipoCampo.Lista, Orden = 1, SeccionId = Guid.Empty };
        db.AddRange(catalogo, campoPadre);

        var campo = new Campo
        {
            Id = Guid.NewGuid(),
            SeccionId = Guid.Empty,
            Etiqueta = "Ciudad",
            Tipo = TipoCampo.Lista,
            FuenteOpciones = FuenteOpciones.Catalogo,
            CatalogoId = catalogo.Id,
            CampoPadreId = campoPadre.Id,
            Orden = 2
        };
        db.Add(campo);
        await db.SaveChangesAsync();

        var guardado = await db.Campos.Include(x => x.Catalogo).Include(x => x.CampoPadre).SingleAsync(x => x.Id == campo.Id);
        Assert.Equal("Ciudades", guardado.Catalogo!.Nombre);
        Assert.Equal("Departamento", guardado.CampoPadre!.Etiqueta);
    }

    [Fact]
    public async Task RespuestaValor_ConservaSnapshotDeTextoYClaveCompuesta()
    {
        await using var db = CrearDb(nameof(RespuestaValor_ConservaSnapshotDeTextoYClaveCompuesta));
        var formulario = new Formulario { Id = Guid.NewGuid(), Nombre = "Encuesta" };
        var seccion = new Seccion { Id = Guid.NewGuid(), FormularioId = formulario.Id, Titulo = "Datos", Orden = 1 };
        var campo = new Campo { Id = Guid.NewGuid(), SeccionId = seccion.Id, Etiqueta = "País", Tipo = TipoCampo.Lista, Orden = 1 };
        var respuesta = new Respuesta { Id = Guid.NewGuid(), FormularioId = formulario.Id, Fecha = DateTime.UtcNow, UsuarioId = Guid.NewGuid().ToString() };
        db.AddRange(formulario, seccion, campo, respuesta);
        db.Add(new RespuestaValor
        {
            RespuestaId = respuesta.Id,
            CampoId = campo.Id,
            Valor = Guid.NewGuid().ToString(),
            ValorTexto = "Colombia"
        });
        await db.SaveChangesAsync();

        var valor = await db.RespuestaValores.SingleAsync();
        Assert.Equal("Colombia", valor.ValorTexto);
        Assert.Equal(respuesta.Id, valor.RespuestaId);
        Assert.Equal(campo.Id, valor.CampoId);
    }

    [Fact]
    public async Task SaveChanges_RegistraAuditoriaConUsuarioActual()
    {
        await using var db = CrearDb(nameof(SaveChanges_RegistraAuditoriaConUsuarioActual));
        db.Add(new Formulario { Id = Guid.NewGuid(), Nombre = "Formulario auditado" });
        await db.SaveChangesAsync();

        var auditoria = await db.Auditorias.SingleAsync();
        var formulario = await db.Formularios.SingleAsync();
        Assert.Equal("Formulario", auditoria.Entidad);
        Assert.Equal("test-user", auditoria.UsuarioId);
        Assert.Equal("test-user", formulario.CreadoPorId);
        Assert.Equal(AccionAuditoria.Crear, auditoria.Accion);
    }

    private static AppDbContext CrearDb(string nombre) => new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(nombre).Options,
        new UsuarioPrueba("test-user"),
        TimeProvider.System);

    private sealed class UsuarioPrueba(string? userId, params string[] roles) : ICurrentUserService
    {
        public string? UserId { get; } = userId;
        public IReadOnlyList<string> Roles { get; } = roles;
    }
}
