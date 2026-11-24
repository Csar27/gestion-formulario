using FluentValidation;
using GestionFormulario.Domain.Enums;

namespace GestionFormulario.Application.Features.Formularios;

public sealed class FormularioCreateRequestValidator : AbstractValidator<FormularioCreateRequest>
{
    public FormularioCreateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Secciones).NotEmpty().WithMessage("El formulario debe tener al menos una sección.");
        RuleForEach(x => x.Secciones).SetValidator(new SeccionRequestValidator());
    }
}

public sealed class FormularioUpdateRequestValidator : AbstractValidator<FormularioUpdateRequest>
{
    public FormularioUpdateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Secciones).NotEmpty().WithMessage("El formulario debe tener al menos una sección.");
        RuleForEach(x => x.Secciones).SetValidator(new SeccionRequestValidator());
    }
}

public sealed class SeccionRequestValidator : AbstractValidator<SeccionRequest>
{
    public SeccionRequestValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Campos).NotEmpty().WithMessage("Cada sección debe tener al menos un campo.");
        RuleForEach(x => x.Campos).SetValidator(new CampoRequestValidator());
    }
}

public sealed class CampoRequestValidator : AbstractValidator<CampoRequest>
{
    public CampoRequestValidator()
    {
        RuleFor(x => x.Etiqueta).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Orden).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Tipo)
            .NotEmpty()
            .Must(x => Enum.TryParse<TipoCampo>(x, ignoreCase: true, out _))
            .WithMessage("Tipo de campo no válido.");
        RuleFor(x => x.FuenteOpciones)
            .NotEmpty()
            .Must(x => Enum.TryParse<FuenteOpciones>(x, ignoreCase: true, out _))
            .WithMessage("Fuente de opciones no válida.");
        RuleFor(x => x)
            .Must(x => x.Min is null || x.Max is null || x.Min <= x.Max)
            .WithMessage("Min no puede ser mayor que Max.");
        RuleFor(x => x)
            .Must(x =>
            {
                var tipo = Enum.TryParse<TipoCampo>(x.Tipo, true, out var t) ? t : TipoCampo.Texto;
                var fuente = Enum.TryParse<FuenteOpciones>(x.FuenteOpciones, true, out var f) ? f : FuenteOpciones.Manual;
                return tipo != TipoCampo.Lista ||
                       (fuente == FuenteOpciones.Catalogo
                           ? x.CatalogoId.HasValue
                           : x.Opciones is { Count: > 0 });
            })
            .WithMessage("Un campo Lista requiere opciones manuales o un catálogo.");
    }
}
