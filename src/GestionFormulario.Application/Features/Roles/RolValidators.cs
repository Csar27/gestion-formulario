using FluentValidation;

namespace GestionFormulario.Application.Features.Roles;

public sealed class RolCreateRequestValidator : AbstractValidator<RolCreateRequest>
{
    public RolCreateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Descripcion).MaximumLength(500).When(x => x.Descripcion is not null);
    }
}

public sealed class RolUpdateRequestValidator : AbstractValidator<RolUpdateRequest>
{
    public RolUpdateRequestValidator()
    {
        RuleFor(x => x.Descripcion).MaximumLength(500).When(x => x.Descripcion is not null);
    }
}

public sealed class AsignarPermisosRequestValidator : AbstractValidator<AsignarPermisosRequest>
{
    public AsignarPermisosRequestValidator()
    {
        RuleFor(x => x.Codigos).NotNull();
    }
}
