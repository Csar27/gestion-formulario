using FluentValidation;

namespace GestionFormulario.Application.Features.Usuarios;

public sealed class UsuarioCreateRequestValidator : AbstractValidator<UsuarioCreateRequest>
{
    public UsuarioCreateRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class UsuarioUpdateRequestValidator : AbstractValidator<UsuarioUpdateRequest>
{
    public UsuarioUpdateRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(200);
    }
}
