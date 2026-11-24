using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Auth;
using GestionFormulario.Application.Features.Catalogos;
using GestionFormulario.Application.Features.Formularios;
using GestionFormulario.Application.Features.Productos;
using GestionFormulario.Application.Features.Reportes;
using GestionFormulario.Application.Features.Respuestas;
using GestionFormulario.Application.Features.Roles;
using GestionFormulario.Application.Features.Usuarios;
using GestionFormulario.Infrastructure.Configuration;
using GestionFormulario.Infrastructure.Identity;
using GestionFormulario.Infrastructure.Persistence;
using GestionFormulario.Infrastructure.Seed;
using GestionFormulario.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionFormulario.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton(TimeProvider.System);

        services.AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<DbInitializer>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IRolService, RolService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IFormularioService, FormularioService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IRespuestaService, RespuestaService>();
        services.AddScoped<IReporteService, ReporteService>();
        services.AddScoped<IValorCampoValidator, ValorCampoValidator>();

        return services;
    }
}
