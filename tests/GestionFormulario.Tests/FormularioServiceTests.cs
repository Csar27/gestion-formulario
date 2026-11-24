using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Formularios;
using GestionFormulario.Infrastructure.Persistence;
using GestionFormulario.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionFormulario.Tests;

public sealed class FormularioServiceTests
{
    [Fact]
    public async Task ActualizarFormulario_ConservaIdsYCampoPadre()
    {
        await using var db = CrearDb(nameof(ActualizarFormulario_ConservaIdsYCampoPadre));
        var usuario = new UsuarioPrueba("test-user");
        var service = new FormularioService(
            db,
            usuario,
            new FormularioCreateRequestValidator(),
            new FormularioUpdateRequestValidator());

        var creado = await service.CreateAsync(
            new FormularioCreateRequest("Formulario", false, new[]
            {
                new SeccionRequest("Ubicacion", 1, new[] { Campo("Pais", 1) })
            }, null),
            default);

        var campoPaisId = creado.Secciones[0].Campos[0].Id;
        var actualizado = await service.UpdateAsync(
            creado.Id,
            new FormularioUpdateRequest("Formulario", false, new[]
            {
                new SeccionRequest("Ubicacion", 1, new[]
                {
                    Campo("Pais", 1),
                    new CampoRequest("Departamento", "Lista", true, 2, null, null,
                        "Catalogo", null, Guid.NewGuid(), campoPaisId)
                })
            }, null),
            default);

        Assert.Equal(campoPaisId, actualizado.Secciones[0].Campos[0].Id);
        Assert.Equal(campoPaisId, actualizado.Secciones[0].Campos[1].CampoPadreId);
        Assert.Equal(2, actualizado.Secciones[0].Campos.Count);
    }
    private static CampoRequest Campo(string etiqueta, int orden) =>
        new(etiqueta, "Texto", true, orden, null, null, "Manual", null, null, null);

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
