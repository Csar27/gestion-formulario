using FluentValidation;

namespace GestionFormulario.Application.Features.Catalogos;

public sealed class CatalogoCreateRequestValidator : AbstractValidator<CatalogoCreateRequest>
{
    public CatalogoCreateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descripcion).MaximumLength(1000);
    }
}

public sealed class CatalogoUpdateRequestValidator : AbstractValidator<CatalogoUpdateRequest>
{
    public CatalogoUpdateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descripcion).MaximumLength(1000);
    }
}

public sealed class CatalogoItemCreateRequestValidator : AbstractValidator<CatalogoItemCreateRequest>
{
    public CatalogoItemCreateRequestValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Texto).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Orden).GreaterThanOrEqualTo(0);
    }
}

public sealed class CatalogoItemUpdateRequestValidator : AbstractValidator<CatalogoItemUpdateRequest>
{
    public CatalogoItemUpdateRequestValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Texto).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Orden).GreaterThanOrEqualTo(0);
    }
}

public sealed class ImportarCatalogoRequestValidator : AbstractValidator<ImportarCatalogoRequest>
{
    public ImportarCatalogoRequestValidator()
    {
        RuleFor(x => x.Contenido).NotNull();
        RuleFor(x => x.NombreArchivo)
            .NotEmpty()
            .Must(x => x.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                       x.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("El archivo debe ser .csv o .xlsx.");
    }
}
